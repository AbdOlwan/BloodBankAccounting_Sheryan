namespace BloodBankAccounting.Models;

/// <summary>
/// كتالوج الأصناف — كل سجل يمثل صنفاً مستقلاً (اسم + سعر + حد طلب).
/// الفصيلة لا تنتمي لتعريف الصنف — تُحدَّد عند إدخال وحدات الدم في المخزون.
/// القيد الفريد: Name (مطبَّق على مستوى الكود وقاعدة البيانات).
/// </summary>
public class ProductCatalogItem
{
    public int Id { get; set; }

    /// <summary>اسم الصنف — مثل "كرات دم مكدسة"، "بلازما طازجة مجمدة"</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>سعر البيع الافتراضي — يُستخدم مرجعاً عند إنشاء الفواتير</summary>
    public decimal DefaultSalePrice { get; set; }

    /// <summary>الحد الأدنى للمخزون الذي يُفعِّل تنبيه إعادة الطلب — اختياري</summary>
    public int? ReorderLevel { get; set; }

    /// <summary>Soft delete — عند التعطيل لا يُعرض في القوائم لكن يبقى في السجلات التاريخية</summary>
    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.Now;
}