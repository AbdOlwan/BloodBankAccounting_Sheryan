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

public partial class PaymentVoucherViewModel : ViewModelBase
{
    [ObservableProperty] private string _voucherNumber = string.Empty;
    [ObservableProperty] private DateTime _date = DateTime.Today;

    /// <summary>اسم المستلم (موظف / مورّد / جهة)</summary>
    [ObservableProperty] private string _payeeName = string.Empty;

    /// <summary>البيان — وصف سبب الصرف</summary>
    [ObservableProperty] private string? _notes;

    [ObservableProperty] private decimal _amount;
    [ObservableProperty] private Safe? _selectedSafe;

    public ObservableCollection<Safe> Safes { get; } = [];

    /// <summary>رقم السند المحفوظ — يُستخدم لطباعة السند بعد الحفظ</summary>
    private int? _savedVoucherId;

    public bool CanPrint => _savedVoucherId.HasValue;

    public PaymentVoucherViewModel() { _ = LoadAsync(); }

    private async Task LoadAsync()
    {
        using var db = new AppDbContext();
        var safes = await db.Safes.OrderBy(s => s.Name).ToListAsync();
        Safes.Clear(); safes.ForEach(Safes.Add);
        VoucherNumber = await NumberGenerator.NextPaymentVoucherNumberAsync(db);
        SelectedSafe = safes.FirstOrDefault(s => s.IsMain);
        _savedVoucherId = null;
        OnPropertyChanged(nameof(CanPrint));
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
                Type = VoucherType.Payment,
                Date = Date,
                SafeId = SelectedSafe!.Id,
                PayeeName = PayeeName.Trim(),
                Amount = Amount,
                Notes = Notes
            };

            db.ReceiptVouchers.Add(voucher);
            await db.SaveChangesAsync();

            // خصم المبلغ من الخزينة (المال يخرج)
            var safe = await db.Safes.FindAsync(SelectedSafe!.Id);
            safe!.Balance -= Amount;

            await db.SaveChangesAsync();

            // قيد المحاسبة: مصاريف مدين — خزينة دائن
            JournalService.PostPayment(db, voucher, SelectedSafe!);
            await db.SaveChangesAsync();

            await tx.CommitAsync();

            _savedVoucherId = voucher.Id;
            OnPropertyChanged(nameof(CanPrint));
            ShowSuccess($"تم حفظ سند الدفع {VoucherNumber} ✓");
        }
        catch (Exception ex) { ShowError(ex.Message); }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private async Task PrintAsync()
    {
        if (!_savedVoucherId.HasValue) return;
        IsBusy = true;
        try { await PaymentPrinter.PrintAsync(_savedVoucherId.Value); }
        catch (Exception ex) { ShowError(ex.Message); }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private async Task NewVoucherAsync()
    {
        PayeeName = string.Empty;
        Notes = null;
        Amount = 0;
        await LoadAsync();
    }

    private bool Validate()
    {
        if (string.IsNullOrWhiteSpace(PayeeName)) { ShowError("أدخل اسم المستلم"); return false; }
        if (SelectedSafe is null) { ShowError("اختر الخزينة"); return false; }
        if (Amount <= 0) { ShowError("المبلغ يجب أن يكون أكبر من صفر"); return false; }

        // تحقق من الرصيد الكافي
        if (SelectedSafe.Balance < Amount)
        {
            ShowError($"رصيد الخزينة غير كافٍ\nالرصيد الحالي: {SelectedSafe.Balance:N2} ج.م\nالمطلوب صرفه: {Amount:N2} ج.م");
            return false;
        }

        return true;
    }
}