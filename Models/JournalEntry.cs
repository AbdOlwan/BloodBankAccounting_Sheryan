namespace BloodBankAccounting.Models;

/// <summary>
/// قيد يومية — كل سطر = حساب واحد إما مدين أو دائن
/// الأزواج مترابطة عبر RefType + RefId
/// </summary>
public class JournalEntry
{
    public int    Id          { get; set; }
    public DateTime Date      { get; set; } = DateTime.Now;
    public string AccountCode { get; set; } = string.Empty;
    public string AccountName { get; set; } = string.Empty;
    public decimal DebitAmount  { get; set; }
    public decimal CreditAmount { get; set; }

    /// <summary>Invoice | Receipt | Payment</summary>
    public string RefType { get; set; } = string.Empty;
    public int    RefId   { get; set; }

    public int? InvoiceId { get; set; }
    public Invoice? Invoice { get; set; }

    public int? ReceiptVoucherId { get; set; }
    public ReceiptVoucher? ReceiptVoucher { get; set; }

    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}
