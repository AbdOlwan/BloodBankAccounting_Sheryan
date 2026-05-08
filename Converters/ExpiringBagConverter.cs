using System.Globalization;
using System.Windows.Data;
using BloodBankAccounting.Models;

namespace BloodBankAccounting.Converters;

public class ExpiringBagConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values[0] is DateTime expiry && values[1] is BloodBagStatus status)
            return status == BloodBagStatus.Available && expiry <= DateTime.Now.AddDays(7);
        return false;
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}