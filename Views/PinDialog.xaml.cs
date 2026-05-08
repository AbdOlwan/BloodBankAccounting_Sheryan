using System.Windows;
using System.Windows.Input;

namespace BloodBankAccounting.Views;

public partial class PinDialog : Window
{
    public string EnteredPin { get; private set; } = string.Empty;

    public PinDialog()
    {
        InitializeComponent();
        OkBtn.Click += (_, _) => Confirm();
        CancelBtn.Click += (_, _) => { DialogResult = false; Close(); };
        PinBox.KeyDown += (_, e) => { if (e.Key == Key.Return) Confirm(); };
        Loaded += (_, _) => PinBox.Focus();

        // السحب من الشريط الفارغ
        MouseLeftButtonDown += (_, e) => { if (e.ButtonState == MouseButtonState.Pressed) DragMove(); };
    }

    private void Confirm()
    {
        ErrorText.Visibility = Visibility.Hidden;   // Hidden لا Collapsed للحفاظ على المساحة
        EnteredPin = PinBox.Password;
        DialogResult = true;
        Close();
    }

    /// <summary>
    /// عرض الحوار وإرجاع الرمز المُدخَل، أو null عند الإلغاء.
    /// </summary>
    public static string? Show(Window? owner = null)
    {
        var dlg = new PinDialog();
        if (owner is not null) dlg.Owner = owner;
        return dlg.ShowDialog() == true ? dlg.EnteredPin : null;
    }

    /// <summary>يُظهر رسالة الخطأ ويمسح الحقل (للاستخدام الخارجي)</summary>
    public void ShowError()
    {
        ErrorText.Visibility = Visibility.Visible;
        PinBox.Clear();
        PinBox.Focus();
    }
}