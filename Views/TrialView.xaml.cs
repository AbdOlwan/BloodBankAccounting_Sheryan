using BloodBankAccounting.ViewModels;
using System.Windows;

namespace BloodBankAccounting.Views;

public partial class TrialView : Window
{
    public bool Proceed { get; private set; }

    public TrialView()
    {
        InitializeComponent();
        var vm = new TrialViewModel();
        vm.ActivationSucceeded += () => { Proceed = true; Close(); };
        DataContext = vm;
    }
}
