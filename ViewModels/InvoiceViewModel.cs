using BloodBankAccounting.Data;
using BloodBankAccounting.Helpers;
using BloodBankAccounting.Models;
using BloodBankAccounting.ViewModels.Base;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using System.Collections.ObjectModel;

namespace BloodBankAccounting.ViewModels;

public partial class InvoiceViewModel : ViewModelBase
{
    // ─── Header ──────────────────────────────────────────────
    [ObservableProperty] private string _invoiceNumber = string.Empty;
    [ObservableProperty] private DateTime _date = DateTime.Today;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsSafeVisible))]
    [NotifyPropertyChangedFor(nameof(IsPartialVisible))]
    [NotifyPropertyChangedFor(nameof(RemainingAfterInitial))]
    private PaymentType _paymentType = PaymentType.Cash;

    [ObservableProperty] private Customer? _selectedCustomer;
    [ObservableProperty] private Safe? _selectedSafe;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(RemainingAfterInitial))]
    private decimal _initialPayment;

    // ─── Quick Add Customer ───────────────────────────────────
    [ObservableProperty] private bool _isQuickAddVisible;
    [ObservableProperty] private string _newCustomerName = string.Empty;
    [ObservableProperty] private CustomerType _newCustomerType = CustomerType.Hospital;
    [ObservableProperty] private string _newCustomerPhone = string.Empty;

    public IEnumerable<CustomerType> CustomerTypes { get; } = Enum.GetValues<CustomerType>();

    // ─── Manual Line Entry ────────────────────────────────────
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(NewLineTotal))]
    private string _newLineName = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(NewLineTotal))]
    private int _newLineQty = 1;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(NewLineTotal))]
    private decimal _newLineUnitPrice;

    public decimal NewLineTotal => NewLineQty * NewLineUnitPrice;

    // ─── Barcode Scan (secondary) ─────────────────────────────
    [ObservableProperty] private string _scanBagNumber = string.Empty;

    // ─── Lines ────────────────────────────────────────────────
    public ObservableCollection<InvoiceLineItem> Lines { get; } = [];

    // ─── Totals ───────────────────────────────────────────────
    [ObservableProperty] private decimal _discountAmount;
    [ObservableProperty] private decimal _subTotal;
    [ObservableProperty] private decimal _totalAmount;

    public bool IsSafeVisible => PaymentType == PaymentType.Cash || PaymentType == PaymentType.Partial;
    public bool IsPartialVisible => PaymentType == PaymentType.Partial;
    public decimal RemainingAfterInitial => Math.Max(0, TotalAmount - InitialPayment);

    // ─── Lookup lists ─────────────────────────────────────────
    public ObservableCollection<Customer> Customers { get; } = [];
    public ObservableCollection<Safe> Safes { get; } = [];
    public ObservableCollection<BloodBag> AvailableBags { get; } = [];

    public IEnumerable<PaymentType> PaymentTypes { get; } = Enum.GetValues<PaymentType>();

    /// <summary>أنواع منتجات الدم الشائعة — قابلة للتحرير الحر في ComboBox</summary>
    public IReadOnlyList<string> BloodProductTypes { get; } =
    [
        "دم كامل",
        "كرات دم مكدسه",
        "بلازما طازجه مجمده",
        "كرايو - راسب اببض",
        "٦ وحدات صفائح دموية",
        " وحدة صفائح",
        "فلترة كيس دم",
        "غسيل كيس دم",
        "كيس دم اطفال",
    ];

    public InvoiceViewModel() { _ = LoadAsync(); }

    private async Task LoadAsync()
    {
        IsBusy = true;
        try
        {
            using var db = new AppDbContext();

            var customers = await db.Customers.Where(c => c.IsActive).OrderBy(c => c.Name).ToListAsync();
            var safes = await db.Safes.OrderBy(s => s.Name).ToListAsync();
            var bags = await db.BloodBags
                .Where(b => b.Status == BloodBagStatus.Available && b.ExpiryDate > DateTime.Today)
                .OrderBy(b => b.ExpiryDate)
                .ToListAsync();

            Customers.Clear(); customers.ForEach(Customers.Add);
            Safes.Clear(); safes.ForEach(Safes.Add);
            AvailableBags.Clear(); bags.ForEach(AvailableBags.Add);

            InvoiceNumber = await NumberGenerator.NextInvoiceNumberAsync(db);
            SelectedSafe = safes.FirstOrDefault(s => s.IsMain);
        }
        finally { IsBusy = false; }
    }

    // ─── Quick Add Customer ───────────────────────────────────
    [RelayCommand]
    private void ToggleQuickAdd()
    {
        IsQuickAddVisible = !IsQuickAddVisible;
        NewCustomerName = string.Empty;
        NewCustomerPhone = string.Empty;
        NewCustomerType = CustomerType.Hospital;
    }

    [RelayCommand]
    private async Task QuickSaveCustomerAsync()
    {
        if (string.IsNullOrWhiteSpace(NewCustomerName))
        {
            ShowError("أدخل اسم العميل");
            return;
        }

        IsBusy = true;
        try
        {
            using var db = new AppDbContext();

            var customer = new Customer
            {
                Name = NewCustomerName.Trim(),
                Type = NewCustomerType,
                Phone = NewCustomerPhone.Trim(),
                IsActive = true
            };

            db.Customers.Add(customer);
            await db.SaveChangesAsync();

            Customers.Add(customer);
            SelectedCustomer = customer;
            IsQuickAddVisible = false;
            NewCustomerName = string.Empty;
            NewCustomerPhone = string.Empty;
            ShowSuccess($"تمت إضافة العميل «{customer.Name}» ✓");
        }
        catch (Exception ex) { ShowError(ex.Message); }
        finally { IsBusy = false; }
    }

    // ─── إضافة صنف يدوي ──────────────────────────────────────
    [RelayCommand]
    private void AddManualLine()
    {
        if (string.IsNullOrWhiteSpace(NewLineName))
        {
            ShowError("أدخل اسم الصنف"); return;
        }
        if (NewLineQty <= 0)
        {
            ShowError("الكمية يجب أن تكون أكبر من صفر"); return;
        }
        if (NewLineUnitPrice <= 0)
        {
            ShowError("أدخل سعر الوحدة"); return;
        }

        var line = new InvoiceLineItem
        {
            BloodBagId = null,
            IsManual = true,
            ProductName = NewLineName.Trim(),
            UnitPrice = NewLineUnitPrice,
            Quantity = NewLineQty
        };
        line.PropertyChanged += (_, _) => RecalculateTotals();
        Lines.Add(line);
        RecalculateTotals();

        // إعادة تعيين حقول الإدخال
        NewLineName = string.Empty;
        NewLineQty = 1;
        NewLineUnitPrice = 0;
    }

    // ─── إضافة كيس بالنقر ────────────────────────────────────
    [RelayCommand]
    private void AddLine(BloodBag bag)
    {
        if (Lines.Any(l => l.BloodBagId == bag.Id))
        {
            ShowError("هذا الكيس مضاف بالفعل"); return;
        }

        var line = new InvoiceLineItem
        {
            BloodBagId = bag.Id,
            IsManual = false,
            ProductName = $"{bag.BagNumber} — {bag.BloodGroup.ToArabic()}",
            UnitPrice = bag.SalePrice,
            Quantity = 1
        };
        line.PropertyChanged += (_, _) => RecalculateTotals();
        Lines.Add(line);
        RecalculateTotals();
    }

    // ─── إضافة كيس بالباركود ─────────────────────────────────
    [RelayCommand]
    private void AddLineByBarcode()
    {
        var barcode = ScanBagNumber.Trim();
        if (string.IsNullOrWhiteSpace(barcode)) return;

        var bag = AvailableBags.FirstOrDefault(b =>
            string.Equals(b.BagNumber.Trim(), barcode, StringComparison.OrdinalIgnoreCase));

        if (bag is null) ShowError($"لا يوجد كيس متاح بهذا الباركود:\n{barcode}");
        else AddLine(bag);

        ScanBagNumber = string.Empty;
    }

    [RelayCommand]
    private void RemoveLine(InvoiceLineItem line)
    {
        Lines.Remove(line);
        RecalculateTotals();
    }

    partial void OnDiscountAmountChanged(decimal value) => RecalculateTotals();

    private void RecalculateTotals()
    {
        SubTotal = Lines.Sum(l => l.Total);
        TotalAmount = Math.Max(0, SubTotal - DiscountAmount);
        OnPropertyChanged(nameof(RemainingAfterInitial));
    }

    // ─── Save ─────────────────────────────────────────────────
    [RelayCommand]
    private async Task SaveAsync()
    {
        if (!Validate()) return;

        IsBusy = true;
        try
        {
            using var db = new AppDbContext();
            await using var tx = await db.Database.BeginTransactionAsync();

            var invoice = new Invoice
            {
                InvoiceNumber = InvoiceNumber,
                Date = Date,
                PaymentType = PaymentType,
                CustomerId = SelectedCustomer!.Id,
                SafeId = IsSafeVisible ? SelectedSafe!.Id : null,
                SubTotal = SubTotal,
                DiscountAmount = DiscountAmount,
                TotalAmount = TotalAmount,
                PaidAmount = PaymentType switch
                {
                    PaymentType.Cash => TotalAmount,
                    PaymentType.Partial => InitialPayment,
                    _ => 0
                }
            };

            db.Invoices.Add(invoice);
            await db.SaveChangesAsync();

            foreach (var line in Lines)
            {
                db.InvoiceLines.Add(new InvoiceLine
                {
                    InvoiceId = invoice.Id,
                    BloodBagId = line.BloodBagId,            // null للأصناف اليدوية
                    ProductName = line.ProductName,
                    UnitPrice = line.UnitPrice,
                    Quantity = line.Quantity,
                    Total = line.Total
                });

                // تحديث حالة الكيس فقط إذا كان مرتبطاً
                if (line.BloodBagId.HasValue)
                {
                    var bag = await db.BloodBags.FindAsync(line.BloodBagId.Value);
                    if (bag is not null) bag.Status = BloodBagStatus.Sold;
                }
            }

            var customer = await db.Customers.FindAsync(SelectedCustomer!.Id);

            switch (PaymentType)
            {
                case PaymentType.Cash:
                    var safeCash = await db.Safes.FindAsync(SelectedSafe!.Id);
                    safeCash!.Balance += TotalAmount;
                    break;

                case PaymentType.Credit:
                    customer!.Balance += TotalAmount;
                    break;

                case PaymentType.Partial:
                    customer!.Balance += (TotalAmount - InitialPayment);
                    var safePartial = await db.Safes.FindAsync(SelectedSafe!.Id);
                    safePartial!.Balance += InitialPayment;
                    break;
            }

            await db.SaveChangesAsync();

            invoice.Customer = customer!;

            if (PaymentType == PaymentType.Partial)
                JournalService.PostPartialInvoice(db, invoice, InitialPayment, AccountCodes.Cash);
            else
                JournalService.PostInvoice(db, invoice,
                    PaymentType == PaymentType.Cash ? AccountCodes.Cash : AccountCodes.AccountsRec);

            await db.SaveChangesAsync();
            await tx.CommitAsync();

            ShowSuccess($"تم حفظ الفاتورة {InvoiceNumber} بنجاح ✓");
            await ResetFormAsync();
        }
        catch (Exception ex)
        {
            // ابحث عن السبب العميق للخطأ
            var message = ex.InnerException?.InnerException?.Message
                          ?? ex.InnerException?.Message
                          ?? ex.Message;

            ShowError($"حدث خطأ أثناء الحفظ:\n{message}");
        }
        finally { IsBusy = false; }
    }

    private bool Validate()
    {
        if (SelectedCustomer is null) { ShowError("اختر العميل"); return false; }
        if (!Lines.Any()) { ShowError("أضف صنفاً واحداً على الأقل"); return false; }

        // ─── التحقق من الخصم ──────────────────────────────────
        if (DiscountAmount < 0)
        {
            ShowError("الخصم لا يمكن أن يكون قيمة سالبة"); return false;
        }
        if (DiscountAmount > SubTotal)
        {
            ShowError("الخصم لا يمكن أن يتجاوز إجمالي الفاتورة"); return false;
        }

        if (PaymentType == PaymentType.Cash && SelectedSafe is null)
        { ShowError("اختر الخزينة"); return false; }
        if (PaymentType == PaymentType.Partial)
        {
            if (SelectedSafe is null) { ShowError("اختر الخزينة"); return false; }
            if (InitialPayment <= 0) { ShowError("أدخل المبلغ المدفوع الآن"); return false; }
            if (InitialPayment >= TotalAmount)
            { ShowError("المبلغ المدفوع يجب أن يكون أقل من الإجمالي\nللدفع الكامل اختر «نقدي»"); return false; }
        }
        return true;
    }

    private async Task ResetFormAsync()
    {
        Lines.Clear();
        SelectedCustomer = null;
        DiscountAmount = 0;
        InitialPayment = 0;
        ScanBagNumber = string.Empty;
        IsQuickAddVisible = false;
        NewLineName = string.Empty;
        NewLineQty = 1;
        NewLineUnitPrice = 0;
        await LoadAsync();
    }
}

// ─── Line item DTO ────────────────────────────────────────────
public partial class InvoiceLineItem : ObservableObject
{
    /// <summary>null → صنف يدوي</summary>
    public int? BloodBagId { get; set; }
    public bool IsManual { get; set; }
    public string ProductName { get; set; } = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Total))]
    private decimal _unitPrice;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Total))]
    private int _quantity = 1;

    public decimal Total => UnitPrice * Quantity;
}

// ─── Extension ────────────────────────────────────────────────
public static class BloodGroupExtensions
{
    public static string ToArabic(this BloodGroup g) => g switch
    {
        BloodGroup.APositive => "A+",
        BloodGroup.ANegative => "A-",
        BloodGroup.BPositive => "B+",
        BloodGroup.BNegative => "B-",
        BloodGroup.ABPositive => "AB+",
        BloodGroup.ABNegative => "AB-",
        BloodGroup.OPositive => "O+",
        BloodGroup.ONegative => "O-",
        _ => g.ToString()
    };
}