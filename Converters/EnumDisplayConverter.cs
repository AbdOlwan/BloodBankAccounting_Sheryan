// Converters/EnumDisplayConverter.cs
using System;
using System.Globalization;
using System.Windows.Data;
using BloodBankAccounting.Models;

namespace BloodBankAccounting.Converters;

public class EnumDisplayConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value switch
        {
            // CustomerType
            CustomerType.Hospital => "مستشفى",
            CustomerType.Person => "فرد",

            // PaymentType
            PaymentType.Cash => "نقدي",
            PaymentType.Credit => "آجل",
            PaymentType.Partial => "جزئي",

            // SafeType
            SafeType.Cash => "خزينة نقدية",
            SafeType.Bank => "بنك",

            // BloodBagStatus
            BloodBagStatus.Available => "متاح",
            BloodBagStatus.Sold => "مباع",
            BloodBagStatus.Expired => "منتهي الصلاحية",
            BloodBagStatus.Damaged => "تالف",
            BloodBagStatus.Reserved => "محجوز",

            // VoucherType
            VoucherType.Receipt => "سند قبض",
            VoucherType.Payment => "سند صرف",

            // BloodGroup
            BloodGroup.APositive => "A+",
            BloodGroup.ANegative => "A-",
            BloodGroup.BPositive => "B+",
            BloodGroup.BNegative => "B-",
            BloodGroup.ABPositive => "AB+",
            BloodGroup.ABNegative => "AB-",
            BloodGroup.OPositive => "O+",
            BloodGroup.ONegative => "O-",

            _ => value?.ToString() ?? string.Empty
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}


//...............
