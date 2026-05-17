using BloodBankAccounting.Data;
using BloodBankAccounting.Helpers;
using BloodBankAccounting.Models;
using BloodBankAccounting.Printing;
using BloodBankAccounting.ViewModels.Base;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using System.Collections.ObjectModel;
using System.Windows;

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
        try
        {
            if (IsFinancialUnlocked) { IsFinancialUnlocked = false; return; }

            if (string.IsNullOrEmpty(_ownerPinHash)) { IsFinancialUnlocked = true; return; }

            var enteredPin = Views.PinDialog.Show(Application.Current.MainWindow);
            if (enteredPin is null) return;

            if (PinHelper.Verify(enteredPin, _ownerPinHash))
                IsFinancialUnlocked = true;
            else
                ShowError("الرمز السري غير صحيح");
        }
        catch (Exception ex)
        {
            DiagnosticLogger.Log(nameof(ToggleFinancialLock), ex);
            ShowError($"خطأ في عملية القفل:\n{ex.Message}");
        }
    }

    // ════════════════════════════════════════════════════════════
    //  بيانات الخزينة
    // ════════════════════════════════════════════════════════════
    public ObservableCollection<Safe> Safes { get; } = [];

    // _allEntries قائمة داخلية فقط — لا تُربط بـ UI مباشرة
    private readonly List<SafeEntryRow> _allEntries = [];
    public ObservableCollection<SafeEntryRow> Entries { get; } = [];

    [ObservableProperty] private Safe? _selectedSafe;

    [ObservableProperty] private DateTime _from = DateTime.Today;
    [ObservableProperty] private DateTime _to = DateTime.Today;

    [ObservableProperty] private decimal _openingBalance;
    [ObservableProperty] private decimal _closingBalance;
    [ObservableProperty] private decimal _totalDebit;
    [ObservableProperty] private decimal _totalCredit;

    // ════════════════════════════════════════════════════════════
    //  فلتر المجمع/المنفصل
    // ════════════════════════════════════════════════════════════
    [ObservableProperty] private bool _showCombined = false;

    partial void OnShowCombinedChanged(bool value) => RunLoadStatement();

    // ════════════════════════════════════════════════════════════
    //  البحث النصي
    // ════════════════════════════════════════════════════════════
    [ObservableProperty] private string _searchText = string.Empty;
    public int FilteredCount => Entries.Count;

    partial void OnSearchTextChanged(string value) => ApplySearch();

    // ApplySearch تعمل دائماً على UI Thread
    private void ApplySearch()
    {
        try
        {
            var term = SearchText?.Trim() ?? string.Empty;

            var filtered = string.IsNullOrEmpty(term)
                ? _allEntries.ToList()
                : _allEntries.Where(e =>
                    e.Date.ToString("d/M/yyyy").Contains(term) ||
                    e.RefTypeDisplay.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                    e.Description.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                    e.Debit.ToString("N2").Contains(term) ||
                    e.Credit.ToString("N2").Contains(term) ||
                    e.Balance.ToString("N2").Contains(term)).ToList();

            Application.Current.Dispatcher.Invoke(() =>
            {
                Entries.Clear();
                foreach (var row in filtered) Entries.Add(row);
                OnPropertyChanged(nameof(FilteredCount));
            });
        }
        catch (Exception ex)
        {
            DiagnosticLogger.Log(nameof(ApplySearch), ex);
        }
    }

    [RelayCommand]
    private void ClearSearch() => SearchText = string.Empty;

    // ════════════════════════════════════════════════════════════
    //  تحميل
    // ════════════════════════════════════════════════════════════
    public SafeReportViewModel() { RunLoadAsync(); }

    // async void بدلاً من _ = Task() — هي النمط الصحيح لـ fire-and-forget في WPF
    private async void RunLoadAsync()
    {
        try { await LoadAsync(); }
        catch (Exception ex)
        {
            DiagnosticLogger.Log(nameof(LoadAsync), ex);
            ShowError(
                $"خطأ في تحميل بيانات الخزائن:\n\n" +
                $"النوع: {ex.GetType().Name}\n" +
                $"الرسالة: {ex.Message}\n\n" +
                $"الخطأ الداخلي: {ex.InnerException?.Message}");
        }
    }

    private async void RunLoadStatement()
    {
        try { await LoadStatementAsync(); }
        catch (Exception ex)
        {
            DiagnosticLogger.Log(nameof(LoadStatementAsync) + "_auto", ex);
            ShowError(
                $"خطأ في تحميل كشف الخزينة:\n\n" +
                $"النوع: {ex.GetType().Name}\n" +
                $"الرسالة: {ex.Message}\n\n" +
                $"الخطأ الداخلي: {ex.InnerException?.Message}");
        }
    }

    private async Task LoadAsync()
    {
        using var db = new AppDbContext();
        var safes = await db.Safes.OrderBy(s => s.Name).ToListAsync();

        Application.Current.Dispatcher.Invoke(() =>
        {
            Safes.Clear();
            safes.ForEach(Safes.Add);
        });

        SelectedSafe = safes.FirstOrDefault(s => s.IsMain) ?? safes.FirstOrDefault();

        var org = await db.OrgSettings.FirstOrDefaultAsync();
        _ownerPinHash = org?.OwnerPinHash ?? string.Empty;
        IsFinancialUnlocked = string.IsNullOrEmpty(_ownerPinHash);
    }

    partial void OnSelectedSafeChanged(Safe? value)
    {
        if (value is not null) RunLoadStatement();
    }

    [RelayCommand]
    private async Task LoadStatementAsync()
    {
        try
        {
            if (SelectedSafe is null && !ShowCombined) return;

            using var db = new AppDbContext();

            // بناء الفلتر صراحةً بدلاً من Contains على array
            // لأن EF Core 8 يُترجم Contains إلى OPENJSON...WITH غير مدعوم على بعض إصدارات SQL Server
            string safeCode1;
            string? safeCode2 = null;

            if (ShowCombined)
            {
                safeCode1 = AccountCodes.Cash;
                safeCode2 = AccountCodes.Electronic;
            }
            else
            {
                safeCode1 = JournalService.GetSafeAccountCode(SelectedSafe!);
            }

            bool isCombined = safeCode2 is not null;

            var beforeDebit = await db.JournalEntries
                .Where(j => j.Date < From &&
                            (j.AccountCode == safeCode1 || (isCombined && j.AccountCode == safeCode2)))
                .SumAsync(j => (decimal?)j.DebitAmount) ?? 0;

            var beforeCredit = await db.JournalEntries
                .Where(j => j.Date < From &&
                            (j.AccountCode == safeCode1 || (isCombined && j.AccountCode == safeCode2)))
                .SumAsync(j => (decimal?)j.CreditAmount) ?? 0;

            OpeningBalance = beforeDebit - beforeCredit;

            var entries = await db.JournalEntries
                .Where(j => j.Date >= From && j.Date <= To &&
                            (j.AccountCode == safeCode1 || (isCombined && j.AccountCode == safeCode2)))
                .OrderBy(j => j.Date)
                .ToListAsync();

            // بناء الـ rows على background thread — بدون لمس ObservableCollection
            var rows = new List<SafeEntryRow>(entries.Count);
            var runningBalance = OpeningBalance;

            foreach (var e in entries)
            {
                runningBalance += e.DebitAmount - e.CreditAmount;
                rows.Add(new SafeEntryRow
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

            var totalDebit = entries.Sum(e => e.DebitAmount);
            var totalCredit = entries.Sum(e => e.CreditAmount);

            // كل تعديل على UI يتم على UI Thread
            Application.Current.Dispatcher.Invoke(() =>
            {
                _allEntries.Clear();
                _allEntries.AddRange(rows);
                TotalDebit = totalDebit;
                TotalCredit = totalCredit;
                ClosingBalance = runningBalance;
            });

            ApplySearch();
        }
        catch (Exception ex)
        {
            DiagnosticLogger.Log(nameof(LoadStatementAsync), ex);
            ShowError(
                $"خطأ في تحميل بيانات الخزينة:\n\n" +
                $"النوع: {ex.GetType().Name}\n" +
                $"الرسالة: {ex.Message}\n\n" +
                $"الخطأ الداخلي: {ex.InnerException?.Message}\n\n" +
                $"تم حفظ التفاصيل في:\n{DiagnosticLogger.GetLogPath()}");
        }
    }

    // ════════════════════════════════════════════════════════════
    //  طباعة
    // ════════════════════════════════════════════════════════════
    [RelayCommand]
    private async Task PrintAsync()
    {
        if (!ShowCombined && SelectedSafe is null) { ShowError("اختر الخزينة أولاً"); return; }
        IsBusy = true;
        try
        {
            await SafeStatementPrinter.PrintAsync(
                ShowCombined ? "جميع الخزائن" : SelectedSafe!.Name,
                From, To,
                OpeningBalance, ClosingBalance,
                TotalDebit, TotalCredit,
                Entries.ToList());
        }
        catch (Exception ex)
        {
            DiagnosticLogger.Log(nameof(PrintAsync), ex);
            ShowError($"خطأ في الطباعة:\n{ex.Message}\n\nالخطأ الداخلي: {ex.InnerException?.Message}");
        }
        finally { IsBusy = false; }
    }

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
        catch (Exception ex)
        {
            DiagnosticLogger.Log(nameof(PrintEntryAsync), ex);
            ShowError(
                $"خطأ في طباعة الحركة:\n\n" +
                $"النوع: {ex.GetType().Name}\n" +
                $"الرسالة: {ex.Message}\n\n" +
                $"الخطأ الداخلي: {ex.InnerException?.Message}");
        }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private async Task EditInvoiceEntryAsync(SafeEntryRow entry)
    {
        if (entry is null) return;
        if (entry.RefType != "Invoice")
        {
            ShowError("التعديل متاح للفواتير فقط.\nسندات القبض والصرف لها شاشات تعديل منفصلة.");
            return;
        }

        try
        {
            var saved = Views.EditInvoiceWindow.ShowEdit(entry.RefId, Application.Current.MainWindow);
            if (saved) await LoadStatementAsync();
        }
        catch (Exception ex)
        {
            DiagnosticLogger.Log(nameof(EditInvoiceEntryAsync), ex);
            ShowError($"خطأ في تعديل الفاتورة:\n{ex.Message}\n\nالخطأ الداخلي: {ex.InnerException?.Message}");
        }
    }
}

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