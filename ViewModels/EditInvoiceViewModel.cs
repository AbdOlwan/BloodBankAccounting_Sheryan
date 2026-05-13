using BloodBankAccounting.Data;
using BloodBankAccounting.Helpers;
using BloodBankAccounting.Models;
using BloodBankAccounting.ViewModels.Base;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using System.Collections.ObjectModel;

namespace BloodBankAccounting.ViewModels;

/// <summary>
/// ViewModel لتعديل فاتورة موجودة.
/// يفتح كـ Dialog من InvoiceListViewModel أو SafeReportViewModel.
/// ما يسمح بتعديله: التاريخ، الخصم، الأصناف اليدوية (اسم/سعر/كمية)، الملاحظات.
/// الأصناف المرتبطة بكيس دم لا تُعدَّل (تحتاج إلغاء كامل للفاتورة).
/// </summary>
public partial class EditInvoiceViewModel : ViewModelBase
{
    // ════════════════════════════════════════════════════════════
    //  Header
    // ════════════════════════════════════════════════════════════
    [ObservableProperty] private int _invoiceDbId;
    [ObservableProperty] private string _invoiceNumber = string.Empty;
    [ObservableProperty] private string _customerName = string.Empty;
    [ObservableProperty] private DateTime _date = DateTime.Today;
    [ObservableProperty] private string _notes = string.Empty;

    // ─── نوع الدفع (للعرض فقط — لا يتغير) ────────────────────
    [ObservableProperty] private string _paymentTypeDisplay = string.Empty;

