using BloodBankAccounting.Data;
using BloodBankAccounting.Helpers;
using BloodBankAccounting.Models;
using BloodBankAccounting.Printing;
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

    // ─── Customer search ─────────────────────────────────────
    [ObservableProperty] private string _customerSearchText = string.Empty;
    [ObservableProperty] private bool _isSearchDropdownOpen;

    public ObservableCollection<Customer> Customers { get; } = [];
    public ObservableCollection<Safe> Safes { get; } = [];
    public ObservableCollection<Invoice> CustomerInvoices { get; } = [];
    public ObservableCollection<Customer> FilteredCustomers { get; } = [];

    private int? _savedVoucherId;
    public bool CanPrint => _savedVoucherId.HasValue;

    public ReceiptVoucherViewModel() { _ = LoadAsync(); }

    // ─── Filter as user types ─────────────────────────────────
    partial void OnCustomerSearchTextChanged(string value)
    {
        FilteredCustomers.Clear();

        if (string.IsNullOrWhiteSpace(value))
        {
            IsSearchDropdownOpen = false;
            // Clear selection if user erased the box
            if (SelectedCustomer is not null)
            {
                SelectedCustomer = null;
                CustomerInvoices.Clear();
                Amount = 0;
            }
            return;
        }

        var lower = value.Trim().ToLowerInvariant();
        var matches = Customers
            .Where(c => c.Name.ToLowerInvariant().Contains(lower))
            .Take(12);

        foreach (var c in matches) FilteredCustomers.Add(c);
        IsSearchDropdownOpen = FilteredCustomers.Count > 0;
    }

    // ─── Pick a customer from dropdown ───────────────────────
    [RelayCommand]
    private void SelectCustomer(Customer customer)
    {
        SelectedCustomer = customer;
        CustomerSearchText = customer.Name;
        IsSearchDropdownOpen = false;
    }

    // ─── Close dropdown from View code-behind ────────────────
    public void CloseDropdown() => IsSearchDropdownOpen = false;

    private async Task LoadAsync()
    {
        using var db = new AppDbContext();
        var customers = await db.Customers.Where(c => c.IsActive && c.Balance > 0)
                                          .OrderBy(c => c.Name).ToListAsync();
        var safes = await db.Safes.OrderBy(s => s.Name).ToListAsync();

        Customers.Clear(); customers.ForEach(Customers.Add);
        Safes.Clear(); safes.ForEach(Safes.Add);

        VoucherNumber = await NumberGenerator.NextVoucherNumberAsync(db);
        SelectedSafe = safes.FirstOrDefault(s => s.IsMain);
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
                     && i.PaidAmount < i.TotalAmount)
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
            using var db = new AppDbContext();
            await using var tx = await db.Database.BeginTransactionAsync();

            var voucher = new ReceiptVoucher
            {
                VoucherNumber = VoucherNumber,
                Date = Date,
                SafeId = SelectedSafe!.Id,
                CustomerId = SelectedCustomer!.Id,
                RelatedInvoiceId = SelectedInvoice?.Id,
                Amount = Amount,
                Notes = Notes
            };

            db.ReceiptVouchers.Add(voucher);
            await db.SaveChangesAsync();

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

            voucher.Customer = customer;
            JournalService.PostReceipt(db, voucher, SelectedSafe!);
            await db.SaveChangesAsync();

            await tx.CommitAsync();
            _savedVoucherId = voucher.Id;
            OnPropertyChanged(nameof(CanPrint));
            ShowSuccess($"تم حفظ سند القبض {VoucherNumber} ✓");

            await LoadAsync();
            SelectedCustomer = null;
            CustomerSearchText = string.Empty;
            IsSearchDropdownOpen = false;
            Amount = 0;
        }
        catch (Exception ex) { ShowError(ex.Message); }
        finally { IsBusy = false; }
    }

    private bool Validate()
    {
        if (SelectedCustomer is null) { ShowError("اختر العميل"); return false; }
        if (SelectedSafe is null) { ShowError("اختر الخزينة"); return false; }
        if (Amount <= 0) { ShowError("المبلغ يجب أن يكون أكبر من صفر"); return false; }
        return true;
    }

    [RelayCommand]
    private async Task PrintAsync()
    {
        if (!_savedVoucherId.HasValue) return;
        IsBusy = true;
        try { await ReceiptPrinter.PrintAsync(_savedVoucherId.Value); }
        catch (Exception ex) { ShowError(ex.Message); }
        finally { IsBusy = false; }
    }
}