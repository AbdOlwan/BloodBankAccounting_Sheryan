using BloodBankAccounting.Data;
using BloodBankAccounting.Helpers;
using BloodBankAccounting.Models;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.IO;

namespace BloodBankAccounting.Printing;

public static class PaymentPrinter
{
    private const string ColPrimary = "#C0392B";
    private const string ColDark = "#1D1D1F";
    private const string ColSecondary = "#6E6E73";
    private const string ColSurface = "#F5F5F7";
    private const string ColBorder = "#D2D2D7";
    private const string ColWhite = "#FFFFFF";
    private const string ColBrand = "#8B7355";

    public static async Task PrintAsync(int voucherId)
    {
        using var db = new AppDbContext();
        var v = await db.ReceiptVouchers
            .Include(r => r.Safe)
            .FirstAsync(r => r.Id == voucherId);

        // ── تحميل بيانات المؤسسة ──────────────────────────
        var org = await OrgInfoCache.GetAsync();

        byte[]? logoBytes = null;
        if (!string.IsNullOrWhiteSpace(org.LogoPath) && File.Exists(org.LogoPath))
            logoBytes = await File.ReadAllBytesAsync(org.LogoPath);

        var path = Path.Combine(Path.GetTempPath(), $"Payment_{v.VoucherNumber}.pdf");
        var inWords = ArabicNumberHelper.ToWords(v.Amount);

        var doc = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(1.4f, Unit.Centimetre);
                page.DefaultTextStyle(x => x
                    .FontFamily("Arial")
                    .FontSize(11)
                    .FontColor(ColDark)
                    .DirectionFromRightToLeft());

                page.Content().Column(col =>
                {
                    string[] labels = { "نسخة العميل", "نسخة المحاسبة", "نسخة الأرشيف" };
                    for (int i = 0; i < 3; i++)
                    {
                        col.Item().Element(e => BuildCopy(e, v, org, logoBytes, inWords, labels[i]));
                        if (i < 2)
                            col.Item().PaddingVertical(6).AlignCenter()
                                .Text("- - - - - - - - - - - - - - - - - - - - - - - - -")
                                .FontSize(8).FontColor(ColBorder);
                    }
                });
            });
        });

        doc.GeneratePdf(path);
        System.Diagnostics.Process.Start(
            new System.Diagnostics.ProcessStartInfo(path) { UseShellExecute = true });
    }

    private static void BuildCopy(
        IContainer container,
        ReceiptVoucher v,
        Models.OrganizationSettings org,
        byte[]? logoBytes,
        string words,
        string label)
    {
        container.Border(0.5f).BorderColor(ColBorder).Padding(14).Column(col =>
        {
            // ── Header ──────────────────────────────────────────────────
            col.Item().Row(row =>
            {
                // LEFT: date + voucher number
                row.RelativeItem().Column(c =>
                {
                    c.Item().Text($"التاريخ: {DateTime.Now:d/M/yyyy}")
                        .FontSize(9).FontColor(ColSecondary).AlignLeft();
                    c.Item().PaddingTop(3)
                        .Text(v.VoucherNumber)
                        .FontSize(9).Bold().FontColor(ColDark).AlignLeft();
                });

                row.ConstantItem(16);

                // CENTER: voucher title
                row.ConstantItem(160).Column(c =>
                {
                    c.Item()
                        .Background(ColPrimary).Padding(8)
                        .AlignCenter()
                        .Text("سند دفع نقدي")
                        .FontColor(ColWhite).Bold().FontSize(13);
                    c.Item().PaddingTop(3)
                        .AlignCenter()
                        .Text(label)
                        .FontSize(8).FontColor(ColSecondary);
                });

                row.ConstantItem(16);

                // RIGHT: bank name + logo
                row.RelativeItem().Column(c =>
                {
                    c.Item().Row(r =>
                    {
                        r.RelativeItem().Column(inner =>
                        {
                            inner.Item()
                                .Text(org.BankName)
                                .FontSize(18).Bold().FontColor(ColPrimary).AlignRight();
                            if (!string.IsNullOrWhiteSpace(org.SubTitle))
                                inner.Item().PaddingTop(2)
                                    .Text(org.SubTitle)
                                    .FontSize(9).FontColor(ColSecondary).AlignRight();
                        });
                        if (logoBytes is not null)
                        {
                            r.ConstantItem(6);
                            r.ConstantItem(38).Height(38).Image(logoBytes).FitArea();
                        }
                    });
                });
            });

            col.Item().PaddingVertical(10).LineHorizontal(0.5f).LineColor(ColBorder);

            // ── Details Table ────────────────────────────────────────────
            col.Item().Border(0.5f).BorderColor(ColBorder).Table(table =>
            {
                table.ColumnsDefinition(cols =>
                {
                    cols.RelativeColumn(3);
                    cols.ConstantColumn(1);
                    cols.ConstantColumn(120);
                });

                void Row(string lbl, string val, bool highlight = false)
                {
                    var valueCell = table.Cell()
                        .Border(0.5f).BorderColor(ColBorder)
                        .Padding(10).AlignRight();

                    if (highlight)
                        valueCell.Text(val).Bold().FontSize(14).FontColor(ColPrimary);
                    else
                        valueCell.Text(val);

                    table.Cell().Background(ColBorder);

                    table.Cell()
                        .Border(0.5f).BorderColor(ColBorder)
                        .Background(ColSurface).Padding(10).AlignRight()
                        .Text(lbl).FontSize(9).SemiBold().FontColor(ColSecondary);
                }

                Row("رقم السند", v.VoucherNumber);
                Row("التاريخ", v.Date.ToString("d/M/yyyy"));
                Row("الخزينة", v.Safe?.Name ?? "—");
                Row("صُرف إلى", string.IsNullOrWhiteSpace(v.PayeeName) ? "—" : v.PayeeName);
                Row("المبلغ", $"{v.Amount:N2} ج.م", highlight: true);
                Row("المبلغ كتابةً", words);
                Row("البيان", string.IsNullOrWhiteSpace(v.Notes) ? "—" : v.Notes);
            });

            col.Item().PaddingVertical(10);

            // ── Signatures ───────────────────────────────────────────────
            col.Item().Border(0.5f).BorderColor(ColBorder).Table(table =>
            {
                table.ColumnsDefinition(cols =>
                {
                    cols.RelativeColumn();
                    cols.ConstantColumn(1);
                    cols.RelativeColumn();
                    cols.ConstantColumn(1);
                    cols.RelativeColumn();
                });

                void SigCell(string title)
                {
                    table.Cell()
                        .Background(ColSurface).Padding(12)
                        .AlignCenter().Column(c =>
                        {
                            c.Item().Text(title)
                                .FontSize(9).SemiBold()
                                .FontColor(ColSecondary).AlignCenter();
                            c.Item().PaddingTop(20)
                                .Text("_________________")
                                .FontColor(ColBorder).AlignCenter();
                        });
                }

                SigCell("المدير");
                table.Cell().Background(ColBorder);
                SigCell("المحاسب");
                table.Cell().Background(ColBorder);
                SigCell("أمين الخزينة");
            });

            // ── Branding ─────────────────────────────────────────────────
            col.Item().PaddingTop(6).AlignCenter()
                .Text(t =>
                {
                    t.Span("برنامج حسابات شريان  |  ")
                        .FontSize(7).FontColor(ColBrand);
                    t.Span("م. عبدالرحمن علوان")
                        .FontSize(7).FontColor(ColBrand).Bold();
                    t.Span("  ✦  01127136834")
                        .FontSize(7).FontColor(ColBrand);
                });
        });
    }
}