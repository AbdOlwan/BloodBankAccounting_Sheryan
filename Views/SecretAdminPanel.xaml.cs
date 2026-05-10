using BloodBankAccounting.Data;
using System.Windows;

namespace BloodBankAccounting.Views;

public partial class SecretAdminPanel : Window
{
    public SecretAdminPanel()
    {
        InitializeComponent();
    }

    // ─── Backup ───────────────────────────────────────────────────────────
    private async void BtnBackup_Click(object sender, RoutedEventArgs e)
    {
        SetStatus("جاري إنشاء النسخة الاحتياطية...", "#F39C12");
        BtnBackup.IsEnabled = false;

        try
        {
            var path = await DatabaseAdminService.BackupDatabaseAsync();
            SetStatus($"✓ تم الحفظ في:\n{path}", "#2ECC71");
        }
        catch (Exception ex)
        {
            SetStatus($"✗ فشل: {ex.Message}", "#E74C3C");
        }
        finally
        {
            BtnBackup.IsEnabled = true;
        }
    }

    // ─── Clear Data ───────────────────────────────────────────────────────
    private async void BtnClear_Click(object sender, RoutedEventArgs e)
    {
        var confirm = MessageBox.Show(
            "سيتم حذف كل البيانات نهائياً.\nهل أنت متأكد؟",
            "تأكيد المسح",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (confirm != MessageBoxResult.Yes) return;

        SetStatus("جاري مسح البيانات...", "#F39C12");
        BtnClear.IsEnabled = false;

        try
        {
            await DatabaseAdminService.ClearAllDataAsync();
            SetStatus("✓ تم مسح كل البيانات بنجاح.", "#2ECC71");
        }
        catch (Exception ex)
        {
            SetStatus($"✗ فشل: {ex.Message}", "#E74C3C");
        }
        finally
        {
            BtnClear.IsEnabled = true;
        }
    }

    // ─── Drop Database ────────────────────────────────────────────────────
    private async void BtnDrop_Click(object sender, RoutedEventArgs e)
    {
        // تأكيد مزدوج لأن العملية لا رجعة منها
        var first = MessageBox.Show(
            "⚠ تحذير: سيتم حذف قاعدة البيانات بالكامل من الـ SQL Server.\n" +
            "هذه العملية لا يمكن التراجع عنها.\n\nهل تريد المتابعة؟",
            "حذف قاعدة البيانات",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (first != MessageBoxResult.Yes) return;

        var second = MessageBox.Show(
            "تأكيد أخير: هل أنت متأكد 100% من حذف قاعدة البيانات؟",
            "تأكيد نهائي",
            MessageBoxButton.YesNo,
            MessageBoxImage.Stop);

        if (second != MessageBoxResult.Yes) return;

        SetStatus("جاري الحذف...", "#F39C12");
        BtnDrop.IsEnabled = false;

        try
        {
            await DatabaseAdminService.DropDatabaseAsync();
            SetStatus("✓ تم حذف قاعدة البيانات. أغلق التطبيق الآن.", "#2ECC71");

            // إغلاق التطبيق بعد 2 ثانية
            await Task.Delay(2000);
            Application.Current.Shutdown();
        }
        catch (Exception ex)
        {
            SetStatus($"✗ فشل: {ex.Message}", "#E74C3C");
            BtnDrop.IsEnabled = true;
        }
    }

    private void SetStatus(string msg, string color)
    {
        TxtStatus.Text = msg;
        TxtStatus.Foreground = new System.Windows.Media.BrushConverter()
            .ConvertFromString(color) as System.Windows.Media.Brush;
    }
}