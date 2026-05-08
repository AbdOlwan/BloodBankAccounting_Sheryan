using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace BloodBankAccounting.Views;

public partial class InvoiceView : UserControl
{
    public InvoiceView()
    {
        InitializeComponent();

        // ??? ????? ??? ????? ??? ????? ???? ???????? ????????
        Loaded += (_, _) =>
        {
            TxtScanInvoice.Focus();
            ActivateInvoiceScanBorder();
        };

        TxtScanInvoice.GotFocus += (_, _) => ActivateInvoiceScanBorder();
        TxtScanInvoice.LostFocus += (_, _) => DeactivateInvoiceScanBorder();
    }

    // ??? ??? ?????? ???????? ????????????????????????????????
    private void InvoiceScanIcon_Click(object sender,
        System.Windows.Input.MouseButtonEventArgs e)
    {
        TxtScanInvoice.Focus();
        TxtScanInvoice.SelectAll();
    }

    private void ActivateInvoiceScanBorder()
    {
        InvoiceScanBorder.BorderBrush =
            new SolidColorBrush(Color.FromRgb(0xC0, 0x39, 0x2B));
        InvoiceScanBorder.BorderThickness = new Thickness(2);
        InvoiceScanIcon.Background =
            new SolidColorBrush(Color.FromRgb(0xC0, 0x39, 0x2B));
    }

    private void DeactivateInvoiceScanBorder()
    {
        InvoiceScanBorder.BorderBrush =
            new SolidColorBrush(Color.FromRgb(0xE5, 0xE8, 0xEC));
        InvoiceScanBorder.BorderThickness = new Thickness(1.5);
        InvoiceScanIcon.Background =
            new SolidColorBrush(Color.FromRgb(0x1A, 0x27, 0x44));
    }
}