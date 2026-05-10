using System.IO;
using System.Text.Json;

namespace BloodBankAccounting.Data;

/// <summary>
/// خدمة النسخ الاحتياطي اليومي التلقائي.
/// تعمل في الخلفية بدون أي تدخل من المستخدم.
/// </summary>
public sealed class AutoBackupService : IDisposable
{
    // ─── Singleton ────────────────────────────────────────────────────────
    public static readonly AutoBackupService Instance = new();

    // ─── Events ───────────────────────────────────────────────────────────
    /// <summary>يُطلَق بعد كل نسخة احتياطية ناجحة بمسار الملف الناتج.</summary>
    public event Action<string>? BackupSucceeded;

    /// <summary>يُطلَق لو فشل الـ Backup بسبب خطأ.</summary>
    public event Action<Exception>? BackupFailed;

    /// <summary>آخر حالة للعرض في الـ UI (يُحدَّث من Background Thread).</summary>
    public string LastStatusText { get; private set; } = "جاري التحقق...";

    // ─── Internals ────────────────────────────────────────────────────────
    private Timer? _timer;
    private bool _isRunning;

    // ملف JSON صغير يخزّن تاريخ آخر نسخة احتياطية
    private static readonly string StateFile = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "BloodBankAccounting",
        "backup_state.json");

    private AutoBackupService() { }

    // ══════════════════════════════════════════════════════════════════════
    // Start — يُستدعى مرة واحدة من App.xaml.cs بعد فتح الـ MainWindow
    // ══════════════════════════════════════════════════════════════════════
    public void Start()
    {
        // فحص فوري عند الإطلاق
        Task.Run(CheckAndBackupAsync);

        // ثم كل ساعة (يضمن اصطياد منتصف الليل لو التطبيق ظل شغّالاً)
        _timer = new Timer(
            callback: _ => Task.Run(CheckAndBackupAsync),
            state: null,
            dueTime: TimeSpan.FromHours(1),
            period: TimeSpan.FromHours(1));
    }

    // ══════════════════════════════════════════════════════════════════════
    // الـ Logic الأساسي
    // ══════════════════════════════════════════════════════════════════════
    private async Task CheckAndBackupAsync()
    {
        // منع التشغيل المتزامن لو الساعة دقت وهو لسه بيشتغل
        if (_isRunning) return;
        _isRunning = true;

        try
        {
            var lastDate = LoadLastBackupDate();

            // لو عملنا نسخة احتياطية النهارده قبل كده → مفيش حاجة
            if (lastDate.Date == DateTime.Today)
            {
                LastStatusText = $"آخر نسخة: {lastDate:hh:mm tt}";
                return;
            }

            LastStatusText = "جاري النسخ الاحتياطي...";

            var path = await DatabaseAdminService.BackupDatabaseAsync();

            // احفظ التاريخ بعد النجاح
            SaveLastBackupDate(DateTime.Now);
            LastStatusText = $"آخر نسخة: {DateTime.Now:hh:mm tt}";

            // احذف النسخ الأقدم من 30 يوم تلقائياً
            CleanOldBackups(daysToKeep: 30);

            BackupSucceeded?.Invoke(path);
        }
        catch (Exception ex)
        {
            LastStatusText = "⚠ فشل النسخ الاحتياطي";
            BackupFailed?.Invoke(ex);
        }
        finally
        {
            _isRunning = false;
        }
    }

    // ══════════════════════════════════════════════════════════════════════
    // State persistence
    // ══════════════════════════════════════════════════════════════════════
    private static DateTime LoadLastBackupDate()
    {
        try
        {
            if (!File.Exists(StateFile)) return DateTime.MinValue;
            var json = File.ReadAllText(StateFile);
            var state = JsonSerializer.Deserialize<BackupState>(json);
            return state?.LastBackupDate ?? DateTime.MinValue;
        }
        catch { return DateTime.MinValue; }
    }

    private static void SaveLastBackupDate(DateTime date)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(StateFile)!);
        var json = JsonSerializer.Serialize(new BackupState { LastBackupDate = date });
        File.WriteAllText(StateFile, json);
    }

    // ══════════════════════════════════════════════════════════════════════
    // حذف النسخ القديمة
    // ══════════════════════════════════════════════════════════════════════
    private static void CleanOldBackups(int daysToKeep)
    {
        try
        {
            var backupDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                "BloodBankAccounting", "Backups");

            if (!Directory.Exists(backupDir)) return;

            var cutoff = DateTime.Today.AddDays(-daysToKeep);

            foreach (var file in Directory.GetFiles(backupDir, "*.bak"))
            {
                if (File.GetCreationTime(file) < cutoff)
                    File.Delete(file);
            }
        }
        catch { /* صامت — حذف النسخ القديمة مش حاجة حرجة */ }
    }

    public void Dispose() => _timer?.Dispose();

    private sealed class BackupState
    {
        public DateTime LastBackupDate { get; set; }
    }
}