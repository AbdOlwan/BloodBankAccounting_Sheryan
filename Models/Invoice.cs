namespace BloodBankAccounting.Models;


public class Invoice
{
    public int Id { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public DateTime Date { get; set; } = DateTime.Now;
    public PaymentType PaymentType { get; set; }

    public int CustomerId { get; set; }
    public Customer Customer { get; set; } = null!;

    public int? SafeId { get; set; }
    public Safe? Safe { get; set; }

    public decimal SubTotal { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal PaidAmount { get; set; }

    // ← جديد: المتبقي = الإجمالي - المدفوع
    public decimal RemainingAmount => TotalAmount - PaidAmount;

    public bool IsCancelled { get; set; }
    public string? CancelReason { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public ICollection<InvoiceLine> Lines { get; set; } = [];
    public ICollection<ReceiptVoucher> ReceiptVouchers { get; set; } = [];
    public ICollection<JournalEntry> JournalEntries { get; set; } = [];
    public ElectronicPaymentDetail? ElectronicDetail { get; set; }
}
