using System.Windows;
using System.Windows.Input;

namespace BloodBankAccounting.Views;

public partial class MainWindow : Window
{
    // ─── عداد الضغطات السرية على اللوجو ──────────────────────────────────
    private int _logoClickCount;
    private DateTime _firstClickTime;
    private const int RequiredClicks = 5;
    private static readonly TimeSpan ClickWindow = TimeSpan.FromSeconds(3);

    public MainWindow() => InitializeComponent();

    // يُستدعى من MouseDown على اللوجو في MainWindow.xaml
    internal void LogoBorder_MouseDown(object sender, MouseButtonEventArgs e)
    {
        var now = DateTime.Now;

        // إعادة العداد لو انقضت المهلة
        if (_logoClickCount > 0 && (now - _firstClickTime) > ClickWindow)
            _logoClickCount = 0;

        if (_logoClickCount == 0)
            _firstClickTime = now;

        _logoClickCount++;

        if (_logoClickCount >= RequiredClicks)
        {
            _logoClickCount = 0;
            OpenAdminPanel();
        }
    }

    private void OpenAdminPanel()
    {
        var pwdDialog = new AdminPasswordDialog();
        pwdDialog.ShowDialog();

        if (pwdDialog.Authenticated)
        {
            var panel = new SecretAdminPanel();
            panel.ShowDialog();
        }
    }
}