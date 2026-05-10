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
            await db.Database.MigrateAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"فشل الاتصال بقاعدة البيانات:\n{ex.Message}",
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

    // ─── عرض إشعار صامت لو فشل الـ Backup (اختياري) ──────────────────

    protected override void OnExit(ExitEventArgs e)
    {
        base.OnExit(e);
    }
}