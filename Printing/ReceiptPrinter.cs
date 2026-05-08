using BloodBankAccounting.Data;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.IO;

namespace BloodBankAccounting.Printing;

public static class ReceiptPrinter
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
            .Include(r => r.Customer)
            .Include(r => r.Safe)
            .FirstAsync(r => r.Id == voucherId);

        // ── تحميل بيانات المؤسسة ──────────────────────────
        var org = await OrgInfoCache.GetAsync();

        byte[]? logoBytes = null;
        if (!string.IsNullOrWhiteSpace(org.LogoPath) && File.Exists(org.LogoPath))
            logoBytes = await File.ReadAllBytesAsync(org.LogoPath);

        var path = Path.Combine(Path.GetTempPath(), $"Receipt_{v.VoucherNumber}.pdf");

        var doc = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A5);
                page.Margin(1.8f, Unit.Centimetre);
                page.DefaultTextStyle(x => x
                    .FontFamily("Arial")
                    .FontSize(11)
                    .DirectionFromRightToLeft());

                page.Content().Column(col =>
                {
                    // ── Header: شعار + اسم البنك ──────────────────────
                    col.Item().Row(row =>
                    {
                        if (logoBytes is not null)
                        {
                            row.ConstantItem(44).Height(44).Image(logoBytes).FitArea();
                            row.ConstantItem(8);
                        }
                        row.RelativeItem().Column(inner =>
                        {
                            inner.Item().AlignCenter()
                                .Text(org.BankName)
                                .FontSize(22).Bold().FontColor(ColPrimary);
                            if (!string.IsNullOrWhiteSpace(org.SubTitle))
                                inner.Item().AlignCenter()
                                    .Text(org.SubTitle)
                                    .FontSize(9).FontColor(ColSecondary);
                        });
                    });

                    col.Item().PaddingVertical(10)
                        .Background(ColDark).Padding(8).AlignCenter()
                        .Text("سند قبض").FontColor(ColWhite).Bold();

                    col.Item().Border(0.5f).BorderColor(ColBorder).Table(table =>
                    {
                        table.ColumnsDefinition(cols =>
                        {
                            cols.RelativeColumn();
                            cols.ConstantColumn(110);
                        });
                        AddRow(table, "رقم السند", v.VoucherNumber);
                        AddRow(table, "التاريخ", v.Date.ToString("d/M/yyyy"));
                        AddRow(table, "الخزينة", v.Safe?.Name ?? "—");
                        AddRow(table, "العميل", v.Customer?.Name ?? "—");
                        AddRow(table, "المبلغ", $"{v.Amount:N2} ج.م", highlight: true);
                        AddRow(table, "البيان", string.IsNullOrWhiteSpace(v.Notes) ? "—" : v.Notes);
                    });

                    // ── Branding footer ────────────────────────────────
                    col.Item().PaddingTop(16).LineHorizontal(0.5f).LineColor("#EEEEEE");
                    col.Item().PaddingTop(6).AlignCenter()
                        .Text(t =>
                        {
                            t.Span("برنامج حسابات شريان  |  ")
                                .FontSize(7.5f).FontColor(ColBrand);
                            t.Span("م. عبدالرحمن علوان")
                                .FontSize(7.5f).FontColor(ColBrand).Bold();
                            t.Span("  ✦  01127136834")
                                .FontSize(7.5f).FontColor(ColBrand);
                        });
                });
            });
        });

        doc.GeneratePdf(path);
        System.Diagnostics.Process.Start(
            new System.Diagnostics.ProcessStartInfo(path) { UseShellExecute = true });
    }

    private static void AddRow(TableDescriptor table, string label, string value, bool highlight = false)
    {
        table.Cell().Border(0.5f).BorderColor(ColBorder).Padding(10).AlignRight()
            .Text(value).FontColor(highlight ? ColPrimary : ColDark);
        table.Cell().Border(0.5f).BorderColor(ColBorder).Background(ColSurface)
            .Padding(10).AlignLeft().Text(label).SemiBold();
    }
}