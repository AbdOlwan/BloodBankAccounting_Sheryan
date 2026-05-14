namespace BloodBankAccounting.Models;

/// <summary>
/// رصيد المخزون المجمع لكل (صنف × فصيلة).
/// 
/// مبدأ التوحيد:
///   سواء أُدخلت الكمية يدويًا أو مستقبلًا عبر الباركود،
///   يتم تحديث نفس السجل دون إنشاء سجل جديد.
/// 
/// BloodGroup = null → بدون فصيلة (مثل بلازما، صفائح)
/// </summary>
public class BloodStock
{
    public int Id { get; set; }

    public int ProductId { get; set; }
    public BloodProduct Product { get; set; } = null!;

    /// <summary>null = بدون فصيلة</summary>
    public BloodGroup? BloodGroup { get; set; }

    public int Quantity { get; set; }
    public DateTime LastUpdated { get; set; } = DateTime.Now;
}

/// <summary>
/// كيان مؤقت في الذاكرة فقط — يُمثّل سطرًا في قائمة الإضافة قبل الحفظ الجماعي.
/// لا يُخزَّن في قاعدة البيانات.
/// </summary>
public class StockEntryDraft
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public BloodGroup? BloodGroup { get; set; }
    public int Quantity { get; set; }
}