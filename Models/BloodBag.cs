namespace BloodBankAccounting.Models;

public class BloodBag
{
    public int    Id         { get; set; }
    public string BagNumber  { get; set; } = string.Empty;   // رقم الكيس
    public BloodGroup BloodGroup { get; set; }
    public decimal CostPrice { get; set; }
    public decimal SalePrice { get; set; }
    public DateTime ExpiryDate  { get; set; }
    public DateTime EntryDate   { get; set; } = DateTime.Now;
    public BloodBagStatus Status { get; set; } = BloodBagStatus.Available;
    public string? Notes     { get; set; }

    // Navigation
    public ICollection<InvoiceLine> InvoiceLines { get; set; } = [];
}
