using BloodBankAccounting.Data;
using BloodBankAccounting.Helpers;
using BloodBankAccounting.Models;
using BloodBankAccounting.Printing;
using BloodBankAccounting.ViewModels.Base;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.Net;

namespace BloodBankAccounting.ViewModels;

// ════════════════════════════════════════════════════════════
//  DASHBOARD  (مع قفل البيانات المالية)
// ════════════════════════════════════════════════════════════
//public partial class DashboardViewModel : ViewModelBase
//{
//    [ObservableProperty] private decimal _todaySales;
//    [ObservableProperty] private decimal _totalDebt;
//    [ObservableProperty] private decimal _safeBalance;
//    [ObservableProperty] private int _availableBags;
//    [ObservableProperty] private int _expiringBags;

//    // ─── القفل المالي ─────────────────────────────────────────
//    [ObservableProperty] private bool _isFinancialUnlocked = true;

//    private string _ownerPinHash = string.Empty;

//    /// <summary>أيقونة القفل على الزر</summary>
//    public string LockIcon => IsFinancialUnlocked ? "🔓" : "🔒";

//    /// <summary>نص زر القفل</summary>
//    public string LockButtonText => IsFinancialUnlocked ? "إخفاء البيانات" : "عرض البيانات المالية";

//    partial void OnIsFinancialUnlockedChanged(bool value)
//    {
//        OnPropertyChanged(nameof(LockIcon));
//        OnPropertyChanged(nameof(LockButtonText));
//    }

//    public DashboardViewModel() { _ = LoadAsync(); }

//    private async Task LoadAsync()
//    {
//        using var db = new AppDbContext();
//        var today = DateTime.Today;
//        var nextWeek = today.AddDays(7);

//        TodaySales = await db.Invoices.Where(i => i.Date.Date == today && !i.IsCancelled)
//                                         .SumAsync(i => (decimal?)i.TotalAmount) ?? 0;
//        TotalDebt = await db.Customers.SumAsync(c => (decimal?)c.Balance) ?? 0;
//        SafeBalance = await db.Safes.SumAsync(s => (decimal?)s.Balance) ?? 0;
//        AvailableBags = await db.BloodBags.CountAsync(b => b.Status == BloodBagStatus.Available);
//        ExpiringBags = await db.BloodBags.CountAsync(b =>
//                            b.Status == BloodBagStatus.Available && b.ExpiryDate <= nextWeek);

//        // تحميل هاش الرمز السري للمالك
//        var org = await db.OrgSettings.FirstOrDefaultAsync();
//        _ownerPinHash = org?.OwnerPinHash ?? string.Empty;

//        // إذا لا يوجد رمز سري → البيانات مرئية دائماً
//        if (string.IsNullOrEmpty(_ownerPinHash))
//            IsFinancialUnlocked = true;
//        else
//            IsFinancialUnlocked = false;   // ابدأ مقفولاً
//    }

//    [RelayCommand]
//    private void ToggleFinancialLock()
//    {
//        if (IsFinancialUnlocked)
//        {
//            // قفل مباشر بدون رمز
//            IsFinancialUnlocked = false;
//            return;
//        }

//        // إذا لا يوجد رمز مُفعَّل → فتح مباشر
//        if (string.IsNullOrEmpty(_ownerPinHash))
//        {
//            IsFinancialUnlocked = true;
//            return;
//        }

//        // عرض نافذة الرمز السري
//        var enteredPin = Views.PinDialog.Show(
//            System.Windows.Application.Current.MainWindow);

//        if (enteredPin is null) return;   // ألغى المستخدم

//        if (PinHelper.Verify(enteredPin, _ownerPinHash))
//        {
//            IsFinancialUnlocked = true;
//        }
//        else
//        {
//            ShowError("الرمز السري غير صحيح");
//        }
//    }
//}

// ════════════════════════════════════════════════════════════
//  CUSTOMERS
// ════════════════════════════════════════════════════════════
public partial class CustomersViewModel : ViewModelBase
{
    public ObservableCollection<Customer> Customers { get; } = [];

    [ObservableProperty] private Customer? _selected;
    [ObservableProperty] private string _filterText = string.Empty;

    // Form fields
    [ObservableProperty] private string _name = string.Empty;
    [ObservableProperty] private CustomerType _type = CustomerType.Hospital;
    [ObservableProperty] private string _phone = string.Empty;
    [ObservableProperty] private decimal _creditLimit;
    [ObservableProperty] private bool _isEditing;

    public IEnumerable<CustomerType> CustomerTypes { get; } = Enum.GetValues<CustomerType>();

    public CustomersViewModel() { _ = LoadAsync(); }

    private async Task LoadAsync()
    {
        using var db = new AppDbContext();
        var list = await db.Customers.OrderBy(c => c.Name).ToListAsync();
        Customers.Clear();
        list.ForEach(Customers.Add);
    }

    partial void OnSelectedChanged(Customer? value)
    {
        if (value is null) return;
        Name = value.Name;
        Type = value.Type;
        Phone = value.Phone;
        CreditLimit = value.CreditLimit;
        IsEditing = true;
    }

