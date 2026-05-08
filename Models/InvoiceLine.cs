namespace BloodBankAccounting.Models;

public class InvoiceLine
{
    public int Id        { get; set; }
    public string ProductName { get; set; } = string.Empty;

    public int InvoiceId { get; set; }
    public Invoice Invoice { get; set; } = null!;

    public int? BloodBagId { get; set; }
    public BloodBag? BloodBag { get; set; }

    public decimal UnitPrice { get; set; }
    public int Quantity      { get; set; } = 1;
    public decimal Total     { get; set; }
}
