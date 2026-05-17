using System.Windows.Controls;
using System.Windows.Input;

namespace BloodBankAccounting.Views;

public partial class CustomersView : UserControl
{
    public CustomersView() => InitializeComponent();

    // ???? ???? ?????? ??? ScrollViewer ??????? ??? ??????
    private void DataGrid_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (!e.Handled && OuterScroller != null)
        {
            OuterScroller.ScrollToVerticalOffset(
                OuterScroller.VerticalOffset - e.Delta);
            e.Handled = true;
        }
    }
}