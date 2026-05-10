// MainWindow.xaml.cs
using BloodBankAccounting.ViewModels;
using System.Windows;
using System.Windows.Input;

namespace BloodBankAccounting.Views;

public partial class MainWindow : Window
{
    // ❌ احذف السطر ده تماماً:
    // private const string DebtReportPinHash = "03ac674216f3e15c761ee1a5e255f067953623c8b388b4459e13f978d7c846f4";

    
    private static readonly TimeSpan ClickWindow = TimeSpan.FromSeconds(3);

    private MainWindowViewModel? _vm;

    public MainWindow()
    {
        InitializeComponent();

        if (DataContext is MainWindowViewModel vm)
        {
            _vm = vm;
            vm.DebtReportAccessRequested += OnDebtReportAccessRequested;
        }
    }

    // ─── معالجة طلب الدخول لتقرير الديون ─────────────────────────────────
    private async void OnDebtReportAccessRequested()
    {
        if (_vm is null) return;

        // ✅ لو مفيش PIN مضبوط في DB — افتح مباشرة بدون حوار
        bool isProtected = await _vm.IsDebtReportProtectedAsync();
        if (!isProtected)
        {
            _vm.OpenDebtReport();
            return;
        }

        // ✅ اطلب الـ PIN
        var pin = PinDialog.Show(this);
        if (pin is null) return;  // ألغى المستخدم

        // ✅ تحقق من DB (نفس الـ hash اللي بيستخدمه Dashboard والخزينة)
        bool valid = await _vm.VerifyDebtReportPinAsync(pin);
        if (valid)
        {
            _vm.OpenDebtReport();
        }
        else
        {
            MessageBox.Show(
                "الرمز الذي أدخلته غير صحيح.\nليس لديك صلاحية الوصول إلى تقرير الديون.",
                "وصول مرفوض",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }
    }

    //internal void LogoBorder_MouseDown(object sender, MouseButtonEventArgs e)
    //{
    //    var now = DateTime.Now;

    //    if (_logoClickCount > 0 && (now - _firstClickTime) > ClickWindow)
    //        _logoClickCount = 0;

    //    if (_logoClickCount == 0)
    //        _firstClickTime = now;

    //    _logoClickCount++;

    //    if (_logoClickCount >= RequiredClicks)
    //    {
    //        _logoClickCount = 0;
    //        OpenAdminPanel();
    //    }
    //}

    //private void OpenAdminPanel()
    //{
    //    var pwdDialog = new AdminPasswordDialog();
    //    pwdDialog.ShowDialog();

    //    if (pwdDialog.Authenticated)
    //    {
    //        var panel = new SecretAdminPanel();
    //        panel.ShowDialog();
    //    }
    //}
}