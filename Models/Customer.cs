namespace BloodBankAccounting.Models;

public class Customer
{
    public int    Id          { get; set; }
    public string Name        { get; set; } = string.Empty;
    public CustomerType Type  { get; set; }
    public string Phone       { get; set; } = string.Empty;
    public string? Address    { get; set; }
    public decimal Balance    { get; set; }          // موجب = مدين لنا
    public decimal CreditLimit { get; set; }
    public bool   IsActive    { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    // Navigation
    public ICollection<Invoice>        Invoices        { get; set; } = [];
    public ICollection<ReceiptVoucher> ReceiptVouchers { get; set; } = [];
}
