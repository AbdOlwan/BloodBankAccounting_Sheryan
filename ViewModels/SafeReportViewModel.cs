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

public partial class SafeReportViewModel : ViewModelBase
{
    // ════════════════════════════════════════════════════════════
    //  القفل المالي
    // ════════════════════════════════════════════════════════════
    [ObservableProperty] private bool _isFinancialUnlocked = true;
    private string _ownerPinHash = string.Empty;

    public string LockIcon => IsFinancialUnlocked ? "🔓" : "🔒";
    public string LockButtonText => IsFinancialUnlocked ? "إخفاء البيانات" : "عرض البيانات المالية";

    partial void OnIsFinancialUnlockedChanged(bool value)
    {
        OnPropertyChanged(nameof(LockIcon));
        OnPropertyChanged(nameof(LockButtonText));
    }

    [RelayCommand]
    private void ToggleFinancialLock()
    {
        if (IsFinancialUnlocked) { IsFinancialUnlocked = false; return; }

        if (string.IsNullOrEmpty(_ownerPinHash)) { IsFinancialUnlocked = true; return; }

        var enteredPin = Views.PinDialog.Show(System.Windows.Application.Current.MainWindow);
        if (enteredPin is null) return;

        if (PinHelper.Verify(enteredPin, _ownerPinHash))
            IsFinancialUnlocked = true;
        else
            ShowError("الرمز السري غير صحيح");
    }

    // ════════════════════════════════════════════════════════════
    //  بيانات الخزينة
    // ════════════════════════════════════════════════════════════
    public ObservableCollection<Safe> Safes { get; } = [];
    public ObservableCollection<SafeEntryRow> Entries { get; } = [];

    [ObservableProperty] private Safe? _selectedSafe;
    [ObservableProperty] private DateTime _from = DateTime.Today.AddMonths(-1);
    [ObservableProperty] private DateTime _to = DateTime.Today;

    [ObservableProperty] private decimal _openingBalance;
    [ObservableProperty] private decimal _closingBalance;
    [ObservableProperty] private decimal _totalDebit;
    [ObservableProperty] private decimal _totalCredit;

    public SafeReportViewModel() { _ = LoadAsync(); }

    private async Task LoadAsync()
    {
        using var db = new AppDbContext();
        var safes = await db.Safes.OrderBy(s => s.Name).ToListAsync();
        Safes.Clear();
        safes.ForEach(Safes.Add);
        SelectedSafe = safes.FirstOrDefault(s => s.IsMain) ?? safes.FirstOrDefault();

        var org = await db.OrgSettings.FirstOrDefaultAsync();
        _ownerPinHash = org?.OwnerPinHash ?? string.Empty;

        IsFinancialUnlocked = string.IsNullOrEmpty(_ownerPinHash);
    }

    partial void OnSelectedSafeChanged(Safe? value)
    {
        if (value is not null) _ = LoadStatementAsync();
    }

    partial void OnFromChanged(DateTime value) { if (SelectedSafe is not null) _ = LoadStatementAsync(); }
    partial void OnToChanged(DateTime value) { if (SelectedSafe is not null) _ = LoadStatementAsync(); }

    [RelayCommand]
    private async Task LoadStatementAsync()
    {
        if (SelectedSafe is null) return;

        using var db = new AppDbContext();

        // ← الإصلاح: نطابق الـ AccountCode الفعلي اللي بيحفظه JournalService
        var safeCode = SelectedSafe.Type == SafeType.Cash
            ? AccountCodes.Cash   // "1001"
            : AccountCodes.Bank;  // "1002"

        var beforeDebit = await db.JournalEntries
            .Where(j => j.Date < From && j.AccountCode == safeCode)
            .SumAsync(j => (decimal?)j.DebitAmount) ?? 0;

        var beforeCredit = await db.JournalEntries
            .Where(j => j.Date < From && j.AccountCode == safeCode)
            .SumAsync(j => (decimal?)j.CreditAmount) ?? 0;

        OpeningBalance = beforeDebit - beforeCredit;

        var entries = await db.JournalEntries
            .Where(j => j.Date >= From && j.Date <= To && j.AccountCode == safeCode)
            .OrderBy(j => j.Date)
            .ToListAsync();

        Entries.Clear();
        var runningBalance = OpeningBalance;

        foreach (var e in entries)
        {
            runningBalance += e.DebitAmount - e.CreditAmount;
            Entries.Add(new SafeEntryRow
            {
                Date = e.Date,
                RefType = e.RefType,        // ← مهم للطباعة الفردية
                RefId = e.RefId,          // ← مهم للطباعة الفردية
                RefTypeDisplay = e.RefType switch
                {
                    "Invoice" => "فاتورة",
                    "Receipt" => "قبض",
                    "Payment" => "صرف",
                    _ => e.RefType
                },
                Description = e.AccountName,
                Debit = e.DebitAmount,
                Credit = e.CreditAmount,
                Balance = runningBalance
            });
        }

        TotalDebit = entries.Sum(e => e.DebitAmount);
        TotalCredit = entries.Sum(e => e.CreditAmount);
        ClosingBalance = runningBalance;
    }

    // ════════════════════════════════════════════════════════════
    //  طباعة الكشف كله
    // ════════════════════════════════════════════════════════════
    [RelayCommand]
    private async Task PrintAsync()
    {
        if (SelectedSafe is null) { ShowError("اختر الخزينة أولاً"); return; }
        IsBusy = true;
        try
        {
            await SafeStatementPrinter.PrintAsync(
                SelectedSafe.Name,
                From, To,
                OpeningBalance, ClosingBalance,
                TotalDebit, TotalCredit,
                Entries.ToList());
        }
        finally { IsBusy = false; }
    }

    // ════════════════════════════════════════════════════════════
    //  طباعة حركة واحدة بعينها
    // ════════════════════════════════════════════════════════════
    [RelayCommand]
    private async Task PrintEntryAsync(SafeEntryRow entry)
    {
        if (entry is null) return;
        IsBusy = true;
        try
        {
            switch (entry.RefType)
            {
                case "Invoice":
                    await InvoicePrinter.PrintAsync(entry.RefId);
                    break;
                case "Receipt":
                    await ReceiptPrinter.PrintAsync(entry.RefId);
                    break;
                case "Payment":
                    await PaymentPrinter.PrintAsync(entry.RefId);
                    break;
                default:
                    ShowError($"لا توجد طباعة متاحة لهذا النوع: {entry.RefTypeDisplay}");
                    break;
            }
        }
        catch (Exception ex) { ShowError($"خطأ في الطباعة:\n{ex.Message}"); }
        finally { IsBusy = false; }
    }
}

// ════════════════════════════════════════════════════════════
//  صف كشف الحساب
// ════════════════════════════════════════════════════════════
public class SafeEntryRow
{
    public DateTime Date { get; set; }
    public string RefType { get; set; } = string.Empty;   // ← Invoice / Receipt / Payment
    public int RefId { get; set; }                   // ← Id الوثيقة الأصلية
    public string RefTypeDisplay { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Debit { get; set; }
    public decimal Credit { get; set; }
    public decimal Balance { get; set; }
}
