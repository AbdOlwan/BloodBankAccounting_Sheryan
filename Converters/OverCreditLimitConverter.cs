using System.Globalization;
using System.Windows.Data;

namespace BloodBankAccounting.Converters;

public class OverCreditLimitConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values[0] is decimal balance && values[1] is decimal creditLimit)
            return creditLimit > 0 && balance > creditLimit;
        return false;
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}