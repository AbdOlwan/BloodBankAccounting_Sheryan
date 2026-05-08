using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace BloodBankAccounting.Converters;

/// <summary>
/// يُحوّل bool → Visibility بشكل معكوس:
/// true  → Collapsed
/// false → Visible
/// مستخدم في TrialView لإخفاء زر "متابعة التجربة" عند انتهائها.
/// </summary>
public class InverseBoolToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is true ? Visibility.Collapsed : Visibility.Visible;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}