namespace BloodBankAccounting.Models;

public class ReceiptVoucher
{
    public int    Id            { get; set; }
    public string PayeeName { get; set; } = string.Empty;

    public string VoucherNumber { get; set; } = string.Empty;
    public VoucherType Type     { get; set; } = VoucherType.Receipt;
    public DateTime Date        { get; set; } = DateTime.Now;

    public int SafeId   { get; set; }
    public Safe Safe    { get; set; } = null!;

    public int? CustomerId    { get; set; }
    public Customer? Customer { get; set; }

    public int? RelatedInvoiceId { get; set; }
    public Invoice? RelatedInvoice { get; set; }

    public decimal Amount { get; set; }
    public string? Notes  { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public ICollection<JournalEntry> JournalEntries { get; set; } = [];
}
