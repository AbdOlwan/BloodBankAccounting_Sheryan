using BloodBankAccounting.Data;
using BloodBankAccounting.Models;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.IO;

namespace BloodBankAccounting.Printing;

public static class InvoicePrinter
{
    private const string ColPrimary = "#C0392B";
    private const string ColDark = "#1D1D1F";
    private const string ColSecondary = "#6E6E73";
    private const string ColSurface = "#F5F5F7";
    private const string ColBorder = "#D2D2D7";
    private const string ColWhite = "#FFFFFF";
    private const string ColGreen = "#1E8449";
    private const string ColOrange = "#D35400";   // لون الخصم
    private const string ColBrand = "#8B7355";   // لون الـ branding

    public static async Task PrintAsync(int invoiceId)
    {
        using var db = new AppDbContext();
        var invoice = await db.Invoices
            .Include(i => i.Customer)
            .Include(i => i.Lines)
            .FirstAsync(i => i.Id == invoiceId);

        // ── تحميل بيانات المؤسسة ──────────────────────────
        var org = await OrgInfoCache.GetAsync();

        byte[]? logoBytes = null;
        if (!string.IsNullOrWhiteSpace(org.LogoPath) && File.Exists(org.LogoPath))
            logoBytes = await File.ReadAllBytesAsync(org.LogoPath);

        var path = Path.Combine(Path.GetTempPath(), $"Invoice_{invoice.InvoiceNumber}_{DateTime.Now:yyyyMMddHHmmss}.pdf");

        var doc = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(2.2f, Unit.Centimetre);
                page.DefaultTextStyle(x => x
                    .FontFamily("Arial")
                    .FontSize(11)
                    .FontColor(ColDark)
                    .DirectionFromRightToLeft());

                page.Content().Column(col =>
                {
                    // ─── Header ───────────────────────────────────────────
                    col.Item().Row(row =>
                    {
                        // LEFT visual: info box
                        row.ConstantItem(200)
                            .Border(0.5f).BorderColor(ColBorder)
                            .Background(ColSurface).Padding(12)
                            .Column(inner =>
                            {
                                inner.Item()
                                    .Text("فاتورة مبيعات")
                                    .FontSize(12).Bold().FontColor(ColPrimary).AlignCenter();

                                inner.Item().PaddingTop(8).Row(r =>
                                {
                                    r.RelativeItem().AlignRight()
                                        .Text(invoice.InvoiceNumber).Bold();
                                    r.ConstantItem(8);
                                    r.ConstantItem(80).AlignLeft()
                                        .Text("رقم الفاتورة").FontSize(9).FontColor(ColSecondary);
                                });
                                inner.Item().PaddingTop(4).Row(r =>
                                {
                                    r.RelativeItem().AlignRight()
                                        .Text(invoice.Date.ToString("d/M/yyyy"));
                                    r.ConstantItem(8);
                                    r.ConstantItem(80).AlignLeft()
                                        .Text("التاريخ").FontSize(9).FontColor(ColSecondary);
                                });
                            });

                        row.ConstantItem(16);

                        // RIGHT visual: bank name + optional logo
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().Row(r =>
                            {
                                if (logoBytes is not null)
                                {
                                    r.ConstantItem(52).Height(52)
                                        .Image(logoBytes).FitArea();
                                    r.ConstantItem(10);
                                }
                                r.RelativeItem().Column(inner =>
                                {
                                    inner.Item()
                                        .Text(org.BankName)
                                        .FontSize(logoBytes is null ? 26 : 20)
                                        .Bold().FontColor(ColPrimary).AlignRight();
                                    if (!string.IsNullOrWhiteSpace(org.SubTitle))
                                        inner.Item().PaddingTop(3)
                                            .Text(org.SubTitle)
                                            .FontSize(10).FontColor(ColSecondary).AlignRight();
                                    if (!string.IsNullOrWhiteSpace(org.Phone))
                                        inner.Item().PaddingTop(2)
                                            .Text($"📞  {org.Phone}")
                                            .FontSize(9).FontColor(ColSecondary).AlignRight();
                                    if (!string.IsNullOrWhiteSpace(org.Address))
                                        inner.Item()
                                            .Text(org.Address)
                                            .FontSize(9).FontColor(ColSecondary).AlignRight();
                                });
                            });
                        });
                    });

                    col.Item().PaddingVertical(14)
                        .LineHorizontal(0.5f).LineColor(ColBorder);

                    // ─── Customer Info ────────────────────────────────────
                    col.Item().Border(0.5f).BorderColor(ColBorder).Padding(14).Row(row =>
                    {
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().Text("رقم الهاتف")
                                .FontSize(9).FontColor(ColSecondary).AlignLeft();
                            c.Item().PaddingTop(3)
                                .Text(invoice.Customer.Phone ?? "—")
                                .FontSize(13).Bold().AlignLeft();
                        });

                        row.ConstantItem(16);
                        row.ConstantItem(1).Background(ColBorder);
                        row.ConstantItem(16);

                        row.RelativeItem().Column(c =>
                        {
                            c.Item().Text("بيانات العميل")
                                .FontSize(9).FontColor(ColSecondary).AlignRight();
                            c.Item().PaddingTop(3)
                                .Text(invoice.Customer.Name)
                                .FontSize(13).Bold().AlignRight();
                        });
                    });

                    // ─── Payment & Notes ─────────────────────────────────
                    if (!string.IsNullOrWhiteSpace(invoice.Notes) || invoice.PaymentType != PaymentType.Cash)
                    {
                        col.Item().PaddingTop(10).Border(0.5f).BorderColor(ColBorder)
                            .Background(ColSurface).Padding(12).Row(row =>
                            {
                                row.RelativeItem().Column(c =>
                                {
                                    c.Item().Text("ملاحظات")
                                        .FontSize(9).FontColor(ColSecondary).AlignLeft();
                                    c.Item().PaddingTop(3)
                                        .Text(string.IsNullOrWhiteSpace(invoice.Notes) ? "—" : invoice.Notes)
                                        .FontSize(11).AlignLeft();
                                });

                                row.ConstantItem(1).Background(ColBorder);
                                row.ConstantItem(16);

                                row.ConstantItem(160).Column(c =>
                                {
                                    c.Item().Text("طريقة الدفع")
                                        .FontSize(9).FontColor(ColSecondary).AlignRight();
                                    c.Item().PaddingTop(3)
                                        .Text(invoice.PaymentType switch
                                        {
                                            PaymentType.Cash => "نقدي",
                                            PaymentType.Credit => "آجل",
                                            PaymentType.Partial => $"جزئي — مدفوع: {invoice.PaidAmount:N2} ج.م",
                                            _ => invoice.PaymentType.ToString()
                                        })
                                        .FontSize(11).Bold().AlignRight();
                                });
                            });
                    }

                    // ─── Lines Table ──────────────────────────────────────
                    col.Item().PaddingTop(18).Border(0.5f).BorderColor(ColBorder).Table(table =>
                    {
                        table.ColumnsDefinition(cols =>
                        {
                            cols.ConstantColumn(100);
                            cols.RelativeColumn(2);
                            cols.RelativeColumn(2);
                            cols.RelativeColumn(4);
                            cols.ConstantColumn(36);
                        });

                        static IContainer TH(IContainer c) =>
                            c.Border(0.5f).BorderColor(ColBorder)
                             .Background(ColPrimary)
                             .Padding(10).AlignCenter();

                        table.Cell().Element(TH).Text("الإجمالي")
                            .FontColor(ColWhite).Bold();
                        table.Cell().Element(TH).Text("سعر الوحدة")
                            .FontColor(ColWhite).Bold();
                        table.Cell().Element(TH).Text("الكمية")
                            .FontColor(ColWhite).Bold();
                        table.Cell().Element(TH).Text("الصنف / المنتج")
                            .FontColor(ColWhite).Bold();
                        table.Cell().Element(TH).Text("#")
                            .FontColor(ColWhite).Bold();

                        int i = 1;
                        foreach (var line in invoice.Lines)
                        {
                            var bg = i % 2 == 0 ? ColSurface : ColWhite;

                            table.Cell().Element(c => TD(c, bg)).AlignCenter()
                                .Text($"{line.Total:N2} ج.م").Bold().FontColor(ColGreen);
                            table.Cell().Element(c => TD(c, bg)).AlignCenter()
                                .Text($"{line.UnitPrice:N2} ج.م");
                            table.Cell().Element(c => TD(c, bg)).AlignCenter()
                                .Text(line.Quantity.ToString());
                            table.Cell().Element(c => TD(c, bg)).AlignRight()
                                .Text(line.ProductName);
                            table.Cell().Element(c => TD(c, bg)).AlignCenter()
                                .Text(i.ToString()).FontColor(ColSecondary);
                            i++;
                        }

                        static IContainer TD(IContainer c, string bg) =>
                            c.Border(0.5f).BorderColor(ColBorder).Background(bg).Padding(10);
                    });

                    // ─── Totals Box ───────────────────────────────────────
                    // يعرض: إجمالي الأصناف / الخصم (إن وجد) / صافي الفاتورة
                    col.Item().PaddingTop(2).Row(row =>
                    {
                        row.RelativeItem(); // spacer يدفع الجدول لليمين

                        row.ConstantItem(300)
                            .Border(0.5f).BorderColor(ColBorder)
                            .Background(ColSurface)
                            .Column(totals =>
                            {
                                // ── صف: إجمالي الأصناف ───────────────────
                                totals.Item()
                                    .BorderBottom(invoice.DiscountAmount > 0 ? 0.5f : 0f)
                                    .BorderColor(ColBorder)
                                    .Padding(12)
                                    .Row(r =>
                                    {
                                        r.RelativeItem().AlignRight()
                                            .Text($"{invoice.SubTotal:N2} ج.م")
                                            .FontSize(12).Bold().FontColor(ColDark);
                                        r.ConstantItem(8);
                                        r.ConstantItem(110).AlignLeft()
                                            .Text("إجمالي الأصناف")
                                            .FontSize(9).FontColor(ColSecondary).AlignCenter();
                                    });

                                // ── صف: الخصم — يظهر فقط إذا كان > 0 ────
                                if (invoice.DiscountAmount > 0)
                                {
                                    totals.Item()
                                        .BorderBottom(0.5f).BorderColor(ColBorder)
                                        .Padding(12)
                                        .Row(r =>
                                        {
                                            r.RelativeItem().AlignRight()
                                                .Text(t =>
                                                {
                                                    t.Span("− ").FontColor(ColOrange).Bold();
                                                    t.Span($"{invoice.DiscountAmount:N2} ج.م")
                                                        .FontSize(12).Bold().FontColor(ColOrange);
                                                });
                                            r.ConstantItem(8);
                                            r.ConstantItem(110).AlignLeft()
                                                .Text("الخصم")
                                                .FontSize(9).FontColor(ColSecondary).AlignCenter();
                                        });
                                }

                                // ── صف: صافي الفاتورة ────────────────────
                                totals.Item()
                                    .Background(ColPrimary)
                                    .Padding(14)
                                    .Row(r =>
                                    {
                                        r.RelativeItem().AlignRight()
                                            .Text($"{invoice.TotalAmount:N2} ج.م")
                                            .FontSize(15).Bold().FontColor(ColWhite);
                                        r.ConstantItem(8);
                                        r.ConstantItem(110).AlignLeft()
                                            .Text("صافي الفاتورة")
                                            .FontSize(10).FontColor(ColWhite).AlignCenter();
                                    });
                            });
                    });

                    // ─── Thank-you + Branding ─────────────────────────────
                    col.Item().PaddingTop(20).AlignCenter()
                        .Text("شكراً لتعاملكم معنا")
                        .FontSize(10).FontColor(ColSecondary);

                    col.Item().PaddingTop(8).LineHorizontal(0.5f).LineColor("#EEEEEE");

                    col.Item().PaddingTop(6).AlignCenter()
                        .Text(t =>
                        {
                            t.Span("برنامج حسابات شريان  |  ")
                                .FontSize(8).FontColor(ColBrand);
                            t.Span("م. عبدالرحمن علوان")
                                .FontSize(8).FontColor(ColBrand).Bold();
                            t.Span("  ✦  01127136834")
                                .FontSize(8).FontColor(ColBrand);
                        });
                });
            });
        });

        doc.GeneratePdf(path);
        System.Diagnostics.Process.Start(
            new System.Diagnostics.ProcessStartInfo(path) { UseShellExecute = true });
    }
}