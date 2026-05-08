using BloodBankAccounting.Data;
using BloodBankAccounting.Models;
using Microsoft.EntityFrameworkCore;

namespace BloodBankAccounting.Helpers;

public static class NumberGenerator
{
    public static async Task<string> NextInvoiceNumberAsync(AppDbContext db)
    {
        var year = DateTime.Now.Year;
        var count = await db.Invoices.CountAsync(x => x.CreatedAt.Year == year);
        return $"INV-{year}-{(count + 1):D5}";
    }

    /// <summary>??? ???</summary>
    public static async Task<string> NextVoucherNumberAsync(AppDbContext db)
    {
        var year = DateTime.Now.Year;
        var count = await db.ReceiptVouchers
            .CountAsync(x => x.Type == VoucherType.Receipt && x.CreatedAt.Year == year);
        return $"RCV-{year}-{(count + 1):D5}";
    }

    /// <summary>??? ???</summary>
    public static async Task<string> NextPaymentVoucherNumberAsync(AppDbContext db)
    {
        var year = DateTime.Now.Year;
        var count = await db.ReceiptVouchers
            .CountAsync(x => x.Type == VoucherType.Payment && x.CreatedAt.Year == year);
        return $"PAY-{year}-{(count + 1):D5}";
    }
}