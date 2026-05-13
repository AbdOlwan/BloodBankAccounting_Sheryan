using BloodBankAccounting.ViewModels;
using System.Windows;

namespace BloodBankAccounting.Views;

// لازم يكون
public partial class EditInvoiceWindow : Window
{
    private readonly EditInvoiceViewModel _vm;

    public EditInvoiceWindow(int invoiceId)
    {
        InitializeComponent();
        _vm = new EditInvoiceViewModel(invoiceId);
        DataContext = _vm;

        // زر الإلغاء — إغلاق بدون حفظ
        BtnCancel.Click += (_, _) =>
        {
            DialogResult = false;
            Close();
        };

        // بعد الحفظ الناجح → أغلق النافذة تلقائياً
        // نراقب IsBusy: لما يرجع false و Saved = true → أغلق
        _vm.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(EditInvoiceViewModel.IsBusy)
                && !_vm.IsBusy
                && _vm.Saved)
            {
                var timer = new System.Windows.Threading.DispatcherTimer
                {
                    Interval = TimeSpan.FromSeconds(0.5)
                };
                timer.Tick += (_, _) =>
                {
                    timer.Stop();
                    DialogResult = true;
                    Close();
                };
                timer.Start();
            }
        };

        // إغلاق بضغط Escape
        KeyDown += (_, e) =>
        {
            if (e.Key == System.Windows.Input.Key.Escape && !_vm.IsBusy)
            {
                DialogResult = false;
                Close();
            }
        };
    }

    /// <summary>
    /// فتح نافذة التعديل كـ Dialog.
    /// يُعيد true إذا تم الحفظ بنجاح، false إذا أُلغي.
    /// </summary>
    public static bool ShowEdit(int invoiceId, Window? owner = null)
    {
        var win = new EditInvoiceWindow(invoiceId);
        if (owner is not null) win.Owner = owner;
        return win.ShowDialog() == true && ((EditInvoiceViewModel)win.DataContext).Saved;
    }
}