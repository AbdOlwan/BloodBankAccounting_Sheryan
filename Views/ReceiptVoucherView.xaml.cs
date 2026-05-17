using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace BloodBankAccounting.Views;

public partial class ReceiptVoucherView : UserControl
{
    public ReceiptVoucherView() => InitializeComponent();

    private void ScrollViewer_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (sender is ScrollViewer sv && !e.Handled)
        {
            sv.ScrollToVerticalOffset(sv.VerticalOffset - e.Delta / 3.0);
            e.Handled = true;
        }
    }
}