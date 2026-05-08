using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Windows;

namespace BloodBankAccounting.ViewModels.Base;

public abstract partial class ViewModelBase : ObservableObject
{
    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    protected static void ShowError(string message)
        => MessageBox.Show(message, "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);

    protected static void ShowSuccess(string message)
        => MessageBox.Show(message, "تم", MessageBoxButton.OK, MessageBoxImage.Information);

    protected static bool Confirm(string message)
        => MessageBox.Show(message, "تأكيد", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes;
}
