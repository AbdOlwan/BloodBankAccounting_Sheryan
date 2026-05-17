using BloodBankAccounting.Data;
using BloodBankAccounting.Helpers;
using BloodBankAccounting.Views;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Infrastructure;
using System.Windows;

namespace BloodBankAccounting;

public partial class App : Application
{
    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // ─── Global Exception Handlers ─────────────────────────────────
        DispatcherUnhandledException += (s, ex) =>
        {
            DiagnosticLogger.Log("DispatcherUnhandledException", ex.Exception);
            MessageBox.Show(
                $"خطأ غير متوقع في الواجهة:\n\n" +
                $"النوع: {ex.Exception.GetType().Name}\n" +
                $"الرسالة: {ex.Exception.Message}\n\n" +
                $"الخطأ الداخلي: {ex.Exception.InnerException?.Message}\n\n" +
                $"تم حفظ التفاصيل في:\n{DiagnosticLogger.GetLogPath()}",
                "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            ex.Handled = true; // يمنع إغلاق البرنامج
        };

        AppDomain.CurrentDomain.UnhandledException += (s, ex) =>
        {
            if (ex.ExceptionObject is Exception err)
            {
                DiagnosticLogger.Log("UnhandledException", err);
                try
                {
                    MessageBox.Show(
                        $"خطأ حرج:\n{err.Message}\n\nتم الحفظ في:\n{DiagnosticLogger.GetLogPath()}",
                        "خطأ حرج", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                catch { }
            }
        };

        TaskScheduler.UnobservedTaskException += (s, ex) =>
        {
            DiagnosticLogger.Log("UnobservedTaskException", ex.Exception);
            ex.SetObserved(); // يمنع انهيار التطبيق بسبب Task غير مراقب
        };

        ShutdownMode = ShutdownMode.OnExplicitShutdown;

        QuestPDF.Settings.License = LicenseType.Community;

        // ─── 1. Trial / License check ──────────────────────────────────
        if (!TrialManager.IsActivated())
        {
            var trial = new TrialView();
            trial.ShowDialog();
            if (!trial.Proceed) { Shutdown(); return; }
        }

        // ─── 2. Database ───────────────────────────────────────────────
        try
        {
            using var db = new AppDbContext();
            await InitializeDatabaseAsync(db);
        }
        catch (Exception ex)
        {
            DiagnosticLogger.Log("OnStartup.InitializeDatabase", ex);
            MessageBox.Show(
                $"فشل الاتصال بقاعدة البيانات:\n{ex.Message}\n\nالخطأ الداخلي: {ex.InnerException?.Message}",
                "خطأ في الاتصال",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            Shutdown();
            return;
        }

        // ─── 3. Open Main Window ───────────────────────────────────────
        var main = new MainWindow();
        MainWindow = main;
        ShutdownMode = ShutdownMode.OnMainWindowClose;
        main.Show();
    }

    // ───────────────────────────────────────────────────────────────────
    // تهيئة قاعدة البيانات بذكاء:
    //   • قاعدة بيانات موجودة (عميل قديم) → طبّق التغييرات الجديدة فقط
    //   • قاعدة بيانات غير موجودة (تثبيت جديد) → أنشئ كل شيء من أول
    // ───────────────────────────────────────────────────────────────────
    private static async Task InitializeDatabaseAsync(AppDbContext db)
    {
        bool dbExists = await db.Database.CanConnectAsync();

        if (dbExists)
            await ApplyDeltaMigrationAsync(db);
        else
            await db.Database.MigrateAsync();
    }

    // ───────────────────────────────────────────────────────────────────
    // يطبّق فقط التغييرات الجديدة على قاعدة بيانات العميل القديم
    // كل خطوة محمية بـ IF NOT EXISTS — لا خطر على البيانات الموجودة
    // ───────────────────────────────────────────────────────────────────
    private static async Task ApplyDeltaMigrationAsync(AppDbContext db)
    {
        // ── 1. إضافة عمود BloodStockId في InvoiceLines ─────────────────
        await db.Database.ExecuteSqlRawAsync(@"
            IF NOT EXISTS (
                SELECT 1 FROM sys.columns
                WHERE object_id = OBJECT_ID(N'InvoiceLines')
                  AND name = N'BloodStockId'
            )
            BEGIN
                ALTER TABLE [InvoiceLines] ADD [BloodStockId] int NULL;
            END
        ");

        // ── 2. إضافة الـ Index ──────────────────────────────────────────
        await db.Database.ExecuteSqlRawAsync(@"
            IF NOT EXISTS (
                SELECT 1 FROM sys.indexes
                WHERE name = N'IX_InvoiceLines_BloodStockId'
                  AND object_id = OBJECT_ID(N'InvoiceLines')
            )
            BEGIN
                CREATE INDEX [IX_InvoiceLines_BloodStockId]
                ON [InvoiceLines] ([BloodStockId]);
            END
        ");

        // ── 3. إضافة الـ Foreign Key ────────────────────────────────────
        await db.Database.ExecuteSqlRawAsync(@"
            IF NOT EXISTS (
                SELECT 1 FROM sys.foreign_keys
                WHERE name = N'FK_InvoiceLines_BloodStocks_BloodStockId'
            )
            BEGIN
                ALTER TABLE [InvoiceLines]
                ADD CONSTRAINT [FK_InvoiceLines_BloodStocks_BloodStockId]
                FOREIGN KEY ([BloodStockId]) REFERENCES [BloodStocks] ([Id]);
            END
        ");

        // ── 4. تصحيح جدول __EFMigrationsHistory ────────────────────────
        // ننشئ الجدول لو مش موجود، ثم نكتب الـ IDs الصحيحة بالظبط
        await db.Database.ExecuteSqlRawAsync(@"
            IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
            BEGIN
                CREATE TABLE [__EFMigrationsHistory] (
                    [MigrationId]    nvarchar(150) NOT NULL,
                    [ProductVersion] nvarchar(32)  NOT NULL,
                    CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
                );
            END

            DELETE FROM [__EFMigrationsHistory];

            INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
            VALUES
                (N'20260514133505_EditProductCatalog', N'8.0.26'),
                (N'20260515041509_EditInvoce',         N'8.0.26');
        ");
    }

    protected override void OnExit(ExitEventArgs e)
    {
        base.OnExit(e);
    }
}