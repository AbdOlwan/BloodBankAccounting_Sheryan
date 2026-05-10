using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System.IO;

namespace BloodBankAccounting.Data;

/// <summary>
/// عمليات الصيانة السرية لقاعدة البيانات — للمبرمج فقط.
/// </summary>
public static class DatabaseAdminService
{
    // ─── Connection string helper ─────────────────────────────────────────
    private static string GetConnectionString()
    {
        var config = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false)
            .Build();
        return config.GetConnectionString("DefaultConnection")!;
    }

    private static string GetMasterConnectionString()
    {
        // نفس الـ connection string بس Database = master
        var builder = new SqlConnectionStringBuilder(GetConnectionString());
        builder.InitialCatalog = "master";
        return builder.ToString();
    }

    private static string GetDatabaseName()
    {
        var builder = new SqlConnectionStringBuilder(GetConnectionString());
        return builder.InitialCatalog;
    }

    // ══════════════════════════════════════════════════════════════════════
    // 1. حذف قاعدة البيانات بالكامل
    // ══════════════════════════════════════════════════════════════════════
    /// <summary>
    /// يحذف قاعدة البيانات من الـ SQL Server نهائياً.
    /// يستخدم بعد الانتهاء من التجريب عند العميل.
    /// </summary>
    public static async Task DropDatabaseAsync()
    {
        var dbName = GetDatabaseName();

        await using var conn = new SqlConnection(GetMasterConnectionString());
        await conn.OpenAsync();

        // أوّلاً: فصل كل الـ connections الحالية
        var closeSessions = $"""
            ALTER DATABASE [{dbName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
            """;

        var dropDb = $"""
            DROP DATABASE IF EXISTS [{dbName}];
            """;

        await using (var cmd = new SqlCommand(closeSessions, conn))
            await cmd.ExecuteNonQueryAsync();

        await using (var cmd = new SqlCommand(dropDb, conn))
            await cmd.ExecuteNonQueryAsync();
    }

    // ══════════════════════════════════════════════════════════════════════
    // 2. مسح كل البيانات مع الاحتفاظ بالهيكل والـ Seed
    // ══════════════════════════════════════════════════════════════════════
    /// <summary>
    /// يحذف كل الصفوف من كل الجداول ويُعيد تشغيل الـ Seed الأساسي
    /// (الخزينة الرئيسية + إعدادات المنظمة).
    /// </summary>
    public static async Task ClearAllDataAsync()
    {
        await using var conn = new SqlConnection(GetConnectionString());
        await conn.OpenAsync();

        // الترتيب مهم — نبدأ من الجداول اللي فيها FK تجاه غيرها
        string[] tables =
        [
            "JournalEntries",
            "InvoiceLines",
            "ReceiptVouchers",
            "Invoices",
            "BloodBags",
            "Customers",
        ];

        foreach (var table in tables)
        {
            var sql = $"DELETE FROM [{table}]; DBCC CHECKIDENT ('[{table}]', RESEED, 0);";
            await using var cmd = new SqlCommand(sql, conn);
            await cmd.ExecuteNonQueryAsync();
        }

        // إعادة ضبط رصيد الخزينة الرئيسية
        await using var resetSafe = new SqlCommand(
            "UPDATE Safes SET Balance = 0 WHERE IsMain = 1;", conn);
        await resetSafe.ExecuteNonQueryAsync();
    }

    // ══════════════════════════════════════════════════════════════════════
    // 3. Backup قاعدة البيانات
    // ══════════════════════════════════════════════════════════════════════
    /// <summary>
    /// يعمل BACKUP DATABASE وبيحفظ الملف في مجلد Documents الخاص بالمستخدم.
    /// يرجع المسار الكامل لملف الـ .bak اللي اتنشأ.
    /// </summary>
    public static async Task<string> BackupDatabaseAsync()
    {
        var dbName = GetDatabaseName();
        var timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
        var fileName = $"{dbName}_Backup_{timestamp}.bak";

        // ─── أفضل مكان للـ backup ──────────────────────────────────────────
        // نستخدم مجلد مؤقت على سيرفر الـ SQL أوّل (SQL Server لازم يكون له
        // صلاحية الكتابة فيه)، وبعدين ننقل الملف لـ Documents.
        var sqlServerBackupDir = await GetSqlServerDefaultBackupPathAsync();
        var tempPath = Path.Combine(sqlServerBackupDir, fileName);

        // تنفيذ الـ BACKUP
        await using var conn = new SqlConnection(GetConnectionString());
        await conn.OpenAsync();

        var backupSql = $"""
            BACKUP DATABASE [{dbName}]
            TO DISK = N'{tempPath}'
            WITH FORMAT, INIT,
                 NAME = N'{dbName} Full Backup',
                 COMPRESSION,
                 STATS = 10;
            """;

        await using (var cmd = new SqlCommand(backupSql, conn) { CommandTimeout = 120 })
            await cmd.ExecuteNonQueryAsync();

        // ─── ننقل الملف لـ Documents\BloodBankAccounting\Backups ───────────
        var docsFolder = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        var destDir = Path.Combine(docsFolder, "BloodBankAccounting", "Backups");
        Directory.CreateDirectory(destDir);

        var destPath = Path.Combine(destDir, fileName);

        // لو الـ temp path مختلف عن الـ dest path ننقله
        if (!string.Equals(tempPath, destPath, StringComparison.OrdinalIgnoreCase))
        {
            File.Move(tempPath, destPath, overwrite: true);
        }

        return destPath;
    }

    // ─── helper: جيب مسار الـ Backup الافتراضي لـ SQL Server ──────────────
    private static async Task<string> GetSqlServerDefaultBackupPathAsync()
    {
        await using var conn = new SqlConnection(GetMasterConnectionString());
        await conn.OpenAsync();

        // نقرأ من الـ Registry عبر SQL Server
        var sql = """
            DECLARE @path NVARCHAR(512);
            EXEC master.dbo.xp_instance_regread
                N'HKEY_LOCAL_MACHINE',
                N'Software\Microsoft\MSSQLServer\MSSQLServer',
                N'BackupDirectory',
                @path OUTPUT;
            SELECT @path;
            """;

        await using var cmd = new SqlCommand(sql, conn);
        var result = await cmd.ExecuteScalarAsync();

        if (result is string path && !string.IsNullOrWhiteSpace(path))
            return path;

        // fallback لو فشل
        return Path.GetTempPath();
    }
}