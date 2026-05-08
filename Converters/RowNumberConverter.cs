using System.Globalization;
using System.Windows.Data;

namespace BloodBankAccounting.Converters;

public class RowNumberConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => value;
}