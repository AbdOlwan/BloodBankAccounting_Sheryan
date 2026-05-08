using BloodBankAccounting.ViewModels;
using System.Windows;
using System.Windows.Controls;

namespace BloodBankAccounting.Views;

public partial class OrganizationSettingsView : UserControl
{
    public OrganizationSettingsView() => InitializeComponent();

    // PasswordBox لا يدعم Binding مباشر لأسباب أمنية — نمرر القيمة عبر Code-behind
    private void OnChangePinClick(object sender, RoutedEventArgs e)
    {
        if (DataContext is not OrgSettingsViewModel vm) return;
        vm.ChangePinFromView(CurrentPinBox.Password, NewPinBox.Password);
        CurrentPinBox.Clear();
        NewPinBox.Clear();
    }

    private void OnRemovePinClick(object sender, RoutedEventArgs e)
    {
        if (DataContext is not OrgSettingsViewModel vm) return;
        vm.RemovePinFromView(CurrentPinBox.Password);
        CurrentPinBox.Clear();
    }
}
