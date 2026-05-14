using BloodBankAccounting.Data;
using BloodBankAccounting.Helpers;
using BloodBankAccounting.ViewModels.Base;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using System.Windows.Threading;

namespace BloodBankAccounting.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    [ObservableProperty] private ViewModelBase? _currentPage;
    [ObservableProperty] private string _currentPageTitle = "الرئيسية";
    [ObservableProperty] private string _activePage = string.Empty;
    [ObservableProperty] private string _backupStatus = "جاري التحقق...";

    // ─── حدث طلب الدخول لتقرير الديون ───────────────────────────────────
    public event Action? DebtReportAccessRequested;

    // ─── Navigation ───────────────────────────────────────────────────────
    [RelayCommand]
    private void NavigateToDashboard()
    {
        CurrentPage = new DashboardViewModel();
        CurrentPageTitle = "لوحة التحكم";
        ActivePage = "Dashboard";
    }

    [RelayCommand]
    private void NavigateToInvoice()
    {
        CurrentPage = new InvoiceViewModel();
        CurrentPageTitle = "فاتورة مبيعات جديدة";
        ActivePage = "Invoice";
    }

    [RelayCommand]
    private void NavigateToInvoiceList()
    {
        CurrentPage = new InvoiceListViewModel();
        CurrentPageTitle = "سجل الفواتير";
        ActivePage = "InvoiceList";
    }

    [RelayCommand]
    private void NavigateToReceipt()
    {
        CurrentPage = new ReceiptVoucherViewModel();
        CurrentPageTitle = "سند قبض جديد";
        ActivePage = "Receipt";
    }

    [RelayCommand]
    private void NavigateToPayment()
    {
        CurrentPage = new PaymentVoucherViewModel();
        CurrentPageTitle = "سند دفع جديد";
        ActivePage = "Payment";
    }

    [RelayCommand]
    private void NavigateToCustomers()
    {
        CurrentPage = new CustomersViewModel();
        CurrentPageTitle = "إدارة العملاء";
        ActivePage = "Customers";
    }

    [RelayCommand]
    private void NavigateToBloodInventory()
    {
        CurrentPage = new BloodInventoryViewModel();
        CurrentPageTitle = "مخزون الدم";
        ActivePage = "BloodInventory";
    }

    [RelayCommand]
    private void NavigateToProductCatalog()
    {
        CurrentPage = new ProductCatalogViewModel();
        CurrentPageTitle = "إدارة الأصناف";
        ActivePage = "ProductCatalog";
    }

    /// <summary>
    /// لا ينتقل مباشرة — يُطلق الحدث كي يتحقق MainWindow من الـ PIN أولاً.
    /// </summary>
    [RelayCommand]
    private void NavigateToDebtReport() => DebtReportAccessRequested?.Invoke();

    /// <summary>يُستدعى من MainWindow.xaml.cs بعد التحقق الناجح من الـ PIN.</summary>
    public void OpenDebtReport()
    {
        CurrentPage = new DebtReportViewModel();
        CurrentPageTitle = "تقرير الديون";
        ActivePage = "DebtReport";
    }

    [RelayCommand]
    private void NavigateToSafeReport()
    {
        CurrentPage = new SafeReportViewModel();
        CurrentPageTitle = "الخزينة";
        ActivePage = "SafeReport";
    }

    [RelayCommand]
    private void NavigateToOrgSettings()
    {
        CurrentPage = new OrgSettingsViewModel();
        CurrentPageTitle = "إعدادات المؤسسة";
        ActivePage = "OrgSettings";
    }

    // ─── التحقق من الـ PIN — يقرأ من DB في كل مرة ───────────────────────
    //
    // لماذا DB مباشرة وليس cache؟
    //   لو العميل غيّر الـ PIN من شاشة الإعدادات أثناء نفس الجلسة،
    //   يُطبَّق الرمز الجديد فوراً بدون إعادة تشغيل التطبيق.

    /// <summary>هل يوجد رمز سري مضبوط؟ (OwnerPinHash غير فارغ في DB)</summary>
    public async Task<bool> IsDebtReportProtectedAsync()
    {
        using var db = new AppDbContext();
        var org = await db.OrgSettings.FirstOrDefaultAsync();
        return !string.IsNullOrEmpty(org?.OwnerPinHash);
    }

    /// <summary>يتحقق من صحة الرمز المُدخَل عبر PinHelper.Verify.</summary>
    public async Task<bool> VerifyDebtReportPinAsync(string pin)
    {
        using var db = new AppDbContext();
        var org = await db.OrgSettings.FirstOrDefaultAsync();
        return PinHelper.Verify(pin, org?.OwnerPinHash ?? string.Empty);
    }

    // ─── Backup ───────────────────────────────────────────────────────────
    public MainWindowViewModel()
    {
        NavigateToDashboard();
    }

   
}