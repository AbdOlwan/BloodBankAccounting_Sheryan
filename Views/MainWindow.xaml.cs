using BloodBankAccounting.ViewModels;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace BloodBankAccounting.Views;

public partial class MainWindow : Window
{
    private MainWindowViewModel? _vm;
    private readonly DispatcherTimer _collapseTimer;
    private bool _sidebarOpen = false;

    private const double ExpandedWidth = 220;
    private const double TriggerZone = 32;   // px من الحافة اليمنى تفتح الـ Sidebar

    public MainWindow()
    {
        InitializeComponent();

        _collapseTimer = new DispatcherTimer
        { Interval = TimeSpan.FromMilliseconds(380) };
        _collapseTimer.Tick += (_, _) =>
        {
            _collapseTimer.Stop();
            AnimateSidebar(false);
        };

        if (DataContext is MainWindowViewModel vm)
        {
            _vm = vm;
            vm.DebtReportAccessRequested += OnDebtReportAccessRequested;
            vm.PropertyChanged += OnVmPropertyChanged;
        }
    }

    // ─── Auto Sidebar ─────────────────────────────────────────────────────
    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);

        // FlowDirection=RTL → الـ Sidebar جهة اليمين → X قريب من ActualWidth
        double x = e.GetPosition(this).X;
        double zone = _sidebarOpen ? ExpandedWidth : TriggerZone;
        bool inZone = x > ActualWidth - zone;
        bool isPinned = _vm?.IsSidebarPinned ?? false;

        if (inZone)
        {
            _collapseTimer.Stop();
            if (!_sidebarOpen) AnimateSidebar(true);
        }
        else if (!isPinned && _sidebarOpen)
        {
            _collapseTimer.Start();
        }
    }

    private void OnVmPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(MainWindowViewModel.IsSidebarPinned)) return;

        if (_vm!.IsSidebarPinned)
        {
            _collapseTimer.Stop();
            AnimateSidebar(true);
        }
        // unpinned: الماوس هو اللي يتحكم في الإغلاق
    }

    private void AnimateSidebar(bool expand)
    {
        _sidebarOpen = expand;
        SidebarBorder.BeginAnimation(WidthProperty, new DoubleAnimation
        {
            To = expand ? ExpandedWidth : 0,
            Duration = TimeSpan.FromMilliseconds(210),
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseInOut }
        });
    }

    // ─── Debt Report PIN ─────────────────────────────────────────────────
    private async void OnDebtReportAccessRequested()
    {
        if (_vm is null) return;

        if (!await _vm.IsDebtReportProtectedAsync())
        { _vm.OpenDebtReport(); return; }

        var pin = PinDialog.Show(this);
        if (pin is null) return;

        if (await _vm.VerifyDebtReportPinAsync(pin))
            _vm.OpenDebtReport();
        else
            MessageBox.Show(
                "الرمز الذي أدخلته غير صحيح.\nليس لديك صلاحية الوصول إلى تقرير الديون.",
                "وصول مرفوض", MessageBoxButton.OK, MessageBoxImage.Warning);
    }
}