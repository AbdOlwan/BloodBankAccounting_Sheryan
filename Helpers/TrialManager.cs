using Microsoft.Win32;
using System.IO;
using System.Management;
using System.Security.Cryptography;
using System.Text;

namespace BloodBankAccounting.Helpers;

/// <summary>
/// نظام التجربة والتفعيل — حماية البرنامج من النسخ غير المرخّصة
/// </summary>
public static class TrialManager
{
    private const int TRIAL_DAYS = 20;
    // ⚠️ يجب أن يطابق تماماً قيمة SECRET في KeyGeneratorTool.csx
    private const string SECRET = "SH3RY4N_BL00D_B4NK_2025_S3CR3T!";

    // مسار الريجستري — تحت CurrentUser لتجنّب الحاجة لأذونات المسؤول
    private const string REG_PATH = @"SOFTWARE\Classes\Applications\BloodBankApp\Config";
    private const string VAL_INSTALL = "Cfg1";
    private const string VAL_LICENSE = "Cfg2";

    // ── Public API ───────────────────────────────────────────────────────────

    /// <summary>
    /// يُرجع معرّف الجهاز الفريد بصيغة XXXX-XXXX-XXXX
    /// مبني على CPU ID + رقم تسلسل قرص النظام
    /// </summary>
    public static string GetMachineId()
    {
        var raw = $"{GetCpuId()}|{GetVolumeSerial()}";

        using var sha = SHA256.Create();
        var hash = sha.ComputeHash(Encoding.UTF8.GetBytes(raw));
        var hex = BitConverter.ToString(hash, 0, 6).Replace("-", "").ToUpper();

        return $"{hex[..4]}-{hex[4..8]}-{hex[8..12]}";
    }

    /// <summary>
    /// الأيام المتبقية من فترة التجربة.
    /// يُرجع int.MaxValue إذا كان البرنامج مفعّلاً.
    /// </summary>
    public static int DaysRemaining()
    {
        if (IsActivated()) return int.MaxValue;

        var installDate = GetOrSetInstallDate();
        var elapsed = (int)(DateTime.Now - installDate).TotalDays;
        return Math.Max(0, TRIAL_DAYS - elapsed);
    }

    /// <summary>هل يحق للبرنامج أن يعمل؟ (مفعّل أو التجربة لم تنته)</summary>
    public static bool CanRun() => IsActivated() || DaysRemaining() > 0;

    /// <summary>هل البرنامج مفعّل بكود صحيح مسجّل على هذا الجهاز؟</summary>
    public static bool IsActivated()
    {
        var stored = ReadReg(VAL_LICENSE);
        return !string.IsNullOrEmpty(stored)
            && stored == ComputeKey(GetMachineId());
    }

    /// <summary>
    /// محاولة تفعيل البرنامج بالكود المُدخَل.
    /// يُرجع true عند النجاح ويحفظ الترخيص في الريجستري.
    /// </summary>
    public static bool Activate(string inputKey)
    {
        var normalized = inputKey.Trim().ToUpper();
        var expected = ComputeKey(GetMachineId());

        if (normalized != expected) return false;

        WriteReg(VAL_LICENSE, expected);
        return true;
    }

    // ── Private helpers ──────────────────────────────────────────────────────

    /// <summary>
    /// يحسب كود التفعيل المتوقع لهذا الجهاز.
    /// نفس الخوارزمية المستخدمة في KeyGeneratorTool.csx.
    /// </summary>
    private static string ComputeKey(string machineId)
    {
        var key = Encoding.UTF8.GetBytes(SECRET);
        var data = Encoding.UTF8.GetBytes(machineId.ToUpper());

        using var hmac = new HMACSHA256(key);
        var hash = hmac.ComputeHash(data);
        var hex = BitConverter.ToString(hash, 0, 8).Replace("-", "").ToUpper();

        return $"{hex[..4]}-{hex[4..8]}-{hex[8..12]}-{hex[12..16]}";
    }

    /// <summary>يجلب تاريخ أول تشغيل، أو يحفظ اليوم إذا كانت أول مرة.</summary>
    private static DateTime GetOrSetInstallDate()
    {
        var val = ReadReg(VAL_INSTALL);

        if (!string.IsNullOrEmpty(val) &&
            DateTime.TryParseExact(val, "yyyyMMdd", null,
                System.Globalization.DateTimeStyles.None, out var date))
            return date;

        // أول تشغيل — سجّل التاريخ
        WriteReg(VAL_INSTALL, DateTime.Now.ToString("yyyyMMdd"));
        return DateTime.Now;
    }

    private static string GetCpuId()
    {
        try
        {
            using var s = new ManagementObjectSearcher(
                "SELECT ProcessorId FROM Win32_Processor");
            foreach (ManagementObject o in s.Get())
                return o["ProcessorId"]?.ToString()?.Trim() ?? "CPU_UNKNOWN";
        }
        catch { /* WMI غير متاح */ }
        return "CPU_UNKNOWN";
    }

    private static string GetVolumeSerial()
    {
        try
        {
            var drive = Path.GetPathRoot(
                Environment.GetFolderPath(Environment.SpecialFolder.System))
                ?.TrimEnd('\\');

            using var s = new ManagementObjectSearcher(
                $"SELECT VolumeSerialNumber FROM Win32_LogicalDisk WHERE DeviceID='{drive}'");
            foreach (ManagementObject o in s.Get())
                return o["VolumeSerialNumber"]?.ToString()?.Trim() ?? "VOL_UNKNOWN";
        }
        catch { /* WMI غير متاح */ }
        return "VOL_UNKNOWN";
    }

    private static string? ReadReg(string name)
    {
        try
        {
            using var k = Registry.CurrentUser.OpenSubKey(REG_PATH);
            return k?.GetValue(name)?.ToString();
        }
        catch { return null; }
    }

    private static void WriteReg(string name, string value)
    {
        try
        {
            using var k = Registry.CurrentUser.CreateSubKey(REG_PATH, writable: true);
            k?.SetValue(name, value);
        }
        catch { /* silent fail — مش هيأثر على التشغيل */ }
    }
}