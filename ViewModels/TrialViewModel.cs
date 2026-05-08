using BloodBankAccounting.Helpers;
using BloodBankAccounting.ViewModels.Base;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Windows;
using System.Windows.Threading;

namespace BloodBankAccounting.ViewModels;

public partial class TrialViewModel : ViewModelBase
{
    [ObservableProperty] private string _activationKey = string.Empty;
    [ObservableProperty] private string _machineId;
    [ObservableProperty] private bool _hasError;

    // FIX: اسم مختلف عن StatusMessage في ViewModelBase لتفادي التعارض
    [ObservableProperty] private string _trialStatus = string.Empty;

    // FIX: نص زر النسخ — بيتغير لحظة الضغط ويرجع تاني بعد ثانيتين
    [ObservableProperty] private string _copyButtonText = "📋 نسخ";

    public int DaysRemaining { get; } = TrialManager.DaysRemaining();
    public bool IsExpired { get; } = TrialManager.DaysRemaining() == 0;

    public event Action? ActivationSucceeded;

    private readonly DispatcherTimer _copyResetTimer;

    public TrialViewModel()
    {
        _machineId = TrialManager.GetMachineId();

        _copyResetTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(2)
        };
        _copyResetTimer.Tick += (_, _) =>
        {
            CopyButtonText = "📋 نسخ";
            TrialStatus = string.Empty;
            _copyResetTimer.Stop();
        };
    }

    [RelayCommand]
    private void Activate()
    {
        HasError = false;
        TrialStatus = string.Empty;

        if (string.IsNullOrWhiteSpace(ActivationKey))
        {
            HasError = true;
            TrialStatus = "أدخل كود التفعيل";
            return;
        }

        if (TrialManager.Activate(ActivationKey))
        {
            MessageBox.Show(
                "✅ تم تفعيل البرنامج بنجاح!\nشكراً لثقتكم في شريان.",
                "تم التفعيل",
                MessageBoxButton.OK,
                MessageBoxImage.Information);

            ActivationSucceeded?.Invoke();
        }
        else
        {
            HasError = true;
            TrialStatus = "كود التفعيل غير صحيح. تأكد من الكود وحاول مجدداً.";
        }
    }

    [RelayCommand]
    private void CopyMachineId()
    {
        Clipboard.SetText(MachineId);

        // FIX: تغيير فوري لزر النسخ + رسالة نجاح خضراء
        CopyButtonText = "✅ تم";
        TrialStatus = "✓ تم نسخ معرّف الجهاز";
        HasError = false;

        _copyResetTimer.Stop();
        _copyResetTimer.Start();
    }

    [RelayCommand]
    private void ContinueTrial()
    {
        ActivationSucceeded?.Invoke();
    }
}