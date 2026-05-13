namespace BloodBankAccounting.Models;

public class ElectronicPaymentDetail
{
    public int Id { get; set; }
    public ElectronicPaymentMethod Method { get; set; }
    public string FromAccountName { get; set; } = string.Empty;
    public string ToAccountName { get; set; } = string.Empty;
    public string? TransactionReference { get; set; }
    public DateTime TransactionDateTime { get; set; } = DateTime.Now;

    public int? InvoiceId { get; set; }
    public Invoice? Invoice { get; set; }
    public int? ReceiptVoucherId { get; set; }
    public ReceiptVoucher? ReceiptVoucher { get; set; }
}