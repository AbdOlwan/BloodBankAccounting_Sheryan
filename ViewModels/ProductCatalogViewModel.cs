using BloodBankAccounting.Data;
using BloodBankAccounting.Models;
using BloodBankAccounting.ViewModels.Base;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using System.Collections.ObjectModel;

namespace BloodBankAccounting.ViewModels;

public partial class ProductCatalogViewModel : ViewModelBase
{
    // ════════════════════════════════════════════════════════════
    //  قائمة الأصناف في الجدول
    //  تم التغيير: ProductCatalogItem → BloodProduct
    //  لأن BloodProduct هو المصدر الوحيد للأصناف في التطبيق
    //  وشاشة المخزون تقرأ منه مباشرةً
    // ════════════════════════════════════════════════════════════
    public ObservableCollection<BloodProduct> Items { get; } = [];

    // ════════════════════════════════════════════════════════════
    //  حقول النموذج
    // ════════════════════════════════════════════════════════════
    [ObservableProperty] private string _name = string.Empty;
    [ObservableProperty] private decimal _defaultSalePrice;
    [ObservableProperty] private int? _reorderLevel;

    // ─── وضع التعديل ─────────────────────────────────────────
    /// <summary>null = وضع إضافة | قيمة = وضع تعديل السجل المحدد</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FormTitle))]
    [NotifyPropertyChangedFor(nameof(IsEditMode))]
    private int? _editingId;

    public bool IsEditMode => EditingId.HasValue;
    public string FormTitle => EditingId.HasValue ? "✏️  تعديل الصنف" : "➕  إضافة صنف جديد";

    // ════════════════════════════════════════════════════════════
    //  Constructor
    // ════════════════════════════════════════════════════════════
    public ProductCatalogViewModel()
    {
        _ = LoadAsync();
    }

    // ════════════════════════════════════════════════════════════
    //  تحميل البيانات
    //  تم التغيير: ProductCatalogItems → BloodProducts
    // ════════════════════════════════════════════════════════════
    private async Task LoadAsync()
    {
        IsBusy = true;
        try
        {
            using var db = new AppDbContext();
            var items = await db.BloodProducts
                .OrderBy(p => p.SortOrder)
                .ThenBy(p => p.Name)
                .ToListAsync();

            Items.Clear();
            items.ForEach(Items.Add);
        }
        catch (Exception ex) { ShowError($"خطأ أثناء تحميل البيانات:\n{ex.Message}"); }
        finally { IsBusy = false; }
    }

    // ════════════════════════════════════════════════════════════
    //  زر "جديد" — مسح النموذج والرجوع لوضع الإضافة
    // ════════════════════════════════════════════════════════════
    [RelayCommand]
    private void NewItem()
    {
        ClearForm();
    }

    // ════════════════════════════════════════════════════════════
    //  التحديد من الجدول للتعديل
    //  تم التغيير: ProductCatalogItem → BloodProduct
    // ════════════════════════════════════════════════════════════
    [RelayCommand]
    private void EditItem(BloodProduct item)
    {
        if (item is null) return;

        EditingId = item.Id;
        Name = item.Name;
        DefaultSalePrice = item.DefaultSalePrice;
        ReorderLevel = item.ReorderLevel;
    }

    // ════════════════════════════════════════════════════════════
    //  حفظ (إضافة أو تعديل)
    //  تم التغيير: ProductCatalogItems → BloodProducts
    // ════════════════════════════════════════════════════════════
    [RelayCommand]
    private async Task SaveAsync()
    {
        if (!Validate()) return;

        IsBusy = true;
        try
        {
            using var db = new AppDbContext();

            if (EditingId.HasValue)
            {
                // ── وضع التعديل ──────────────────────────────────────
                var existing = await db.BloodProducts.FindAsync(EditingId.Value);
                if (existing is null) { ShowError("لم يتم العثور على الصنف."); return; }

                // تحقق من التكرار مع استثناء السجل الحالي
                bool duplicate = await db.BloodProducts.AnyAsync(p =>
                    p.Id != EditingId.Value &&
                    p.Name == Name.Trim());

                if (duplicate)
                {
                    ShowError($"يوجد صنف بنفس الاسم بالفعل:\n{Name.Trim()}");
                    return;
                }

                existing.Name = Name.Trim();
                existing.DefaultSalePrice = DefaultSalePrice;
                existing.ReorderLevel = ReorderLevel;
            }
            else
            {
                // ── وضع الإضافة ──────────────────────────────────────
                bool duplicate = await db.BloodProducts.AnyAsync(p =>
                    p.Name == Name.Trim());

                if (duplicate)
                {
                    ShowError($"يوجد صنف بنفس الاسم بالفعل:\n{Name.Trim()}");
                    return;
                }

                // SortOrder = آخر قيمة + 1 لضمان الترتيب الصحيح
                int nextSort = await db.BloodProducts.AnyAsync()
                    ? await db.BloodProducts.MaxAsync(p => p.SortOrder) + 1
                    : 1;

                db.BloodProducts.Add(new BloodProduct
                {
                    Name = Name.Trim(),
                    DefaultSalePrice = DefaultSalePrice,
                    ReorderLevel = ReorderLevel,
                    IsActive = true,
                    SortOrder = nextSort
                });
            }

            await db.SaveChangesAsync();
            ShowSuccess(EditingId.HasValue ? "تم تحديث الصنف بنجاح ✓" : "تمت إضافة الصنف بنجاح ✓");
            ClearForm();
            await LoadAsync();
        }
        catch (Exception ex)
        {
            ShowError($"خطأ أثناء الحفظ:\n{ex.InnerException?.Message ?? ex.Message}");
        }
        finally { IsBusy = false; }
    }

    // ════════════════════════════════════════════════════════════
    //  تعطيل / تفعيل (Soft Delete)
    //  تم التغيير: ProductCatalogItems → BloodProducts
    //              ProductCatalogItem  → BloodProduct
    // ════════════════════════════════════════════════════════════
    [RelayCommand]
    private async Task ToggleActiveAsync(BloodProduct item)
    {
        if (item is null) return;

        string action = item.IsActive ? "تعطيل" : "تفعيل";
        if (!Confirm($"هل تريد {action} الصنف:\n{item.Name}؟"))
            return;

        IsBusy = true;
        try
        {
            using var db = new AppDbContext();
            var entity = await db.BloodProducts.FindAsync(item.Id);
            if (entity is null) return;

            entity.IsActive = !entity.IsActive;
            await db.SaveChangesAsync();
            await LoadAsync();

            if (EditingId == item.Id) ClearForm();
        }
        catch (Exception ex) { ShowError(ex.Message); }
        finally { IsBusy = false; }
    }

    // ════════════════════════════════════════════════════════════
    //  Helpers
    // ════════════════════════════════════════════════════════════
    private void ClearForm()
    {
        EditingId = null;
        Name = string.Empty;
        DefaultSalePrice = 0;
        ReorderLevel = null;
    }

    private bool Validate()
    {
        if (string.IsNullOrWhiteSpace(Name))
        { ShowError("اسم الصنف مطلوب."); return false; }

        if (DefaultSalePrice < 0)
        { ShowError("سعر البيع لا يمكن أن يكون سالباً."); return false; }

        if (ReorderLevel.HasValue && ReorderLevel.Value < 0)
        { ShowError("الحد الأدنى لإعادة الطلب لا يمكن أن يكون سالباً."); return false; }

        return true;
    }
}