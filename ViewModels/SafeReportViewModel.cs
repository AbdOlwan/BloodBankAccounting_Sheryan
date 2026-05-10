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

    /// <summary>جميع الحركات المحمّلة من DB (المصدر الكامل)</summary>
    private readonly ObservableCollection<SafeEntryRow> _allEntries = [];

    /// <summary>الحركات المعروضة في الجدول بعد تطبيق البحث النصي</summary>
    public ObservableCollection<SafeEntryRow> Entries { get; } = [];

    [ObservableProperty] private Safe? _selectedSafe;

    // ── النطاق الزمني — الافتراضي: آخر يوم واحد فقط ──────────────────
    [ObservableProperty] private DateTime _from = DateTime.Today;
    [ObservableProperty] private DateTime _to = DateTime.Today;

    [ObservableProperty] private decimal _openingBalance;
    [ObservableProperty] private decimal _closingBalance;
    [ObservableProperty] private decimal _totalDebit;
    [ObservableProperty] private decimal _totalCredit;

    // ════════════════════════════════════════════════════════════
    //  البحث النصي
    // ════════════════════════════════════════════════════════════
    [ObservableProperty] private string _searchText = string.Empty;

    /// <summary>عدد النتائج بعد الفلترة — يُعرض في الـ UI</summary>
    public int FilteredCount => Entries.Count;

    partial void OnSearchTextChanged(string value) => ApplySearch();

    /// <summary>
    /// يُفلتر _allEntries بناءً على SearchText ويُحدّث Entries.
    /// البحث يشمل: التاريخ، النوع، البيان، المبالغ.
    /// </summary>
    private void ApplySearch()
    {
        var term = SearchText?.Trim() ?? string.Empty;

        Entries.Clear();

        var filtered = string.IsNullOrEmpty(term)
            ? _allEntries
            : _allEntries.Where(e =>
                e.Date.ToString("d/M/yyyy").Contains(term) ||
                e.RefTypeDisplay.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                e.Description.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                e.Debit.ToString("N2").Contains(term) ||
                e.Credit.ToString("N2").Contains(term) ||
                e.Balance.ToString("N2").Contains(term));

        foreach (var row in filtered)
            Entries.Add(row);

        OnPropertyChanged(nameof(FilteredCount));
    }

    [RelayCommand]
    private void ClearSearch()
    {
        SearchText = string.Empty;   // OnSearchTextChanged → ApplySearch
    }

    // ════════════════════════════════════════════════════════════
    //  تحميل
    // ════════════════════════════════════════════════════════════
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

    // ── التاريخ لا يُحمّل تلقائياً — فقط عبر زر "عرض" ──────────────────
    // (تم حذف partial void OnFromChanged / OnToChanged العتيقتين)

    [RelayCommand]
    private async Task LoadStatementAsync()
    {
        if (SelectedSafe is null) return;

        using var db = new AppDbContext();

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

        _allEntries.Clear();
        var runningBalance = OpeningBalance;

        foreach (var e in entries)
        {
            runningBalance += e.DebitAmount - e.CreditAmount;
            _allEntries.Add(new SafeEntryRow
            {
                Date = e.Date,
                RefType = e.RefType,
                RefId = e.RefId,
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

        // تطبيق أي بحث موجود على البيانات الجديدة
        ApplySearch();
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
    public string RefType { get; set; } = string.Empty;
    public int RefId { get; set; }
    public string RefTypeDisplay { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Debit { get; set; }
    public decimal Credit { get; set; }
    public decimal Balance { get; set; }
}