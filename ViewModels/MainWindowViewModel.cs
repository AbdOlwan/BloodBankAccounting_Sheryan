using BloodBankAccounting.ViewModels.Base;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using static BloodBankAccounting.ViewModels.OrgSettingsViewModel;

namespace BloodBankAccounting.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    [ObservableProperty] private ViewModelBase? _currentPage;
    [ObservableProperty] private string _currentPageTitle = "الرئيسية";

    [RelayCommand] private void NavigateToDashboard() { CurrentPage = new DashboardViewModel(); CurrentPageTitle = "لوحة التحكم"; }
    [RelayCommand] private void NavigateToInvoice() { CurrentPage = new InvoiceViewModel(); CurrentPageTitle = "فاتورة مبيعات جديدة"; }
    [RelayCommand] private void NavigateToInvoiceList() { CurrentPage = new InvoiceListViewModel(); CurrentPageTitle = "سجل الفواتير"; }
    [RelayCommand] private void NavigateToReceipt() { CurrentPage = new ReceiptVoucherViewModel(); CurrentPageTitle = "سند قبض جديد"; }
    [RelayCommand] private void NavigateToPayment() { CurrentPage = new PaymentVoucherViewModel(); CurrentPageTitle = "سند دفع جديد"; }
    [RelayCommand] private void NavigateToCustomers() { CurrentPage = new CustomersViewModel(); CurrentPageTitle = "إدارة العملاء"; }
    [RelayCommand] private void NavigateToBloodInventory() { CurrentPage = new BloodInventoryViewModel(); CurrentPageTitle = "مخزون الدم"; }
    [RelayCommand] private void NavigateToDebtReport() { CurrentPage = new DebtReportViewModel(); CurrentPageTitle = "تقرير الديون"; }
    [RelayCommand] private void NavigateToSafeReport() { CurrentPage = new SafeReportViewModel(); CurrentPageTitle = "الخزينة"; }
    [RelayCommand] private void NavigateToOrgSettings() { CurrentPage = new OrgSettingsViewModel(); CurrentPageTitle = "إعدادات المؤسسة"; }

    public MainWindowViewModel() => NavigateToDashboard();
}