using System.Text;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
using MauiAppIT13.Models;
using Colors = QuestPDF.Helpers.Colors;
using QuestContainer = QuestPDF.Infrastructure.IContainer;

namespace MauiAppIT13.Services;

public sealed class ReportExportService
{
    public async Task<string> ExportCsvAsync(ReportExportData data, string directory)
    {
        Directory.CreateDirectory(directory);
        var fileName = $"{SanitizeFileName(data.ReportTitle)}_{DateTime.UtcNow:yyyyMMddHHmmss}.csv";
        var fullPath = Path.Combine(directory, fileName);

        var sb = new StringBuilder();
        sb.AppendLine(string.Join(",", data.Headers.Select(EscapeCsv)));
        foreach (var row in data.Rows)
        {
            sb.AppendLine(string.Join(",", row.Select(EscapeCsv)));
        }

        await File.WriteAllTextAsync(fullPath, sb.ToString());
        return fullPath;
    }

    public Task<string> ExportPdfAsync(ReportExportData data, string directory)
    {
        Directory.CreateDirectory(directory);
        var fileName = $"{SanitizeFileName(data.ReportTitle)}_{DateTime.UtcNow:yyyyMMddHHmmss}.pdf";
        var fullPath = Path.Combine(directory, fileName);

        Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Margin(30);
                page.Size(QuestPDF.Helpers.PageSizes.A4);
                page.DefaultTextStyle(x => x.FontSize(12).FontFamily("Helvetica"));

                page.Content().Column(col =>
                {
                    col.Spacing(10);
                    col.Item().Text(data.ReportTitle).FontSize(20).SemiBold();
                    col.Item().Text($"Period: {data.PeriodStartUtc:MMM d, yyyy} - {data.PeriodEndUtc:MMM d, yyyy}");
                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            foreach (var _ in data.Headers)
                            {
                                columns.RelativeColumn();
                            }
                        });

                        table.Header(header =>
                        {
                            foreach (var headerText in data.Headers)
                            {
                                header.Cell().Element(c => ApplyCellStyle(c)).Text(headerText).SemiBold();
                            }
                        });

                        foreach (var row in data.Rows)
                        {
                            foreach (var cell in row)
                            {
                                table.Cell().Element(c => ApplyCellStyle(c)).Text(cell);
                            }
                        }

                    });
                });
            });
        }).GeneratePdf(fullPath);

        return Task.FromResult(fullPath);
    }

    private static QuestContainer ApplyCellStyle(QuestContainer container) =>
        container.Padding(5)
            .BorderBottom(1)
            .BorderColor(Colors.Grey.Lighten2);

    private static string EscapeCsv(string value)
    {
        if (value.Contains(',') || value.Contains('"'))
        {
            return $"\"{value.Replace("\"", "\"\"")}\"";
        }

        return value;
    }

    private static string SanitizeFileName(string name)
    {
        var invalidChars = Path.GetInvalidFileNameChars();
        var sanitized = new string(name.Select(ch => invalidChars.Contains(ch) ? '_' : ch).ToArray());
        return string.IsNullOrWhiteSpace(sanitized) ? "report" : sanitized;
    }
}
