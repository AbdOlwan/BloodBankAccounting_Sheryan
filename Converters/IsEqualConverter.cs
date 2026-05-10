using System.Globalization;
using System.Windows.Data;

namespace BloodBankAccounting.Converters;

/// <summary>
/// يُرجع True لو القيمتان متساويتان — يُستخدم لتحديد الزر النشط في الـ Sidebar.
/// </summary>
public class IsEqualConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        => values.Length == 2 && Equals(values[0], values[1]);

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}