    // ─── الخصم ────────────────────────────────────────────────
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TotalAmount))]
    private decimal _discountAmount;

    // ─── الإجمالي (محسوب) ────────────────────────────────────
    public decimal SubTotal => Lines.Sum(l => l.Total);
    public decimal TotalAmount => Math.Max(0, SubTotal - DiscountAmount);

    // ─── القيم الأصلية قبل التعديل (لحساب الفرق) ─────────────
    private decimal _originalDiscountAmount;
    private decimal _originalTotalAmount;
    private decimal _originalPaidAmount;
    private DateTime _originalDate;
    private string _originalNotes = string.Empty;

    // ─── صناديق PIN ───────────────────────────────────────────
    private string _ownerPinHash = string.Empty;

    // ─── بيانات الخزينة / العميل (للتسوية المالية) ────────────
    private int? _safeId;
    private int _customerId;
    private PaymentType _paymentType;
    private decimal _originalPaidAmountForJournal;

    // ════════════════════════════════════════════════════════════
    //  Lines
    // ════════════════════════════════════════════════════════════
    public ObservableCollection<EditableInvoiceLine> Lines { get; } = [];

    // ── إضافة صنف يدوي جديد ──────────────────────────────────
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
        "كرات دم مكدسه موجب",
        "كرات دم مكدسه سالب"
    ];

    // ════════════════════════════════════════════════════════════
    //  Result
    // ════════════════════════════════════════════════════════════
    /// <summary>يُضبط على true بعد الحفظ الناجح</summary>
    [ObservableProperty] private bool _saved;

    // ════════════════════════════════════════════════════════════
    //  Load
    // ════════════════════════════════════════════════════════════
    public EditInvoiceViewModel(int invoiceId)
    {
        InvoiceDbId = invoiceId;
        _ = LoadAsync(invoiceId);
    }

    private async Task LoadAsync(int invoiceId)
    {
        IsBusy = true;
        try
        {
            using var db = new AppDbContext();

            var inv = await db.Invoices
                .Include(i => i.Customer)
                .Include(i => i.Lines)
                .FirstOrDefaultAsync(i => i.Id == invoiceId);

            if (inv is null) { ShowError("لم يتم العثور على الفاتورة."); return; }
            if (inv.IsCancelled) { ShowError("لا يمكن تعديل فاتورة ملغاة."); return; }

            var org = await db.OrgSettings.FirstOrDefaultAsync();
            _ownerPinHash = org?.OwnerPinHash ?? string.Empty;

            // ── حفظ القيم الأصلية ────────────────────────────
            _originalDate = inv.Date;
            _originalDiscountAmount = inv.DiscountAmount;
            _originalTotalAmount = inv.TotalAmount;
            _originalPaidAmount = inv.PaidAmount;
            _originalNotes = inv.Notes ?? string.Empty;
            _safeId = inv.SafeId;
            _customerId = inv.CustomerId;
            _paymentType = inv.PaymentType;
            _originalPaidAmountForJournal = inv.PaidAmount;

            // ── ملء الـ UI ────────────────────────────────────
            InvoiceNumber = inv.InvoiceNumber;
            CustomerName = inv.Customer.Name;
            Date = inv.Date;
            DiscountAmount = inv.DiscountAmount;
            Notes = inv.Notes ?? string.Empty;
            PaymentTypeDisplay = inv.PaymentType switch
            {
                PaymentType.Cash => "نقدي",
                PaymentType.Credit => "آجل",
                PaymentType.Partial => "جزئي",
                PaymentType.Electronic => "إلكتروني",
                _ => inv.PaymentType.ToString()
            };

            Lines.Clear();
            foreach (var line in inv.Lines)
            {
                var el = new EditableInvoiceLine
                {
                    Id = line.Id,
                    BloodBagId = line.BloodBagId,
                    IsManual = !line.BloodBagId.HasValue,
                    ProductName = line.ProductName,
                    UnitPrice = line.UnitPrice,
                    Quantity = line.Quantity,
                    IsOriginal = true
                };
                el.PropertyChanged += (_, _) =>
                {
                    OnPropertyChanged(nameof(SubTotal));
                    OnPropertyChanged(nameof(TotalAmount));
                };
                Lines.Add(el);
            }

            OnPropertyChanged(nameof(SubTotal));
            OnPropertyChanged(nameof(TotalAmount));
        }
        finally { IsBusy = false; }
    }

    // ════════════════════════════════════════════════════════════
    //  Lines Commands
    // ════════════════════════════════════════════════════════════
    [RelayCommand]
    private void AddManualLine()
    {
        if (string.IsNullOrWhiteSpace(NewLineName))
        { ShowError("أدخل اسم الصنف"); return; }
        if (NewLineQty <= 0)
        { ShowError("الكمية يجب أن تكون أكبر من صفر"); return; }
        if (NewLineUnitPrice <= 0)
        { ShowError("أدخل سعر الوحدة"); return; }

        var el = new EditableInvoiceLine
        {
            Id = 0,   // جديد — لم يُحفظ بعد
            BloodBagId = null,
            IsManual = true,
            IsOriginal = false,
            ProductName = NewLineName.Trim(),
            UnitPrice = NewLineUnitPrice,
            Quantity = NewLineQty
        };
        el.PropertyChanged += (_, _) =>
        {
            OnPropertyChanged(nameof(SubTotal));
            OnPropertyChanged(nameof(TotalAmount));
        };
        Lines.Add(el);

        OnPropertyChanged(nameof(SubTotal));
        OnPropertyChanged(nameof(TotalAmount));

        NewLineName = string.Empty;
        NewLineQty = 1;
        NewLineUnitPrice = 0;
    }

    [RelayCommand]
    private void RemoveLine(EditableInvoiceLine line)
    {
        // لا تحذف أصناف مرتبطة بكيس دم — تحتاج إلغاء كامل
        if (line.BloodBagId.HasValue)
        {
            ShowError("لا يمكن حذف صنف مرتبط بكيس دم.\nلإزالته يجب إلغاء الفاتورة كاملاً وإعادة الإصدار.");
            return;
        }
        Lines.Remove(line);
        OnPropertyChanged(nameof(SubTotal));
        OnPropertyChanged(nameof(TotalAmount));
    }

    partial void OnDiscountAmountChanged(decimal value)
    {
        OnPropertyChanged(nameof(TotalAmount));
    }

    // ════════════════════════════════════════════════════════════
    //  Validation
    // ════════════════════════════════════════════════════════════
    private bool ValidateBeforeSave()
    {
        if (!Lines.Any())
        { ShowError("يجب أن تحتوي الفاتورة على صنف واحد على الأقل."); return false; }

        if (DiscountAmount < 0)
        { ShowError("الخصم لا يمكن أن يكون قيمة سالبة."); return false; }

        if (DiscountAmount >= SubTotal && SubTotal > 0)
        { ShowError("الخصم لا يمكن أن يساوي أو يتجاوز إجمالي الأصناف."); return false; }

        foreach (var line in Lines)
        {
            if (string.IsNullOrWhiteSpace(line.ProductName))
            { ShowError("اسم الصنف لا يمكن أن يكون فارغاً."); return false; }
            if (line.Quantity <= 0)
            { ShowError($"كمية الصنف «{line.ProductName}» يجب أن تكون أكبر من صفر."); return false; }
            if (line.UnitPrice < 0)
            { ShowError($"سعر الصنف «{line.ProductName}» لا يمكن أن يكون سالباً."); return false; }
        }

        if (Date > DateTime.Today)
        { ShowError("تاريخ الفاتورة لا يمكن أن يكون في المستقبل."); return false; }

        return true;
    }

    // ════════════════════════════════════════════════════════════
    //  Save
    // ════════════════════════════════════════════════════════════
    [RelayCommand]
    private async Task SaveAsync()
    {
        // ── 1. Validation ─────────────────────────────────────
        if (!ValidateBeforeSave()) return;

        // ── 2. التحقق من وجود أي تغيير فعلي ──────────────────
        bool hasChange = Date != _originalDate
                      || DiscountAmount != _originalDiscountAmount
                      || Notes.Trim() != _originalNotes.Trim()
                      || HasLineChanges();

        if (!hasChange)
        {
            ShowError("لم تقم بأي تعديل على الفاتورة.");
            return;
        }

        // ── 3. طلب الرمز السري ────────────────────────────────
        if (!string.IsNullOrEmpty(_ownerPinHash))
        {
            var enteredPin = Views.PinDialog.Show(
                System.Windows.Application.Current.MainWindow);
            if (enteredPin is null) return;   // ألغى

            if (!PinHelper.Verify(enteredPin, _ownerPinHash))
            {
                ShowError("الرمز السري غير صحيح. لم يتم التعديل.");
                return;
            }
        }

        // ── 4. ملخص التغييرات للتأكيد الأول ──────────────────
        var summaryLines = BuildChangeSummary();

        if (!Confirm(
            $"مراجعة التعديلات على الفاتورة {InvoiceNumber}\n" +
            $"العميل: {CustomerName}\n\n" +
            summaryLines +
            "\n\nهل تريد المتابعة؟"))
            return;

        // ── 5. تأكيد ثانٍ نهائي ───────────────────────────────
        var newTotal = TotalAmount;
        var diff = newTotal - _originalTotalAmount;
        var diffText = diff == 0 ? "لا تغيير في الإجمالي"
                     : diff > 0 ? $"زيادة الإجمالي بمقدار {diff:N2} جنيه"
                     : $"تخفيض الإجمالي بمقدار {Math.Abs(diff):N2} جنيه";

        if (!Confirm(
            $"⚠️ تأكيد نهائي — هذا الإجراء لا يمكن التراجع عنه\n\n" +
            $"الفاتورة: {InvoiceNumber}\n" +
            $"الإجمالي القديم: {_originalTotalAmount:N2} جنيه\n" +
            $"الإجمالي الجديد: {newTotal:N2} جنيه\n" +
            $"الأثر المالي: {diffText}\n\n" +
            "هل أنت متأكد تماماً من تنفيذ هذا التعديل؟"))
            return;

        // ── 6. الحفظ في قاعدة البيانات ────────────────────────
        IsBusy = true;
        try
        {
            using var db = new AppDbContext();
            await using var tx = await db.Database.BeginTransactionAsync();

            var inv = await db.Invoices
                .Include(i => i.Lines)
                .Include(i => i.JournalEntries)
                .FirstOrDefaultAsync(i => i.Id == InvoiceDbId);

            if (inv is null) { ShowError("لم يتم العثور على الفاتورة."); return; }
            if (inv.IsCancelled) { ShowError("لا يمكن تعديل فاتورة ملغاة."); return; }

            // ── a. تسوية الخزينة / العميل ──────────────────────
            await ReconcileFinancialsAsync(db, inv, newTotal);

            // ── b. حذف القيود القديمة وإعادة تسجيلها ─────────
            if (inv.JournalEntries.Any())
                db.JournalEntries.RemoveRange(inv.JournalEntries);

            // ── c. تحديث الفاتورة ──────────────────────────────
            inv.Date = Date;
            inv.DiscountAmount = DiscountAmount;
            inv.SubTotal = SubTotal;
            inv.TotalAmount = newTotal;
            inv.Notes = Notes.Trim();
            // PaidAmount لا يتغير تلقائياً إلا في حالة Cash حيث Paid = Total
            if (inv.PaymentType == PaymentType.Cash)
                inv.PaidAmount = newTotal;

            // ── d. تحديث الأصناف ───────────────────────────────
            await SyncLinesAsync(db, inv);

            await db.SaveChangesAsync();

            // ── e. إعادة تسجيل القيود المحاسبية ──────────────
            var customer = await db.Customers.FindAsync(_customerId);
            inv.Customer = customer!;

            // Load a fallback Safe (main safe) for cases where SafeId may be null
            Safe? fallbackSafe = await db.Safes.FirstOrDefaultAsync(s => s.IsMain);

            if (inv.PaymentType == PaymentType.Partial)
            {
                var partialSafe = inv.SafeId.HasValue
                    ? await db.Safes.FindAsync(inv.SafeId.Value)
                    : fallbackSafe;
                if (partialSafe is not null)
                    JournalService.PostPartialInvoice(db, inv, inv.PaidAmount, partialSafe);
            }
            else if (inv.PaymentType == PaymentType.Electronic && inv.SafeId.HasValue)
            {
                var elecSafe = await db.Safes.FindAsync(inv.SafeId.Value);
                if (elecSafe is not null)
                    JournalService.PostInvoice(db, inv, elecSafe);
            }
            else
            {
                // Cash → uses SafeId's safe (or main safe); Credit → PostInvoice uses AccountsRec branch, safe is ignored
                var cashSafe = inv.SafeId.HasValue
                    ? await db.Safes.FindAsync(inv.SafeId.Value)
                    : fallbackSafe;
                if (cashSafe is not null)
                    JournalService.PostInvoice(db, inv, cashSafe);
            }

            await db.SaveChangesAsync();

            // ── f. حفظ سجل التدقيق ────────────────────────────
            var financialImpact = diff == 0
                ? "لا تغيير في الإجمالي"
                : diff > 0
                    ? $"زيادة {diff:N2} جنيه"
                    : $"تخفيض {Math.Abs(diff):N2} جنيه";

            db.InvoiceAuditLogs.Add(new InvoiceAuditLog
            {
                InvoiceId = InvoiceDbId,
                EditedAt = DateTime.Now,
                OldDate = _originalDate,
                OldDiscountAmount = _originalDiscountAmount,
                OldTotalAmount = _originalTotalAmount,
                OldPaidAmount = _originalPaidAmount,
                OldNotes = _originalNotes,
                NewDate = Date,
                NewDiscountAmount = DiscountAmount,
                NewTotalAmount = newTotal,
                NewPaidAmount = inv.PaidAmount,
                NewNotes = Notes.Trim(),
                FinancialImpactSummary = financialImpact
            });

            await db.SaveChangesAsync();
            await tx.CommitAsync();

            Saved = true;
            ShowSuccess($"تم تعديل الفاتورة {InvoiceNumber} بنجاح ✓\n{financialImpact}");
        }
        catch (Exception ex)
        {
            var msg = ex.InnerException?.Message ?? ex.Message;
            ShowError($"حدث خطأ أثناء التعديل:\n{msg}");
        }
        finally { IsBusy = false; }
    }

    // ════════════════════════════════════════════════════════════
    //  Helpers
    // ════════════════════════════════════════════════════════════

    /// <summary>
    /// تسوية الخزينة ورصيد العميل بناءً على الفرق بين الإجمالي القديم والجديد.
    /// </summary>
    private async Task ReconcileFinancialsAsync(AppDbContext db, Invoice inv, decimal newTotal)
    {
        var diff = newTotal - _originalTotalAmount;
        if (diff == 0) return;

        switch (inv.PaymentType)
        {
            case PaymentType.Cash:
            case PaymentType.Electronic:
                // نقدي أو إلكتروني — نضبط الخزينة بالفرق
                if (inv.SafeId.HasValue)
                {
                    var safe = await db.Safes.FindAsync(inv.SafeId.Value);
                    if (safe is not null)
                        safe.Balance += diff;
                }
                break;

            case PaymentType.Credit:
                var custCredit = await db.Customers.FindAsync(inv.CustomerId);
                if (custCredit is not null)
                    custCredit.Balance += diff;
                break;

            case PaymentType.Partial:
                var custPartial = await db.Customers.FindAsync(inv.CustomerId);
                if (custPartial is not null)
                    custPartial.Balance += diff;
                break;

        }
    }

    /// <summary>
    /// مزامنة الأصناف: حذف المحذوفة، تحديث الموجودة، إضافة الجديدة.
    /// الأصناف المرتبطة بكيس دم تُحدَّث فقط (لا تُحذف).
    /// </summary>
    private Task SyncLinesAsync(AppDbContext db, Invoice inv)
    {
        var uiIds = Lines
            .Where(l => l.Id > 0)
            .Select(l => l.Id)
            .ToHashSet();

        // حذف الأصناف التي أُزيلت من الـ UI (يدوية فقط)
        foreach (var dbLine in inv.Lines.ToList())
        {
            if (!uiIds.Contains(dbLine.Id) && !dbLine.BloodBagId.HasValue)
                db.InvoiceLines.Remove(dbLine);
        }

        foreach (var uiLine in Lines)
        {
            if (uiLine.Id > 0)
            {
                // تحديث صنف موجود
                var dbLine = inv.Lines.FirstOrDefault(l => l.Id == uiLine.Id);
                if (dbLine is not null)
                {
                    dbLine.ProductName = uiLine.ProductName;
                    dbLine.UnitPrice = uiLine.UnitPrice;
                    dbLine.Quantity = uiLine.Quantity;
                    dbLine.Total = uiLine.Total;
                }
            }
            else
            {
                // إضافة صنف جديد
                db.InvoiceLines.Add(new InvoiceLine
                {
                    InvoiceId = inv.Id,
                    BloodBagId = null,
                    ProductName = uiLine.ProductName,
                    UnitPrice = uiLine.UnitPrice,
                    Quantity = uiLine.Quantity,
                    Total = uiLine.Total
                });
            }
        }
        return Task.CompletedTask;
    
    }

    private bool HasLineChanges()
    {
        // أصناف جديدة مضافة
        if (Lines.Any(l => l.Id == 0)) return true;

        // أصناف أصلية حُذفت من الـ UI
        // (يصعب تتبعها هنا، لكن SyncLinesAsync تتكفل بها)
        // نقارن المجموع كمؤشر سريع
        if (SubTotal != Lines.Where(l => l.IsOriginal).Sum(l => l.Total))
            return true;

        return false;
    }

    private string BuildChangeSummary()
    {
        var sb = new System.Text.StringBuilder();

        if (Date != _originalDate)
            sb.AppendLine($"• التاريخ: {_originalDate:d/M/yyyy} ← {Date:d/M/yyyy}");

        if (DiscountAmount != _originalDiscountAmount)
            sb.AppendLine($"• الخصم: {_originalDiscountAmount:N2} ← {DiscountAmount:N2} جنيه");

        if (TotalAmount != _originalTotalAmount)
            sb.AppendLine($"• الإجمالي: {_originalTotalAmount:N2} ← {TotalAmount:N2} جنيه");

        if (Notes.Trim() != _originalNotes.Trim())
            sb.AppendLine("• الملاحظات: تم التعديل");

        if (Lines.Any(l => l.Id == 0))
            sb.AppendLine($"• أصناف جديدة مضافة: {Lines.Count(l => l.Id == 0)}");

        return sb.Length > 0 ? sb.ToString().TrimEnd() : "تعديلات في تفاصيل الأصناف";
    }

    [RelayCommand]
    private void Cancel()
    {
        // تُغلق النافذة من code-behind عند الضغط على إلغاء
    }
}

// ════════════════════════════════════════════════════════════
//  صنف معدَّل في الفاتورة
// ════════════════════════════════════════════════════════════
public partial class EditableInvoiceLine : ObservableObject
{
    public int Id { get; set; }
    public int? BloodBagId { get; set; }
    public bool IsManual { get; set; }
    /// <summary>true = صنف أصلي من DB | false = مضاف جديد</summary>
    public bool IsOriginal { get; set; }

    [ObservableProperty] private string _productName = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Total))]
    private decimal _unitPrice;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Total))]
    private int _quantity = 1;

    public decimal Total => UnitPrice * Quantity;

    /// <summary>الأصناف المرتبطة بكيس دم لا تُعدَّل ولا تُحذف</summary>
    public bool IsEditable => !BloodBagId.HasValue;
}