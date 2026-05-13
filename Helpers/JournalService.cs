using BloodBankAccounting.Data;
using BloodBankAccounting.Models;

namespace BloodBankAccounting.Helpers;

public static class AccountCodes
{
    public const string Cash = "1001";   // الخزينة النقدية
    public const string Bank = "1002";   // البنك
    public const string Electronic = "1003";   // الخزينة الإلكترونية ← جديد
    public const string AccountsRec = "1100";   // ذمم مدينة (آجل)
    public const string BloodInventory = "1200";   // مخزون الدم
    public const string Sales = "4001";   // إيرادات المبيعات
    public const string COGS = "5001";   // تكلفة المبيعات
    public const string Expenses = "6001";   // المصاريف العمومية
}

public static class JournalService
{
    // ─── Helper: يستنتج كود الحساب من نوع الخزينة ─────────────
    public static string GetSafeAccountCode(Safe safe)
    {
        return safe.Type switch
        {
            SafeType.Electronic => AccountCodes.Electronic,
            SafeType.Bank => AccountCodes.Bank,
            _ => AccountCodes.Cash
        };
    }

    // ─── فاتورة نقدي أو إلكتروني ─────────────────────────────
    public static void PostInvoice(AppDbContext db, Invoice invoice, Safe safe)
    {
        var safeCode = GetSafeAccountCode(safe);
        var entries = new List<JournalEntry>();
        var date = invoice.Date;
        var refId = invoice.Id;

        if (invoice.PaymentType == PaymentType.Cash || invoice.PaymentType == PaymentType.Electronic)
        {
            entries.Add(Debit(date, safeCode, safe.Name, invoice.TotalAmount, "Invoice", refId, invoice.Id));
            entries.Add(Credit(date, AccountCodes.Sales, "إيرادات المبيعات", invoice.TotalAmount, "Invoice", refId, invoice.Id));
        }
        else // آجل أو جزئي – سيتم التعامل معه بشكل منفصل
        {
            entries.Add(Debit(date, AccountCodes.AccountsRec, $"ذمم - {invoice.Customer?.Name}", invoice.TotalAmount, "Invoice", refId, invoice.Id));
            entries.Add(Credit(date, AccountCodes.Sales, "إيرادات المبيعات", invoice.TotalAmount, "Invoice", refId, invoice.Id));
        }

        db.JournalEntries.AddRange(entries);
    }

    // ─── فاتورة جزئي ─────────────────────────────────────────
    public static void PostPartialInvoice(AppDbContext db, Invoice invoice, decimal initialPayment, Safe safe)
    {
        var safeCode = GetSafeAccountCode(safe);
        var remaining = invoice.TotalAmount - initialPayment;
        var date = invoice.Date;
        var refId = invoice.Id;
        var name = invoice.Customer?.Name ?? "";
        var entries = new List<JournalEntry>();

        if (initialPayment > 0)
            entries.Add(Debit(date, safeCode, safe.Name, initialPayment, "Invoice", refId, invoice.Id));

        if (remaining > 0)
            entries.Add(Debit(date, AccountCodes.AccountsRec, $"ذمم - {name}", remaining, "Invoice", refId, invoice.Id));

        entries.Add(Credit(date, AccountCodes.Sales, "إيرادات المبيعات", invoice.TotalAmount, "Invoice", refId, invoice.Id));

        db.JournalEntries.AddRange(entries);
    }

    // ─── سند قبض ─────────────────────────────────────────────
    public static void PostReceipt(AppDbContext db, ReceiptVoucher voucher, Safe safe)
    {
        var safeCode = GetSafeAccountCode(safe);
        var entries = new[]
        {
            Debit (voucher.Date, safeCode,          safe.Name,                          voucher.Amount, "Receipt", voucher.Id, receiptId: voucher.Id),
            Credit(voucher.Date, AccountCodes.AccountsRec, $"ذمم - {voucher.Customer?.Name}", voucher.Amount, "Receipt", voucher.Id, receiptId: voucher.Id)
        };
        db.JournalEntries.AddRange(entries);
    }

    // ─── سند دفع ─────────────────────────────────────────────
    public static void PostPayment(AppDbContext db, ReceiptVoucher voucher, Safe safe)
    {
        var safeCode = GetSafeAccountCode(safe);
        var payee = voucher.PayeeName ?? "—";
        var entries = new[]
        {
            Debit (voucher.Date, AccountCodes.Expenses, $"مصاريف - {payee}", voucher.Amount, "Payment", voucher.Id, receiptId: voucher.Id),
            Credit(voucher.Date, safeCode,              safe.Name,             voucher.Amount, "Payment", voucher.Id, receiptId: voucher.Id)
        };
        db.JournalEntries.AddRange(entries);
    }

    // ─── Helpers ──────────────────────────────────────────────
    private static JournalEntry Debit(DateTime date, string code, string name, decimal amount,
        string refType, int refId, int? invoiceId = null, int? receiptId = null)
        => new()
        {
            Date = date,
            AccountCode = code,
            AccountName = name,
            DebitAmount = amount,
            CreditAmount = 0,
            RefType = refType,
            RefId = refId,
            InvoiceId = invoiceId,
            ReceiptVoucherId = receiptId,
            CreatedAt = DateTime.Now
        };

    private static JournalEntry Credit(DateTime date, string code, string name, decimal amount,
        string refType, int refId, int? invoiceId = null, int? receiptId = null)
        => new()
        {
            Date = date,
            AccountCode = code,
            AccountName = name,
            DebitAmount = 0,
            CreditAmount = amount,
            RefType = refType,
            RefId = refId,
            InvoiceId = invoiceId,
            ReceiptVoucherId = receiptId,
            CreatedAt = DateTime.Now
        };
}