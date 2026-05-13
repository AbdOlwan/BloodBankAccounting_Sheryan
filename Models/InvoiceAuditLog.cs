namespace BloodBankAccounting.Models;

/// <summary>
/// سجل تدقيق تعديلات الفواتير — يُحفظ سجل دائم بكل تعديل:
/// متى تم، ما كانت القيم القديمة، وما أصبحت القيم الجديدة.
/// أضِف إلى AppDbContext: public DbSet&lt;InvoiceAuditLog&gt; InvoiceAuditLogs =&gt; Set&lt;InvoiceAuditLog&gt;();
/// ثم أضِف Migration جديدة.
/// </summary>
public class InvoiceAuditLog
{
    public int Id { get; set; }

    public int InvoiceId { get; set; }
    public Invoice Invoice { get; set; } = null!;

    /// <summary>وقت التعديل بالضبط</summary>
    public DateTime EditedAt { get; set; } = DateTime.Now;

    // ── القيم قبل التعديل ──────────────────────────────────────────
    public DateTime OldDate { get; set; }
    public decimal OldDiscountAmount { get; set; }
    public decimal OldTotalAmount { get; set; }
    public decimal OldPaidAmount { get; set; }
    public string OldNotes { get; set; } = string.Empty;

    // ── القيم بعد التعديل ──────────────────────────────────────────
    public DateTime NewDate { get; set; }
    public decimal NewDiscountAmount { get; set; }
    public decimal NewTotalAmount { get; set; }
    public decimal NewPaidAmount { get; set; }
    public string NewNotes { get; set; } = string.Empty;

    /// <summary>ملخص نصي للأثر المالي (يُعرض في التقارير المستقبلية)</summary>
    public string FinancialImpactSummary { get; set; } = string.Empty;
}