    [RelayCommand]
    private void NewCustomer()
    {
        Selected = null;
        Name = string.Empty;
        Phone = string.Empty;
        CreditLimit = 0;
        Type = CustomerType.Hospital;
        IsEditing = true;
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (string.IsNullOrWhiteSpace(Name)) { ShowError("اسم العميل مطلوب"); return; }
        using var db = new AppDbContext();

        if (Selected is null)
        {
            db.Customers.Add(new Customer { Name = Name, Type = Type, Phone = Phone, CreditLimit = CreditLimit });
        }
        else
        {
            var c = await db.Customers.FindAsync(Selected.Id);
            c!.Name = Name; c.Type = Type; c.Phone = Phone; c.CreditLimit = CreditLimit;
        }

        await db.SaveChangesAsync();
        IsEditing = false;
        await LoadAsync();
    }

    [RelayCommand]
    private async Task SearchAsync()
    {
        using var db = new AppDbContext();
        var q = FilterText.Trim();
        var list = await db.Customers
            .Where(c => string.IsNullOrEmpty(q) || c.Name.Contains(q) || c.Phone.Contains(q))
            .OrderBy(c => c.Name).ToListAsync();
        Customers.Clear();
        list.ForEach(Customers.Add);
    }
}

// ════════════════════════════════════════════════════════════
//  BLOOD INVENTORY  (نظام المخزون المجمع)
// ════════════════════════════════════════════════════════════
public partial class BloodInventoryViewModel : ViewModelBase
{
    // ── جدول المخزون الحالي ───────────────────────────────────────────
    public ObservableCollection<BloodStock> StockRows { get; } = [];
    private List<BloodStock> _allStockRows = [];

    // ── قائمة الإدخال المؤقتة (staging) قبل الحفظ الجماعي ────────────
    public ObservableCollection<StockEntryDraft> PendingEntries { get; } = [];

    // ── مصادر القوائم المنسدلة ─────────────────────────────────────────
    public ObservableCollection<BloodProduct> Products { get; } = [];

    /// <summary>null = بدون فصيلة (أول عنصر في القائمة)</summary>
    public IEnumerable<BloodGroup?> BloodGroups { get; } =
        new BloodGroup?[] { null }
        .Concat(Enum.GetValues<BloodGroup>().Cast<BloodGroup?>());

    // ── حقول نموذج الإدخال ────────────────────────────────────────────
    [ObservableProperty] private BloodProduct? _selectedProduct;
    [ObservableProperty] private BloodGroup? _selectedBloodGroup;
    [ObservableProperty] private int _quantity = 1;

    // ── بحث في جدول المخزون ───────────────────────────────────────────
    [ObservableProperty] private string _filterText = string.Empty;
    partial void OnFilterTextChanged(string value) => ApplyFilter();

    public BloodInventoryViewModel() { _ = LoadAsync(); }

    // ────────────────────────────────────────────────────────────────────
    //  تحميل البيانات
    // ────────────────────────────────────────────────────────────────────
    private async Task LoadAsync()
    {
        IsBusy = true;
        try
        {
            using var db = new AppDbContext();

            // أصناف المنتجات (للقائمة المنسدلة)
            var products = await db.BloodProducts
                .Where(p => p.IsActive)
                .OrderBy(p => p.SortOrder)
                .ToListAsync();
            Products.Clear();
            products.ForEach(Products.Add);

            // المخزون الحالي كاملًا
            _allStockRows = await db.BloodStocks
                .Include(s => s.Product)
                .OrderBy(s => s.Product.SortOrder)
                .ThenBy(s => s.BloodGroup)
                .ToListAsync();

            ApplyFilter();
        }
        finally { IsBusy = false; }
    }

    private void ApplyFilter()
    {
        var q = FilterText.Trim();
        var filtered = string.IsNullOrEmpty(q)
            ? _allStockRows
            : _allStockRows.Where(s =>
                s.Product.Name.Contains(q, StringComparison.OrdinalIgnoreCase)).ToList();

        StockRows.Clear();
        filtered.ForEach(StockRows.Add);
    }

    // ────────────────────────────────────────────────────────────────────
    //  إضافة سطر إلى قائمة الانتظار
    // ────────────────────────────────────────────────────────────────────
    [RelayCommand]
    private void AddToPending()
    {
        if (SelectedProduct is null) { ShowError("اختر صنف الكيس أولًا"); return; }
        if (Quantity <= 0) { ShowError("الكمية يجب أن تكون أكبر من صفر"); return; }

        // دمج مع إدخال موجود بنفس الصنف والفصيلة
        var existing = PendingEntries.FirstOrDefault(
            e => e.ProductId == SelectedProduct.Id && e.BloodGroup == SelectedBloodGroup);

        if (existing is not null)
        {
            // استبدال السجل لتحديث الـ UI (ObservableCollection لا تلاحظ تغيير الخصائص الداخلية)
            var idx = PendingEntries.IndexOf(existing);
            PendingEntries[idx] = new StockEntryDraft
            {
                ProductId = existing.ProductId,
                ProductName = existing.ProductName,
                BloodGroup = existing.BloodGroup,
                Quantity = existing.Quantity + Quantity
            };
        }
        else
        {
            PendingEntries.Add(new StockEntryDraft
            {
                ProductId = SelectedProduct.Id,
                ProductName = SelectedProduct.Name,
                BloodGroup = SelectedBloodGroup,
                Quantity = Quantity
            });
        }

        // إعادة تهيئة الكمية فقط — الصنف والفصيلة تبقيان للتسهيل
        Quantity = 1;
    }

