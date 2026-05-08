namespace BloodBankAccounting.Helpers;

/// <summary>
/// تحويل الأرقام إلى كلمات عربية لطباعة سندات القبض والدفع.
/// يدعم حتى 999,999,999.99
/// </summary>
public static class ArabicNumberHelper
{
    private static readonly string[] Units =
    [
        "", "واحد", "اثنان", "ثلاثة", "أربعة", "خمسة",
        "ستة", "سبعة", "ثمانية", "تسعة", "عشرة",
        "أحد عشر", "اثنا عشر", "ثلاثة عشر", "أربعة عشر", "خمسة عشر",
        "ستة عشر", "سبعة عشر", "ثمانية عشر", "تسعة عشر"
    ];

    private static readonly string[] Tens =
    [
        "", "عشرة", "عشرون", "ثلاثون", "أربعون", "خمسون",
        "ستون", "سبعون", "ثمانون", "تسعون"
    ];

    private static readonly string[] Hundreds =
    [
        "", "مائة", "مئتان", "ثلاثمائة", "أربعمائة", "خمسمائة",
        "ستمائة", "سبعمائة", "ثمانمائة", "تسعمائة"
    ];

    /// <summary>
    /// مثال: 1350.50 → "ألف وثلاثمائة وخمسون جنيهًا وخمسون قرشًا فقط لا غير"
    /// </summary>
    public static string ToWords(decimal amount)
    {
        if (amount == 0) return "صفر جنيه فقط لا غير";

        var whole = (long)Math.Truncate(amount);
        var fraction = (int)Math.Round((amount - whole) * 100);

        var result = ConvertWholeNumber(whole) + " جنيه";
        if (fraction > 0)
            result += $" و{ConvertWholeNumber(fraction)} قرش";

        return result + " فقط لا غير";
    }

    private static string ConvertWholeNumber(long n)
    {
        if (n <= 0) return "صفر";

        var parts = new List<string>();

        if (n >= 1_000_000)
        {
            var m = n / 1_000_000;
            n %= 1_000_000;
            parts.Add(ConvertChunk((int)m) + (m == 1 ? " مليون" : m == 2 ? " مليونان" : " مليون"));
        }

        if (n >= 1_000)
        {
            var t = n / 1_000;
            n %= 1_000;
            if (t == 1) parts.Add("ألف");
            else if (t == 2) parts.Add("ألفان");
            else if (t <= 10) parts.Add(ConvertChunk((int)t) + " آلاف");
            else parts.Add(ConvertChunk((int)t) + " ألف");
        }

        if (n > 0) parts.Add(ConvertChunk((int)n));

        return string.Join(" و", parts);
    }

    private static string ConvertChunk(int n)
    {
        if (n == 0) return "";

        var parts = new List<string>();

        int h = n / 100;
        if (h > 0) { parts.Add(Hundreds[h]); n %= 100; }

        if (n >= 20)
        {
            int t = n / 10;
            int u = n % 10;
            parts.Add(u > 0 ? Units[u] + " و" + Tens[t] : Tens[t]);
        }
        else if (n > 0)
        {
            parts.Add(Units[n]);
        }

        return string.Join(" و", parts);
    }
}