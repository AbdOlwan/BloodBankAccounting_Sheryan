namespace BloodBankAccounting.Models;

public class Safe
{
    public int      Id      { get; set; }
    public string   Name    { get; set; } = string.Empty;
    public SafeType Type    { get; set; }
    public decimal  Balance { get; set; }
    public bool     IsMain  { get; set; }   // الخزينة الرئيسية

    // Navigation
    public ICollection<ReceiptVoucher> ReceiptVouchers { get; set; } = [];
    public ICollection<Invoice>        Invoices        { get; set; } = [];
}