    // ────────────────────────────────────────────────────────────────────
    //  حذف سطر من قائمة الانتظار
    // ────────────────────────────────────────────────────────────────────
    [RelayCommand]
    private void RemoveFromPending(StockEntryDraft entry)
        => PendingEntries.Remove(entry);

    // ────────────────────────────────────────────────────────────────────
    //  حفظ جميع الإدخالات دفعة واحدة
    // ────────────────────────────────────────────────────────────────────
    [RelayCommand]
    private async Task SaveAllAsync()
    {
        if (PendingEntries.Count == 0) { ShowError("لا توجد إدخالات للحفظ"); return; }

        int savedCount = PendingEntries.Count;
        IsBusy = true;
        try
        {
            using var db = new AppDbContext();

            foreach (var draft in PendingEntries)
            {
                // البحث عن سجل مخزون موجود بنفس الصنف والفصيلة
                var stock = await db.BloodStocks.FirstOrDefaultAsync(
                    s => s.ProductId == draft.ProductId && s.BloodGroup == draft.BloodGroup);

                if (stock is not null)
                {
                    // سجل موجود → زيادة الكمية (يعمل سواء كان الإدخال يدويًا أو بالباركود)
                    stock.Quantity += draft.Quantity;
                    stock.LastUpdated = DateTime.Now;
                }
                else
                {
                    // سجل جديد
                    db.BloodStocks.Add(new BloodStock
                    {
                        ProductId = draft.ProductId,
                        BloodGroup = draft.BloodGroup,
                        Quantity = draft.Quantity
                    });
                }
            }

            await db.SaveChangesAsync();
            PendingEntries.Clear();
            ShowSuccess($"تم حفظ {savedCount} {(savedCount == 1 ? "إدخال" : "إدخالات")} بنجاح ✓");
            await LoadAsync();
        }
        catch (Exception ex)
        {
            ShowError($"خطأ أثناء الحفظ: {ex.Message}");
        }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private async Task RefreshAsync() => await LoadAsync();
}

// ════════════════════════════════════════════════════════════
//  INVOICE LIST
// ════════════════════════════════════════════════════════════
public partial class InvoiceListViewModel : ViewModelBase
{
    public ObservableCollection<Invoice> Invoices { get; } = [];

    // ─── كل الفواتير في الذاكرة للفلترة الفورية ──────────────────────
    private List<Invoice> _allInvoices = [];

    [ObservableProperty] private DateTime _from = DateTime.Today.AddDays(-2);
    [ObservableProperty] private DateTime _to = DateTime.Today;
    [ObservableProperty] private string _searchText = string.Empty;
    [ObservableProperty] private int _resultCount;

    partial void OnSearchTextChanged(string value) => FilterInvoices();

    private void FilterInvoices()
    {
        var q = SearchText.Trim();
        var filtered = string.IsNullOrEmpty(q)
            ? _allInvoices
            : _allInvoices.Where(i =>
                i.InvoiceNumber.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                (i.Customer?.Name?.Contains(q, StringComparison.OrdinalIgnoreCase) == true) ||
                i.TotalAmount.ToString("N2").Contains(q) ||
                i.PaidAmount.ToString("N2").Contains(q) ||
                i.Date.ToString("d/M/yyyy").Contains(q) ||
                (i.PaymentType == PaymentType.Cash && "نقدي".Contains(q)) ||
                (i.PaymentType == PaymentType.Credit && "آجل".Contains(q)) ||
                (i.PaymentType == PaymentType.Partial && "جزئي".Contains(q)) ||
                (i.IsCancelled && "ملغي".Contains(q)) ||
                (!i.IsCancelled && "مكتمل".Contains(q))
            ).ToList();

        Invoices.Clear();
        filtered.ForEach(Invoices.Add);
        ResultCount = filtered.Count;
    }

    [RelayCommand]
    private void ClearSearch() => SearchText = string.Empty;

    public InvoiceListViewModel() { _ = LoadAsync(); }

    [RelayCommand]
    private async Task LoadAsync()
    {
        using var db = new AppDbContext();
        _allInvoices = await db.Invoices
                               .Include(i => i.Customer)
                               .Where(i => i.Date.Date >= From && i.Date.Date <= To)
                               .OrderByDescending(i => i.Date)
                               .ToListAsync();
        // نستدعي FilterInvoices مباشرة بدل الاعتماد على SearchText change
        // لأن لو SearchText فاضية أصلاً مش هيطلق الـ event
        FilterInvoices();
    }

    [RelayCommand]
    private async Task PrintInvoiceAsync(Invoice invoice)
    {
        await InvoicePrinter.PrintAsync(invoice.Id);
    }

    // ════════════════════════════════════════════════════════════
    //  استبدل الـ method الموجودة في InvoiceListViewModel
    //  السطور 277–312 في AllViewModels.cs
    // ════════════════════════════════════════════════════════════

