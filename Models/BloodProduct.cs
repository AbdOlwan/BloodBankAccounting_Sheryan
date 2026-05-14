namespace BloodBankAccounting.Models;

/// <summary>
/// صنف منتج الدم — مثل "كرات دم مكدسة"، "وحدة صفائح"
/// هو المصدر الوحيد للأصناف في كل أنحاء التطبيق:
///   - شاشة "إدارة الأصناف" تدير هذا الجدول
///   - شاشة "مخزون بنك الدم" تقرأ منه مباشرةً
/// </summary>
public class BloodProduct
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;

    /// <summary>كود ISBT — يُربط بقارئ الباركود مستقبلًا</summary>
    public string? IsbtCode { get; set; }

    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;

    // ─── حقول الكتالوج (تُدار من شاشة "إدارة الأصناف") ─────────────
    /// <summary>سعر البيع الافتراضي — يُستخدم مرجعًا عند إنشاء الفواتير</summary>
    public decimal DefaultSalePrice { get; set; }

    /// <summary>الحد الأدنى للمخزون الذي يُفعِّل تنبيه إعادة الطلب — اختياري</summary>
    public int? ReorderLevel { get; set; }

    // Navigation
    public ICollection<BloodStock> StockEntries { get; set; } = [];
}