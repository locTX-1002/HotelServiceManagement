using System.Globalization;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Services;

public static class ReportPdfBuilder
{
    static ReportPdfBuilder()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public static byte[] Build(RevenueReport report)
    {
        var doc = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(30);
                page.DefaultTextStyle(x => x.FontSize(10));

                page.Header().Column(col =>
                {
                    col.Item().Text("BÁO CÁO DOANH THU").Bold().FontSize(16);
                    col.Item().Text($"Từ {report.FromDate:dd/MM/yyyy} đến {report.ToDate:dd/MM/yyyy}")
                        .FontSize(11).FontColor(Colors.Grey.Darken1);
                    col.Item().PaddingTop(8).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);
                });

                page.Content().PaddingTop(10).Column(col =>
                {
                    col.Item().Row(row =>
                    {
                        Stat(row, "Tiền phòng", report.RoomRevenue);
                        Stat(row, "Dịch vụ", report.ServiceRevenue);
                        Stat(row, "Phụ thu", report.SurchargeRevenue);
                        Stat(row, "Giảm giá", report.DiscountAmount);
                        Stat(row, "Tổng hoá đơn", report.InvoiceRevenue);
                        Stat(row, "Thực thu", report.CollectedAmount);
                    });

                    col.Item().PaddingTop(12).Table(table =>
                    {
                        table.ColumnsDefinition(c =>
                        {
                            c.RelativeColumn(1.2f);
                            c.RelativeColumn();
                            c.RelativeColumn();
                            c.RelativeColumn();
                            c.RelativeColumn();
                            c.RelativeColumn();
                            c.RelativeColumn();
                        });

                        table.Header(h =>
                        {
                            H(h.Cell(), "Ngày");
                            H(h.Cell(), "Tiền phòng");
                            H(h.Cell(), "Dịch vụ");
                            H(h.Cell(), "Phụ thu");
                            H(h.Cell(), "Giảm giá");
                            H(h.Cell(), "Tổng HĐ");
                            H(h.Cell(), "Thực thu");
                        });

                        foreach (var d in report.ByDay)
                        {
                            C(table, d.Date.ToString("dd/MM/yyyy"));
                            M(table, d.RoomRevenue);
                            M(table, d.ServiceRevenue);
                            M(table, d.SurchargeRevenue);
                            M(table, d.DiscountAmount);
                            M(table, d.InvoiceRevenue);
                            M(table, d.CollectedAmount);
                        }
                    });
                });

                page.Footer().AlignCenter()
                    .Text(t =>
                    {
                        t.Span("FU Hotel · Xuất lúc ");
                        t.Span(DateTime.Now.ToString("HH:mm dd/MM/yyyy"));
                    });
            });
        });

        return doc.GeneratePdf();
    }

    private static void Stat(RowDescriptor row, string label, decimal value)
    {
        row.RelativeItem().Border(0.5f).BorderColor(Colors.Grey.Lighten2)
            .Padding(6).Column(c =>
            {
                c.Item().Text(label).FontSize(8).FontColor(Colors.Grey.Darken1);
                c.Item().Text(F(value)).Bold().FontSize(11);
            });
    }

    private static void H(IContainer h, string text) =>
        h.Background(Colors.Grey.Lighten3).Padding(5).Text(text).Bold().FontSize(9);

    private static void C(TableDescriptor t, string text) =>
        t.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(text).FontSize(9);

    private static void M(TableDescriptor t, decimal v) =>
        t.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(5)
            .AlignRight().Text(F(v)).FontSize(9);

    private static string F(decimal v) => v.ToString("N0", CultureInfo.GetCultureInfo("vi-VN")) + " đ";
}