    [RelayCommand]
    private async Task CancelInvoiceAsync(Invoice invoice)
    {
        // ── 1. Validation أساسي ──────────────────────────────────
        if (invoice.IsCancelled)
        {
            ShowError($"الفاتورة {invoice.InvoiceNumber} ملغاة بالفعل.");
            return;
        }

        // ── 2. طلب سبب الإلغاء ──────────────────────────────────
        var reason = Views.InputDialog.Show(
            $"سبب إلغاء الفاتورة {invoice.InvoiceNumber}",
            "إلغاء فاتورة",
            System.Windows.Application.Current.MainWindow);

        if (reason is null) return;          // المستخدم ضغط إلغاء
        if (string.IsNullOrWhiteSpace(reason))
        {
            ShowError("يجب إدخال سبب الإلغاء.");
            return;
        }

        // ── 3. تأكيد نهائي ───────────────────────────────────────
        if (!Confirm(
            $"تأكيد إلغاء الفاتورة {invoice.InvoiceNumber}\n" +
            $"السبب: {reason}\n\n" +
            "سيتم عكس جميع الأثار المالية — هل أنت متأكد؟"))
            return;

        IsBusy = true;
        try
        {
            using var db = new AppDbContext();
            await using var tx = await db.Database.BeginTransactionAsync();

            // تحميل الفاتورة مع كل البيانات المرتبطة
            var inv = await db.Invoices
                .Include(i => i.Lines)
                .Include(i => i.JournalEntries)
                .FirstOrDefaultAsync(i => i.Id == invoice.Id);

            if (inv is null)
            {
                ShowError("لم يتم العثور على الفاتورة في قاعدة البيانات.");
                return;
            }

            // تحقق مجدداً (race condition protection)
            if (inv.IsCancelled)
            {
                ShowError($"الفاتورة {inv.InvoiceNumber} ملغاة بالفعل.");
                return;
            }

            // ── 4. عكس حالة أكياس الدم ───────────────────────────
            foreach (var line in inv.Lines)
            {
                if (!line.BloodBagId.HasValue) continue;   // صنف يدوي — لا شيء
                var bag = await db.BloodBags.FindAsync(line.BloodBagId.Value);
                if (bag is not null) bag.Status = BloodBagStatus.Available;
            }

            // ── 5. عكس الأثر المالي حسب نوع الدفع ───────────────
            switch (inv.PaymentType)
            {
                // نقدي: المبلغ كله دخل الخزينة → نخصمه منها
                case PaymentType.Cash:
                    if (inv.SafeId.HasValue)
                    {
                        var safeCash = await db.Safes.FindAsync(inv.SafeId.Value);
                        if (safeCash is not null)
                            safeCash.Balance -= inv.TotalAmount;
                    }
                    break;

                // آجل: المبلغ كله أُضيف لدين العميل → نخصمه منه
                case PaymentType.Credit:
                    var cCredit = await db.Customers.FindAsync(inv.CustomerId);
                    if (cCredit is not null)
                        cCredit.Balance -= inv.TotalAmount;
                    break;

                // جزئي: جزء دخل الخزينة + جزء دين على العميل
                case PaymentType.Partial:
                    var cPartial = await db.Customers.FindAsync(inv.CustomerId);
                    if (cPartial is not null)
                        cPartial.Balance -= inv.RemainingAmount;   // الدين المتبقي

                    if (inv.SafeId.HasValue)
                    {
                        var safePartial = await db.Safes.FindAsync(inv.SafeId.Value);
                        if (safePartial is not null)
                            safePartial.Balance -= inv.PaidAmount;   // المقبوض فعلاً
                    }
                    break;
            }

            // ── 6. حذف القيود المحاسبية المرتبطة ────────────────
            if (inv.JournalEntries.Any())
                db.JournalEntries.RemoveRange(inv.JournalEntries);

            // ── 7. تسجيل الإلغاء ─────────────────────────────────
            inv.IsCancelled = true;
            inv.CancelReason = reason.Trim();

            await db.SaveChangesAsync();
            await tx.CommitAsync();

            ShowSuccess($"تم إلغاء الفاتورة {inv.InvoiceNumber} وعكس جميع آثارها المالية ✓");
            await LoadAsync();
        }
        catch (Exception ex)
        {
            var msg = ex.InnerException?.Message ?? ex.Message;
            ShowError($"حدث خطأ أثناء الإلغاء:\n{msg}");
        }
        finally { IsBusy = false; }
    }

    // ════════════════════════════════════════════════════════════
    // أضِف هذا الـ method داخل InvoiceListViewModel
    // بعد CancelInvoiceAsync مباشرةً
    // ════════════════════════════════════════════════════════════

    [RelayCommand]
    private async Task EditInvoiceAsync(Invoice invoice)
    {
        if (invoice.IsCancelled)
        {
            ShowError($"الفاتورة {invoice.InvoiceNumber} ملغاة ولا يمكن تعديلها.");
            return;
        }

        var saved = Views.EditInvoiceWindow.ShowEdit(
            invoice.Id,
            System.Windows.Application.Current.MainWindow);

        if (saved)
            await LoadAsync();   // تحديث القائمة بعد التعديل
    }
}

// ════════════════════════════════════════════════════════════
//  DEBT REPORT
// ════════════════════════════════════════════════════════════
public partial class DebtReportViewModel : ViewModelBase
{
    public ObservableCollection<Customer> Debtors { get; } = [];
    [ObservableProperty] private decimal _totalDebt;

