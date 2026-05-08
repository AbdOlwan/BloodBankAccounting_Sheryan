using BloodBankAccounting.ViewModels;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.IO;

namespace BloodBankAccounting.Printing;

public static class SafeStatementPrinter
{
    private const string ColPrimary = "#C0392B";
    private const string ColDark = "#1D1D1F";
    private const string ColSecondary = "#6E6E73";
    private const string ColSurface = "#F5F5F7";
    private const string ColBorder = "#D2D2D7";
    private const string ColWhite = "#FFFFFF";
    private const string ColGreen = "#1E8449";
    private const string ColRed = "#7B241C";
    private const string ColBlue = "#1A5276";
    private const string ColBrand = "#8B7355";

    public static async Task PrintAsync(
        string safeName,
        DateTime from,
        DateTime to,
        decimal openingBalance,
        decimal closingBalance,
        decimal totalDebit,
        decimal totalCredit,
        List<SafeEntryRow> entries)
    {
        // ── تحميل بيانات المؤسسة ──────────────────────────
        var org = await OrgInfoCache.GetAsync();

        byte[]? logoBytes = null;
        if (!string.IsNullOrWhiteSpace(org.LogoPath) && File.Exists(org.LogoPath))
            logoBytes = await File.ReadAllBytesAsync(org.LogoPath);

        await Task.Run(() =>
        {
            var fileName = $"كشف_حساب_{safeName}_{from:yyyyMMdd}_{to:yyyyMMdd}.pdf";
            var path = Path.Combine(Path.GetTempPath(), fileName);

            var doc = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.MarginTop(1.5f, Unit.Centimetre);
                    page.MarginBottom(1.5f, Unit.Centimetre);
                    page.MarginHorizontal(1.8f, Unit.Centimetre);

                    page.DefaultTextStyle(x => x
                        .FontFamily("Arial")
                        .FontSize(10)
                        .FontColor(ColDark)
                        .DirectionFromRightToLeft());

                    page.Content().Column(col =>
                    {
                        col.Item().Element(BuildHeader(safeName, from, to, org, logoBytes));
                        col.Item().PaddingVertical(12).LineHorizontal(0.5f).LineColor(ColBorder);
                        col.Item().Element(BuildSummaryRow(openingBalance, totalDebit, totalCredit, closingBalance));
                        col.Item().PaddingVertical(14);
                        col.Item().Element(BuildTable(entries));
                        col.Item().PaddingTop(16).Element(BuildFooter());

                        // ── Branding ──────────────────────────────────
                        col.Item().PaddingTop(8).LineHorizontal(0.5f).LineColor("#EEEEEE");
                        col.Item().PaddingTop(5).AlignCenter()
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

                    page.Footer().AlignCenter().Text(t =>
                    {
                        t.Span("صفحة ").FontSize(8).FontColor(ColSecondary);
                        t.CurrentPageNumber().FontSize(8).FontColor(ColSecondary);
                        t.Span(" من ").FontSize(8).FontColor(ColSecondary);
                        t.TotalPages().FontSize(8).FontColor(ColSecondary);
                        t.Span($"   |   طُبع في: {DateTime.Now:d/M/yyyy  HH:mm}")
                            .FontSize(8).FontColor(ColBorder);
                    });
                });
            });

            doc.GeneratePdf(path);
            System.Diagnostics.Process.Start(
                new System.Diagnostics.ProcessStartInfo(path) { UseShellExecute = true });
        });
    }

    // ══════════════════════════════════════════════════════════
    //  رأس الصفحة
    // ══════════════════════════════════════════════════════════
    private static Action<IContainer> BuildHeader(
        string safeName, DateTime from, DateTime to,
        Models.OrganizationSettings org, byte[]? logoBytes)
        => c => c.Row(row =>
        {
            row.ConstantItem(230)
                .Border(0.5f).BorderColor(ColBorder)
                .Background(ColSurface).Padding(12)
                .Column(inner =>
                {
                    inner.Item().Row(r =>
                    {
                        r.RelativeItem().AlignRight().Text(safeName).Bold();
                        r.RelativeItem().AlignLeft().Text("الخزينة").FontSize(9).FontColor(ColSecondary);
                    });
                    inner.Item().PaddingTop(4).Row(r =>
                    {
                        r.RelativeItem().AlignRight().Text(from.ToString("d/M/yyyy"));
                        r.RelativeItem().AlignLeft().Text("من").FontSize(9).FontColor(ColSecondary);
                    });
                    inner.Item().PaddingTop(4).Row(r =>
                    {
                        r.RelativeItem().AlignRight().Text(to.ToString("d/M/yyyy"));
                        r.RelativeItem().AlignLeft().Text("إلى").FontSize(9).FontColor(ColSecondary);
                    });
                });

            row.RelativeItem().Column(inner =>
            {
                inner.Item().Row(r =>
                {
                    r.RelativeItem().Column(bc =>
                    {
                        bc.Item()
                            .Text(org.BankName)
                            .FontSize(22).Bold().FontColor(ColPrimary);
                        if (!string.IsNullOrWhiteSpace(org.SubTitle))
                            bc.Item().PaddingTop(2)
                                .Text("كشف حساب خزينة — " + org.SubTitle)
                                .FontSize(10).FontColor(ColSecondary);
                        else
                            bc.Item().PaddingTop(2)
                                .Text("كشف حساب خزينة")
                                .FontSize(11).FontColor(ColSecondary);
                        if (!string.IsNullOrWhiteSpace(org.Phone))
                            bc.Item().Text($"📞  {org.Phone}")
                                .FontSize(9).FontColor(ColSecondary);
                    });
                    if (logoBytes is not null)
                    {
                        r.ConstantItem(8);
                        r.ConstantItem(52).Height(52).Image(logoBytes).FitArea();
                    }
                });
            });
        });

    // ══════════════════════════════════════════════════════════
    //  ملخص الأرصدة
    // ══════════════════════════════════════════════════════════
    private static Action<IContainer> BuildSummaryRow(
        decimal openingBalance, decimal totalDebit, decimal totalCredit, decimal closingBalance)
        => c => c.Border(0.5f).BorderColor(ColBorder).Table(table =>
        {
            table.ColumnsDefinition(cols =>
            {
                cols.RelativeColumn();
                cols.ConstantColumn(1);
                cols.RelativeColumn();
                cols.ConstantColumn(1);
                cols.RelativeColumn();
                cols.ConstantColumn(1);
                cols.RelativeColumn();
            });

            void Card(string label, decimal value, string color)
            {
                table.Cell()
                    .Background(ColSurface).Padding(12)
                    .AlignCenter().Column(cc =>
                    {
                        cc.Item().Text(label)
                            .FontSize(9).FontColor(ColSecondary).AlignCenter();
                        cc.Item().PaddingTop(5)
                            .Text($"{value:N2} ج.م")
                            .FontSize(13).Bold().FontColor(color).AlignCenter();
                    });
            }

            void Divider() => table.Cell().Background(ColBorder);

            Card("الرصيد الختامي", closingBalance, closingBalance >= 0 ? ColGreen : ColPrimary);
            Divider();
            Card("إجمالي الصادر", totalCredit, ColRed);
            Divider();
            Card("إجمالي الوارد", totalDebit, ColGreen);
            Divider();
            Card("الرصيد الافتتاحي", openingBalance, ColBlue);
        });

    // ══════════════════════════════════════════════════════════
    //  جدول الحركات
    // ══════════════════════════════════════════════════════════
    private static Action<IContainer> BuildTable(List<SafeEntryRow> entries)
        => c => c.Border(0.5f).BorderColor(ColBorder).Table(table =>
        {
            table.ColumnsDefinition(cols =>
            {
                cols.ConstantColumn(105);
                cols.ConstantColumn(95);
                cols.ConstantColumn(95);
                cols.ConstantColumn(62);
                cols.RelativeColumn(3);
                cols.ConstantColumn(78);
            });

            void TH(string text)
            {
                table.Cell()
                    .Border(0.5f).BorderColor(ColBorder)
                    .Background(ColDark).Padding(9)
                    .AlignCenter()
                    .Text(t => t.Span(text).FontColor(ColWhite).Bold().FontSize(10));
            }

            TH("الرصيد");
            TH("صادر  ▼");
            TH("وارد  ▲");
            TH("النوع");
            TH("البيان / الوصف");
            TH("التاريخ");

            int i = 0;
            foreach (var e in entries)
            {
                var bg = i % 2 == 0 ? ColWhite : ColSurface;

                void TD(string txt, string? color = null, bool bold = false, bool center = false)
                {
                    var cell = table.Cell()
                        .Border(0.5f).BorderColor(ColBorder)
                        .Background(bg).Padding(7);

                    IContainer aligned = center ? cell.AlignCenter() : (IContainer)cell;
                    aligned.Text(t =>
                    {
                        var span = t.Span(txt).FontSize(9.5f);
                        if (color != null) span = span.FontColor(color);
                        if (bold) span = span.Bold();
                    });
                }

                var balColor = e.Balance >= 0 ? ColGreen : ColPrimary;
                TD($"{e.Balance:N2}", color: balColor, bold: true);

                if (e.Credit > 0)
                    TD($"{e.Credit:N2}", color: ColRed, bold: true);
                else
                    TD("—", color: ColBorder, center: true);

                if (e.Debit > 0)
                    TD($"{e.Debit:N2}", color: ColGreen, bold: true);
                else
                    TD("—", color: ColBorder, center: true);

                TD(e.RefTypeDisplay, center: true, color: ColSecondary);
                TD(e.Description);
                TD(e.Date.ToString("d/M/yyyy"), center: true, color: ColSecondary);

                i++;
            }

            // صف الإجمالي
            void TF(string txt, string bg2 = "#1D1D1F", string color = "#FFFFFF")
            {
                table.Cell()
                    .Border(0.5f).BorderColor(ColBorder)
                    .Background(bg2).Padding(9)
                    .Text(t => t.Span(txt).FontColor(color).FontSize(10).Bold());
            }

            TF(entries.Count > 0 ? $"{entries.Last().Balance:N2} ج.م" : "0.00 ج.م",
               entries.Count > 0 && entries.Last().Balance >= 0 ? ColBlue : ColRed,
               ColWhite);
            TF($"{entries.Sum(e => e.Credit):N2} ج.م", ColRed, ColWhite);
            TF($"{entries.Sum(e => e.Debit):N2} ج.م", ColGreen, ColWhite);
            TF("", ColDark, ColWhite);
            TF("الإجمالي", ColDark, ColWhite);
            TF("", ColDark, ColWhite);
        });

    // ══════════════════════════════════════════════════════════
    //  ذيل الصفحة — التوقيعات
    // ══════════════════════════════════════════════════════════
    private static Action<IContainer> BuildFooter()
        => c => c.Column(col =>
        {
            col.Item().LineHorizontal(0.5f).LineColor(ColBorder);
            col.Item().PaddingTop(14)
                .Border(0.5f).BorderColor(ColBorder)
                .Table(table =>
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
                            .Border(0.5f).BorderColor(ColBorder)
                            .Background(ColSurface).Padding(14)
                            .AlignCenter().Column(c2 =>
                            {
                                c2.Item().Text(title)
                                    .FontSize(10).SemiBold()
                                    .FontColor(ColSecondary).AlignCenter();
                                c2.Item().PaddingTop(22)
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
        });
}

// ── Extension helpers ──────────────────────────────────────
internal static class QuestExtensions
{
    internal static IContainer When(
        this IContainer c,
        bool condition,
        Func<IContainer, IContainer> apply)
        => condition ? apply(c) : c;

    internal static TextSpanDescriptor When(
        this TextSpanDescriptor t,
        bool condition,
        Func<TextSpanDescriptor, TextSpanDescriptor> apply)
        => condition ? apply(t) : t;
}