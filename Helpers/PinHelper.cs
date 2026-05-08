using System.Security.Cryptography;
using System.Text;

namespace BloodBankAccounting.Helpers;

public static class PinHelper
{
    /// <summary>تحويل الرمز السري إلى hash بـ SHA-256</summary>
    public static string Hash(string pin)
    {
        if (string.IsNullOrWhiteSpace(pin)) return string.Empty;
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(pin.Trim()));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    /// <summary>التحقق من صحة الرمز المُدخَل</summary>
    public static bool Verify(string pin, string storedHash)
        => !string.IsNullOrEmpty(storedHash) && Hash(pin) == storedHash;
}
