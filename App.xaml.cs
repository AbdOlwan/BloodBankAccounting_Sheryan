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

        // ─── FIX: منع الـ WPF من إغلاق التطبيق لما TrialView يتقفل ────────
        // السبب: ShutdownMode="OnMainWindowClose" في App.xaml بيخلي WPF
        // يعتبر TrialView هو الـ MainWindow (أول window يتفتح) وبيقفل
        // التطبيق فور ما بيتقفل — قبل ما MainWindow يتعرض.
        ShutdownMode = ShutdownMode.OnExplicitShutdown;

        QuestPDF.Settings.License = LicenseType.Community;

        // ─── 1. Trial / License check ─────────────────────────
        if (!TrialManager.IsActivated())
        {
            var trial = new TrialView();
            trial.ShowDialog();
            if (!trial.Proceed) { Shutdown(); return; }
        }

        // ─── 2. Database ──────────────────────────────────────
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

        // ─── 3. Open Main Window ──────────────────────────────
        var main = new MainWindow();
        MainWindow = main;

        // ─── FIX: الآن نرجع الـ ShutdownMode الطبيعي بعد ما عيّنا MainWindow ─
        ShutdownMode = ShutdownMode.OnMainWindowClose;

        main.Show();
    }
}