    public DebtReportViewModel() { _ = LoadAsync(); }

    private async Task LoadAsync()
    {
        using var db = new AppDbContext();
        var list = await db.Customers
                               .Where(c => c.Balance > 0)
                               .OrderByDescending(c => c.Balance)
                               .ToListAsync();
        Debtors.Clear();
        list.ForEach(Debtors.Add);
        TotalDebt = list.Sum(c => c.Balance);
    }
}

// ════════════════════════════════════════════════════════════
//  ORGANIZATION SETTINGS  (الإعدادات — جديد)
// ════════════════════════════════════════════════════════════
public partial class OrgSettingsViewModel : ViewModelBase
{
    // ─── بيانات البنك ─────────────────────────────────────────
    [ObservableProperty] private string _bankName = string.Empty;
    [ObservableProperty] private string _subTitle = string.Empty;
    [ObservableProperty] private string _address = string.Empty;
    [ObservableProperty] private string _phone = string.Empty;
    [ObservableProperty] private string? _logoPath;
    [ObservableProperty] private bool _hasLogo;

    // ─── الرمز السري ──────────────────────────────────────────
    [ObservableProperty] private bool _hasPin;
    [ObservableProperty] private string _pinStatusMessage = string.Empty;
    [ObservableProperty] private bool _hasPinError;

    private string _currentPinHash = string.Empty;

    public OrgSettingsViewModel() { _ = LoadAsync(); }

    private async Task LoadAsync()
    {
        using var db = new AppDbContext();
        var org = await db.OrgSettings.FirstOrDefaultAsync()
                  ?? new OrganizationSettings();

        BankName = org.BankName;
        SubTitle = org.SubTitle;
        Address = org.Address;
        Phone = org.Phone;
        LogoPath = org.LogoPath;
        HasLogo = !string.IsNullOrWhiteSpace(org.LogoPath) && System.IO.File.Exists(org.LogoPath);

        _currentPinHash = org.OwnerPinHash;
        HasPin = !string.IsNullOrEmpty(_currentPinHash);
    }

    // ─── حفظ بيانات البنك ─────────────────────────────────────
    [RelayCommand]
    private async Task SaveOrgInfoAsync()
    {
        if (string.IsNullOrWhiteSpace(BankName))
        {
            ShowError("اسم بنك الدم مطلوب");
            return;
        }

        IsBusy = true;
        try
        {
            using var db = new AppDbContext();
            var org = await db.OrgSettings.FirstOrDefaultAsync();

            if (org is null)
            {
                org = new OrganizationSettings { Id = 1 };
                db.OrgSettings.Add(org);
            }

            org.BankName = BankName.Trim();
            org.SubTitle = SubTitle.Trim();
            org.Address = Address.Trim();
            org.Phone = Phone.Trim();
            org.LogoPath = string.IsNullOrWhiteSpace(LogoPath) ? null : LogoPath.Trim();

            await db.SaveChangesAsync();

            OrgInfoCache.Invalidate();   // إعادة تحميل عند الطباعة التالية
            StatusMessage = "✓  تم حفظ بيانات البنك بنجاح";
        }
        catch (Exception ex) { ShowError(ex.Message); }
        finally { IsBusy = false; }
    }

    // ─── شعار البنك ───────────────────────────────────────────
    [RelayCommand]
    private void BrowseLogo()
    {
        var dlg = new OpenFileDialog
        {
            Title = "اختر صورة شعار البنك",
            Filter = "صور (*.png;*.jpg;*.jpeg;*.bmp)|*.png;*.jpg;*.jpeg;*.bmp"
        };
        if (dlg.ShowDialog() != true) return;

        LogoPath = dlg.FileName;
        HasLogo = true;
        StatusMessage = string.Empty;
    }

    [RelayCommand]
    private void RemoveLogo()
    {
        LogoPath = null;
        HasLogo = false;
    }

    // ─── الرمز السري (يُستدعى من Code-behind لأن PasswordBox لا يدعم Binding) ──
    public void ChangePinFromView(string currentPin, string newPin)
    {
        PinStatusMessage = string.Empty;
        HasPinError = false;

        if (string.IsNullOrWhiteSpace(newPin))
        {
            PinStatusMessage = "أدخل الرمز الجديد أولاً";
            HasPinError = true;
            return;
        }

        // إذا كان هناك رمز سابق — تحقق منه أولاً
        if (HasPin && !PinHelper.Verify(currentPin, _currentPinHash))
        {
            PinStatusMessage = "الرمز الحالي غير صحيح";
            HasPinError = true;
            return;
        }

        _ = SaveNewPinAsync(newPin);
    }

    public void RemovePinFromView(string currentPin)
    {
        PinStatusMessage = string.Empty;
        HasPinError = false;

        if (!PinHelper.Verify(currentPin, _currentPinHash))
        {
            PinStatusMessage = "الرمز الحالي غير صحيح";
            HasPinError = true;
            return;
        }

        _ = SaveNewPinAsync(string.Empty);   // رمز فارغ = بدون حماية
    }

