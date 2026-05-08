using BloodBankAccounting.Data;
using BloodBankAccounting.Helpers;
using BloodBankAccounting.Models;
using BloodBankAccounting.ViewModels.Base;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using System.Collections.ObjectModel;

namespace BloodBankAccounting.ViewModels;

public partial class ReceiptVoucherViewModel : ViewModelBase
{
    [ObservableProperty] private string _voucherNumber = string.Empty;
    [ObservableProperty] private DateTime _date = DateTime.Today;
    [ObservableProperty] private decimal _amount;
    [ObservableProperty] private string? _notes;

    [ObservableProperty] private Customer? _selectedCustomer;
    [ObservableProperty] private Safe? _selectedSafe;
    [ObservableProperty] private Invoice? _selectedInvoice;

    public ObservableCollection<Customer> Customers        { get; } = [];
    public ObservableCollection<Safe>     Safes            { get; } = [];
    public ObservableCollection<Invoice>  CustomerInvoices { get; } = [];

    public ReceiptVoucherViewModel() { _ = LoadAsync(); }

    private async Task LoadAsync()
    {
        using var db  = new AppDbContext();
        var customers = await db.Customers.Where(c => c.IsActive && c.Balance > 0)
                                          .OrderBy(c => c.Name).ToListAsync();
        var safes     = await db.Safes.OrderBy(s => s.Name).ToListAsync();

        Customers.Clear(); customers.ForEach(Customers.Add);
        Safes.Clear();     safes.ForEach(Safes.Add);

        VoucherNumber = await NumberGenerator.NextVoucherNumberAsync(db);
        SelectedSafe  = safes.FirstOrDefault(s => s.IsMain);
    }

    partial void OnSelectedCustomerChanged(Customer? value)
    {
        _ = LoadCustomerInvoicesAsync(value);
    }

    private async Task LoadCustomerInvoicesAsync(Customer? customer)
    {
        CustomerInvoices.Clear();
        if (customer is null) return;

        using var db = new AppDbContext();
        var invoices = await db.Invoices
            .Where(i => i.CustomerId == customer.Id
                     && (i.PaymentType == PaymentType.Credit || i.PaymentType == PaymentType.Partial)
                     && !i.IsCancelled
                     && i.PaidAmount < i.TotalAmount)   // لا تزال عليه دفعات
            .OrderByDescending(i => i.Date)
            .ToListAsync();

        invoices.ForEach(CustomerInvoices.Add);
        Amount = customer.Balance;
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (!Validate()) return;
        IsBusy = true;
        try
        {
            using var db  = new AppDbContext();
            await using var tx = await db.Database.BeginTransactionAsync();

            var voucher = new ReceiptVoucher
            {
                VoucherNumber    = VoucherNumber,
                Date             = Date,
                SafeId           = SelectedSafe!.Id,
                CustomerId       = SelectedCustomer!.Id,
                RelatedInvoiceId = SelectedInvoice?.Id,
                Amount           = Amount,
                Notes            = Notes
            };

            db.ReceiptVouchers.Add(voucher);
            await db.SaveChangesAsync();

            // ─── Update balances ─────────────────────────────
            var customer = await db.Customers.FindAsync(SelectedCustomer!.Id);
            customer!.Balance -= Amount;

            var safe = await db.Safes.FindAsync(SelectedSafe!.Id);
            safe!.Balance += Amount;

            if (SelectedInvoice is not null)
            {
                var invoice = await db.Invoices.FindAsync(SelectedInvoice.Id);
                invoice!.PaidAmount += Amount;
            }

            await db.SaveChangesAsync();

            // ─── Journal ─────────────────────────────────────
            voucher.Customer = customer;
            JournalService.PostReceipt(db, voucher);
            await db.SaveChangesAsync();

            await tx.CommitAsync();
            ShowSuccess($"تم حفظ سند القبض {VoucherNumber} ✓");
            await LoadAsync();
            SelectedCustomer = null;
            Amount = 0;
        }
        catch (Exception ex) { ShowError(ex.Message); }
        finally { IsBusy = false; }
    }

    private bool Validate()
    {
        if (SelectedCustomer is null) { ShowError("اختر العميل"); return false; }
        if (SelectedSafe is null)     { ShowError("اختر الخزينة"); return false; }
        if (Amount <= 0)              { ShowError("المبلغ يجب أن يكون أكبر من صفر"); return false; }
        return true;
    }
}
