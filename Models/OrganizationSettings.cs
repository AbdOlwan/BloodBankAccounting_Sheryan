namespace BloodBankAccounting.Models;

public class OrganizationSettings
{
    public int Id { get; set; } = 15;
    public string BankName { get; set; } = "بنك الدم";
    public string SubTitle { get; set; } = "إدارة المحاسبة والمستودعات";
    public string Address { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string? LogoPath { get; set; }           // مسار ملف الشعار
    public string OwnerPinHash { get; set; } = string.Empty; // SHA-256
}