    private async Task SaveNewPinAsync(string newPin)
    {
        IsBusy = true;
        try
        {
            var newHash = PinHelper.Hash(newPin);

            using var db = new AppDbContext();
            var org = await db.OrgSettings.FirstOrDefaultAsync();
            if (org is null) { org = new OrganizationSettings { Id = 1 }; db.OrgSettings.Add(org); }

            org.OwnerPinHash = newHash;
            await db.SaveChangesAsync();

            _currentPinHash = newHash;
            HasPin = !string.IsNullOrEmpty(newHash);
            PinStatusMessage = HasPin ? "✓  تم حفظ الرمز السري بنجاح" : "✓  تم إزالة الرمز السري";
            HasPinError = false;
        }
        catch (Exception ex)
        {
            PinStatusMessage = ex.Message;
            HasPinError = true;
        }
        finally { IsBusy = false; }
    }
}   // ← end of OrgSettingsViewModel




// ════════════════════════════════════════════════════════════
//  CHART DATA CLASSES  (لا تغيير عليهم)
// ════════════════════════════════════════════════════════════

/// <summary>نقطة بيانات يومية لرسم مبيعات وإيرادات آخر 7 أيام</summary>
public class DailySalesPoint
{
    public string Label { get; set; } = string.Empty;
    public string DateLabel { get; set; } = string.Empty;
    public decimal Sales { get; set; }
    public decimal Revenue { get; set; }
    public bool IsToday { get; set; }

    public double SalesBarHeight { get; set; }
    public double SalesSpacerHeight { get; set; }
    public double RevenueBarHeight { get; set; }
    public double RevenueSpacerHeight { get; set; }
}

/// <summary>فصيلة دم واحدة لرسم التوزيع الأفقي</summary>
public class BloodGroupItem
{
    public string GroupName { get; set; } = string.Empty;
    public int Count { get; set; }
    public double BarWidth { get; set; }
    public System.Windows.Media.SolidColorBrush Brush { get; set; }
        = System.Windows.Media.Brushes.DarkRed;
}

// ════════════════════════════════════════════════════════════
//  DASHBOARD VIEW MODEL  — استبدل الكلاس الموجود بهذا
// ════════════════════════════════════════════════════════════
public partial class DashboardViewModel : ViewModelBase
{
    // ─── إحصائيات اليوم ──────────────────────────────────────
    /// <summary>إجمالي قيمة الفواتير المُصدرة اليوم</summary>
    [ObservableProperty] private decimal _todaySales;

    /// <summary>الفلوس الداخلة فعلاً اليوم — نقدي + جزئي + سندات قبض</summary>
    [ObservableProperty] private decimal _todayRevenue;

    [ObservableProperty] private int _todayInvoicesCount;

    /// <summary>فواتير نقدي + جزئي اليوم (تم تحصيلها)</summary>
    [ObservableProperty] private int _todayCashInvoices;

    /// <summary>فواتير آجل اليوم (لم تُحصَّل بعد)</summary>
    [ObservableProperty] private int _todayCreditInvoices;

    /// <summary>نسبة الإيرادات من المبيعات اليوم (0–100+)</summary>
    [ObservableProperty] private decimal _collectionRateToday;

    // ─── إحصائيات الشهر ──────────────────────────────────────
    [ObservableProperty] private decimal _monthSales;
    [ObservableProperty] private decimal _monthRevenue;
    [ObservableProperty] private int _monthInvoicesCount;

    /// <summary>متوسط المبيعات اليومي هذا الشهر</summary>
    [ObservableProperty] private decimal _averageDailySales;

    /// <summary>نسبة التحصيل الشهرية (0–100+)</summary>
    [ObservableProperty] private decimal _collectionRateMonth;

    // ─── مؤشرات مستمرة ───────────────────────────────────────
    [ObservableProperty] private decimal _totalDebt;
    [ObservableProperty] private decimal _safeBalance;
    [ObservableProperty] private int _availableBags;
    [ObservableProperty] private int _expiringBags;
    [ObservableProperty] private string _topDebtorName = "—";
    [ObservableProperty] private decimal _topDebtorBalance;

    // ─── بيانات الرسوم البيانية ───────────────────────────────
    public ObservableCollection<DailySalesPoint> WeeklySales { get; } = [];
    public ObservableCollection<BloodGroupItem> BloodGroupStats { get; } = [];

    // ─── القفل المالي ─────────────────────────────────────────
    [ObservableProperty] private bool _isFinancialUnlocked = true;
    private string _ownerPinHash = string.Empty;

    public string LockIcon => IsFinancialUnlocked ? "🔓" : "🔒";
    public string LockButtonText => IsFinancialUnlocked ? "إخفاء البيانات" : "عرض البيانات المالية";

    // ─── خصائص محسوبة للرسوم ─────────────────────────────────
    /// <summary>عرض شريط التحصيل اليومي بالبكسل — max = 180</summary>
    public double CollectionRateTodayWidth
        => (double)Math.Min(CollectionRateToday, 100) / 100.0 * 180.0;

    /// <summary>عرض شريط التحصيل الشهري بالبكسل — max = 180</summary>
    public double CollectionRateMonthWidth
        => (double)Math.Min(CollectionRateMonth, 100) / 100.0 * 180.0;

