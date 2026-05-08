using BloodBankAccounting.Data;
using BloodBankAccounting.Models;
using Microsoft.EntityFrameworkCore;

namespace BloodBankAccounting.Printing;

/// <summary>
/// يُحمِّل إعدادات المؤسسة مرة واحدة ويخزّنها — استدعِ Invalidate()
/// بعد أي حفظ في OrganizationSettings لإعادة التحميل عند الطباعة التالية.
/// </summary>
public static class OrgInfoCache
{
    private static OrganizationSettings? _cached;

    public static async Task<OrganizationSettings> GetAsync()
    {
        if (_cached is not null) return _cached;
        using var db = new AppDbContext();
        _cached = await db.OrgSettings.FirstOrDefaultAsync()
                  ?? new OrganizationSettings();
        return _cached;
    }

    public static void Invalidate() => _cached = null;
}
