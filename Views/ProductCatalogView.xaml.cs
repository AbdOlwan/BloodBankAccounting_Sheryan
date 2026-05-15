using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace BloodBankAccounting.Views;

public partial class ProductCatalogView : UserControl
{
    public ProductCatalogView() => InitializeComponent();

    private void OuterScroll_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        var inner = FindScrollViewer(e.OriginalSource as DependencyObject);
        if (inner is null || inner == OuterScroll) return;

        inner.ScrollToVerticalOffset(inner.VerticalOffset - e.Delta / 3.0);
        e.Handled = true;
    }

    private static ScrollViewer? FindScrollViewer(DependencyObject? element)
    {
        while (element is not null)
        {
            if (element is ScrollViewer sv) return sv;
            element = VisualTreeHelper.GetParent(element);
        }
        return null;
    }
}