    /// <summary>هل توجد أكياس ستنتهي صلاحيتها خلال أسبوع؟</summary>
    public bool HasExpiringBags => ExpiringBags > 0;

    // ─── Partial change handlers ─────────────────────────────
    partial void OnIsFinancialUnlockedChanged(bool value)
    {
        OnPropertyChanged(nameof(LockIcon));
        OnPropertyChanged(nameof(LockButtonText));
    }

    partial void OnCollectionRateTodayChanged(decimal value)
        => OnPropertyChanged(nameof(CollectionRateTodayWidth));

    partial void OnCollectionRateMonthChanged(decimal value)
        => OnPropertyChanged(nameof(CollectionRateMonthWidth));

    partial void OnExpiringBagsChanged(int value)
        => OnPropertyChanged(nameof(HasExpiringBags));

    // ─── Constructor ─────────────────────────────────────────
    public DashboardViewModel() { _ = LoadAsync(); }

    // ─── Load ─────────────────────────────────────────────────
    private async Task LoadAsync()
    {
        IsBusy = true;
        try
        {
            using var db = new AppDbContext();
            var today = DateTime.Today;
            var nextWeek = today.AddDays(7);
            var monthStart = new DateTime(today.Year, today.Month, 1);

            // ══ إحصائيات اليوم ════════════════════════════════

            TodaySales = await db.Invoices
                .Where(i => i.Date.Date == today && !i.IsCancelled)
                .SumAsync(i => (decimal?)i.TotalAmount) ?? 0;

            // إيرادات = PaidAmount من نقدي/جزئي + سندات قبض
            TodayRevenue = await db.Invoices
                .Where(i => i.Date.Date == today && !i.IsCancelled
                         && i.PaymentType != PaymentType.Credit)
                .SumAsync(i => (decimal?)i.PaidAmount) ?? 0;
            TodayRevenue += await db.ReceiptVouchers
                .Where(v => v.Date.Date == today && v.Type == VoucherType.Receipt)
                .SumAsync(v => (decimal?)v.Amount) ?? 0;

            TodayInvoicesCount = await db.Invoices
                .CountAsync(i => i.Date.Date == today && !i.IsCancelled);

            // تصنيف الفواتير: نقدي/جزئي (محصّل) vs آجل (لم يُحصَّل)
            TodayCashInvoices = await db.Invoices.CountAsync(i =>
                i.Date.Date == today && !i.IsCancelled &&
                (i.PaymentType == PaymentType.Cash
                 || i.PaymentType == PaymentType.Partial
                 || i.PaymentType == PaymentType.Electronic));

            TodayCreditInvoices = await db.Invoices.CountAsync(i =>
                i.Date.Date == today && !i.IsCancelled &&
                i.PaymentType == PaymentType.Credit);

            CollectionRateToday = TodaySales > 0
                ? Math.Round(TodayRevenue / TodaySales * 100, 1)
                : 0;

            // ══ إحصائيات الشهر ════════════════════════════════

            MonthSales = await db.Invoices
                .Where(i => i.Date >= monthStart && !i.IsCancelled)
                .SumAsync(i => (decimal?)i.TotalAmount) ?? 0;

            MonthRevenue = await db.Invoices
                .Where(i => i.Date >= monthStart && !i.IsCancelled
                         && i.PaymentType != PaymentType.Credit)
                .SumAsync(i => (decimal?)i.PaidAmount) ?? 0;
            MonthRevenue += await db.ReceiptVouchers
                .Where(v => v.Date >= monthStart && v.Type == VoucherType.Receipt)
                .SumAsync(v => (decimal?)v.Amount) ?? 0;

            MonthInvoicesCount = await db.Invoices
                .CountAsync(i => i.Date >= monthStart && !i.IsCancelled);

            CollectionRateMonth = MonthSales > 0
                ? Math.Round(MonthRevenue / MonthSales * 100, 1)
                : 0;

            // متوسط يومي = مجموع مبيعات الشهر ÷ عدد الأيام المنقضية
            int daysElapsed = Math.Max(today.Day, 1);
            AverageDailySales = Math.Round(MonthSales / daysElapsed, 0);

            // ══ مؤشرات مستمرة ════════════════════════════════

            TotalDebt = await db.Customers.SumAsync(c => (decimal?)c.Balance) ?? 0;
            SafeBalance = await db.Safes.SumAsync(s => (decimal?)s.Balance) ?? 0;
            AvailableBags = await db.BloodBags.CountAsync(b => b.Status == BloodBagStatus.Available);
            ExpiringBags = await db.BloodBags.CountAsync(b =>
                b.Status == BloodBagStatus.Available && b.ExpiryDate <= nextWeek);

            var topDebtor = await db.Customers
                .Where(c => c.Balance > 0)
                .OrderByDescending(c => c.Balance)
                .FirstOrDefaultAsync();
            TopDebtorName = topDebtor?.Name ?? "لا توجد ديون";
            TopDebtorBalance = topDebtor?.Balance ?? 0;

            // ══ رسم مبيعات آخر 7 أيام ════════════════════════

            var weekStart = today.AddDays(-6);
            var rawSales = await db.Invoices
                .Where(i => i.Date.Date >= weekStart && i.Date.Date <= today && !i.IsCancelled)
                .GroupBy(i => i.Date.Date)
                .Select(g => new
                {
                    Date = g.Key,
                    Sales = g.Sum(i => i.TotalAmount),
                    Revenue = g.Sum(i => i.PaidAmount)
                })
                .ToListAsync();

            const double MaxBarH = 110.0;
            const double MinBarH = 4.0;

            var maxSalesVal = rawSales.Any() ? (double)rawSales.Max(d => d.Sales) : 1.0;
            if (maxSalesVal == 0) maxSalesVal = 1;

            var weekPoints = new List<DailySalesPoint>();
            for (int d = 6; d >= 0; d--)
            {
                var date = today.AddDays(-d);
                var data = rawSales.FirstOrDefault(x => x.Date == date);
                var sVal = (double)(data?.Sales ?? 0);
                var rVal = (double)(data?.Revenue ?? 0);

                double sH = sVal > 0 ? Math.Max(MinBarH, sVal / maxSalesVal * MaxBarH) : 0;
                double rH = rVal > 0 ? Math.Max(MinBarH, rVal / maxSalesVal * MaxBarH) : 0;

                weekPoints.Add(new DailySalesPoint
                {
                    Label = GetArabicDayName(date),
                    DateLabel = date.ToString("d/M"),
                    Sales = data?.Sales ?? 0,
                    Revenue = data?.Revenue ?? 0,
                    SalesBarHeight = sH,
                    SalesSpacerHeight = MaxBarH - sH,
                    RevenueBarHeight = rH,
                    RevenueSpacerHeight = MaxBarH - rH,
                    IsToday = date == today
                });
            }
            WeeklySales.Clear();
            weekPoints.ForEach(WeeklySales.Add);

            // ══ توزيع فصائل الدم ══════════════════════════════

            var bagGroups = await db.BloodBags
                .Where(b => b.Status == BloodBagStatus.Available)
                .GroupBy(b => b.BloodGroup)
                .Select(g => new { Group = g.Key, Count = g.Count() })
                .ToListAsync();

            var groupColors = new Dictionary<BloodGroup, string>
            {
                { BloodGroup.APositive,  "#C0392B" }, { BloodGroup.ANegative,  "#922B21" },
                { BloodGroup.BPositive,  "#2471A3" }, { BloodGroup.BNegative,  "#1A5276" },
                { BloodGroup.ABPositive, "#1E8449" }, { BloodGroup.ABNegative, "#196F3D" },
                { BloodGroup.OPositive,  "#D35400" }, { BloodGroup.ONegative,  "#BA4A00" },
            };

            int maxCount = bagGroups.Any() ? bagGroups.Max(g => g.Count) : 1;
            if (maxCount == 0) maxCount = 1;

            BloodGroupStats.Clear();
            foreach (var bg in bagGroups.OrderByDescending(g => g.Count))
            {
                var hex = groupColors.GetValueOrDefault(bg.Group, "#666666");
                var brushColor = (System.Windows.Media.Color)
                    System.Windows.Media.ColorConverter.ConvertFromString(hex);

                BloodGroupStats.Add(new BloodGroupItem
                {
                    GroupName = bg.Group switch
                    {
                        BloodGroup.APositive => "A+",
                        BloodGroup.ANegative => "A-",
                        BloodGroup.BPositive => "B+",
                        BloodGroup.BNegative => "B-",
                        BloodGroup.ABPositive => "AB+",
                        BloodGroup.ABNegative => "AB-",
                        BloodGroup.OPositive => "O+",
                        BloodGroup.ONegative => "O-",
                        _ => bg.Group.ToString()
                    },
                    Count = bg.Count,
                    BarWidth = (double)bg.Count / maxCount * 200.0,
                    Brush = new System.Windows.Media.SolidColorBrush(brushColor)
                });
            }

            // ══ إعدادات القفل ═════════════════════════════════
            var org = await db.OrgSettings.FirstOrDefaultAsync();
            _ownerPinHash = org?.OwnerPinHash ?? string.Empty;
            IsFinancialUnlocked = string.IsNullOrEmpty(_ownerPinHash);
        }
        finally { IsBusy = false; }
    }

    private static string GetArabicDayName(DateTime date) => date.DayOfWeek switch
    {
        DayOfWeek.Saturday => "سبت",
        DayOfWeek.Sunday => "أحد",
        DayOfWeek.Monday => "اثن",
        DayOfWeek.Tuesday => "ثلاث",
        DayOfWeek.Wednesday => "أرب",
        DayOfWeek.Thursday => "خمس",
        DayOfWeek.Friday => "جمعة",
        _ => ""
    };

    [RelayCommand]
    private void ToggleFinancialLock()
    {
        if (IsFinancialUnlocked)
        {
            IsFinancialUnlocked = false;
            return;
        }

        if (string.IsNullOrEmpty(_ownerPinHash))
        {
            IsFinancialUnlocked = true;
            return;
        }

        var enteredPin = Views.PinDialog.Show(
            System.Windows.Application.Current.MainWindow);
        if (enteredPin is null) return;

        if (PinHelper.Verify(enteredPin, _ownerPinHash))
            IsFinancialUnlocked = true;
        else
            ShowError("الرمز السري غير صحيح");
    }
}