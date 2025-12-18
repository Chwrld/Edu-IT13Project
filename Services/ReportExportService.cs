using System.Text;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
using MauiAppIT13.Models;
using Syncfusion.XlsIO;
using SyncColor = Syncfusion.Drawing.Color;
using QuestColors = QuestPDF.Helpers.Colors;

namespace MauiAppIT13.Services;

public sealed class ReportExportService
{
    private readonly ChartRenderingService _chartRenderingService;

    public ReportExportService()
    {
        _chartRenderingService = new ChartRenderingService();
    }
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

    public async Task<string> ExportPdfAsync(ReportExportData data, string directory)
    {
        Directory.CreateDirectory(directory);
        var fileName = $"{SanitizeFileName(data.ReportTitle)}_{DateTime.UtcNow:yyyyMMddHHmmss}.pdf";
        var fullPath = Path.Combine(directory, fileName);

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(QuestPDF.Helpers.PageSizes.A4);
                page.Margin(40);
                page.DefaultTextStyle(x => x.FontSize(11));

                page.Header().Column(column =>
                {
                    column.Item().Text(data.ReportTitle).FontSize(20).Bold();
                    column.Item().PaddingTop(5).Text($"Period: {data.PeriodStartUtc:MMM d, yyyy} - {data.PeriodEndUtc:MMM d, yyyy}").FontSize(11);
                    column.Item().Text($"Generated: {DateTime.UtcNow:MMM d, yyyy h:mm tt} UTC").FontSize(9).FontColor(QuestColors.Grey.Medium);
                });

                page.Content().PaddingTop(20).Column(column =>
                {
                    // Charts section
                    if (data.Charts != null && data.Charts.Any())
                    {
                        column.Item().Text("Visual Analytics").FontSize(14).Bold();
                        column.Item().PaddingTop(10);

                        foreach (var chartData in data.Charts)
                        {
                            column.Item().PaddingTop(10).Column(chartColumn =>
                            {
                                try
                                {
                                    var chartStream = _chartRenderingService.RenderChartToStreamAsync(chartData, 500, 250).Result;
                                    if (chartStream.Length > 0)
                                    {
                                        chartColumn.Item().Image(chartStream);
                                        
                                        var description = GetChartDescription(chartData, data.Category);
                                        if (!string.IsNullOrEmpty(description))
                                        {
                                            chartColumn.Item().PaddingTop(5).Text(description).FontSize(9).FontColor(QuestColors.Grey.Darken2);
                                        }
                                    }
                                }
                                catch (Exception ex)
                                {
                                    chartColumn.Item().Text($"[Chart rendering failed: {ex.Message}]").FontSize(9).FontColor(QuestColors.Red.Medium);
                                }
                            });
                        }

                        column.Item().PaddingTop(20);
                    }

                    // Data table section
                    column.Item().Text("Detailed Data").FontSize(14).Bold();
                    column.Item().PaddingTop(10);

                    column.Item().Table(table =>
                    {
                        // Define columns
                        table.ColumnsDefinition(columns =>
                        {
                            foreach (var _ in data.Headers)
                            {
                                columns.RelativeColumn();
                            }
                        });

                        // Header row
                        table.Header(header =>
                        {
                            foreach (var headerText in data.Headers)
                            {
                                header.Cell().Background(QuestColors.Blue.Darken2).Padding(5).Text(headerText).FontColor(QuestColors.White).Bold();
                            }
                        });

                        // Data rows
                        foreach (var row in data.Rows)
                        {
                            foreach (var cell in row)
                            {
                                table.Cell().Border(1).BorderColor(QuestColors.Grey.Lighten2).Padding(5).Text(cell);
                            }
                        }

                        // Total row
                        if (data.Category != ReportCategory.TicketSummary)
                        {
                            var totalRow = CalculateTotalRow(data);
                            if (totalRow != null && totalRow.Any())
                            {
                                foreach (var cell in totalRow)
                                {
                                    table.Cell().Background(QuestColors.Grey.Lighten3).Border(1).BorderColor(QuestColors.Grey.Lighten2).Padding(5).Text(cell).Bold();
                                }
                            }
                        }
                    });
                });

                page.Footer().AlignCenter().Text(x =>
                {
                    x.Span("Page ");
                    x.CurrentPageNumber();
                    x.Span(" of ");
                    x.TotalPages();
                });
            });
        });

        document.GeneratePdf(fullPath);
        return fullPath;
    }

    public async Task<string> ExportExcelAsync(ReportExportData data, string directory)
    {
        Directory.CreateDirectory(directory);
        var fileName = $"{SanitizeFileName(data.ReportTitle)}_{DateTime.UtcNow:yyyyMMddHHmmss}.xlsx";
        var fullPath = Path.Combine(directory, fileName);

        using var excelEngine = new ExcelEngine();
        var application = excelEngine.Excel;
        application.DefaultVersion = ExcelVersion.Xlsx;

        var workbook = application.Workbooks.Create(1);
        var worksheet = workbook.Worksheets[0];
        worksheet.Name = SanitizeSheetName(data.ReportTitle);

        var currentRow = 1;

        worksheet.Range[$"A{currentRow}"].Text = data.ReportTitle;
        worksheet.Range[$"A{currentRow}"].CellStyle.Font.Bold = true;
        worksheet.Range[$"A{currentRow}"].CellStyle.Font.Size = 18;
        worksheet.Range[$"A{currentRow}"].CellStyle.Font.Color = ExcelKnownColors.Black;
        currentRow += 2;

        worksheet.Range[$"A{currentRow}"].Text = $"Period: {data.PeriodStartUtc:MMM d, yyyy} - {data.PeriodEndUtc:MMM d, yyyy}";
        worksheet.Range[$"A{currentRow}"].CellStyle.Font.Size = 11;
        currentRow++;

        worksheet.Range[$"A{currentRow}"].Text = $"Generated: {DateTime.UtcNow:MMM d, yyyy h:mm tt} UTC";
        worksheet.Range[$"A{currentRow}"].CellStyle.Font.Size = 9;
        worksheet.Range[$"A{currentRow}"].CellStyle.Font.Color = ExcelKnownColors.Grey_50_percent;
        currentRow += 2;

        if (data.Charts != null && data.Charts.Any())
        {
            worksheet.Range[$"A{currentRow}"].Text = "Visual Analytics";
            worksheet.Range[$"A{currentRow}"].CellStyle.Font.Bold = true;
            worksheet.Range[$"A{currentRow}"].CellStyle.Font.Size = 14;
            currentRow += 2;

            foreach (var chartData in data.Charts)
            {
                try
                {
                    using var chartStream = await _chartRenderingService.RenderChartToStreamAsync(chartData, 600, 300);
                    if (chartStream.Length > 0)
                    {
                        var picture = worksheet.Pictures.AddPicture(currentRow, 1, chartStream);
                        picture.Height = 300;
                        picture.Width = 600;
                        currentRow += 18;
                    }
                }
                catch
                {
                    worksheet.Range[$"A{currentRow}"].Text = $"[Chart: {chartData.Title}]";
                    currentRow += 2;
                }
            }

            currentRow += 2;
        }

        worksheet.Range[$"A{currentRow}"].Text = "Detailed Data";
        worksheet.Range[$"A{currentRow}"].CellStyle.Font.Bold = true;
        worksheet.Range[$"A{currentRow}"].CellStyle.Font.Size = 14;
        currentRow += 2;

        for (int i = 0; i < data.Headers.Count; i++)
        {
            var cell = worksheet.Range[currentRow, i + 1];
            cell.Text = data.Headers[i];
            cell.CellStyle.Font.Bold = true;
            cell.CellStyle.Font.Color = ExcelKnownColors.White;
            cell.CellStyle.Color = SyncColor.FromArgb(0, 91, 165);
            cell.CellStyle.HorizontalAlignment = ExcelHAlign.HAlignCenter;
        }
        currentRow++;

        foreach (var row in data.Rows)
        {
            for (int i = 0; i < row.Count && i < data.Headers.Count; i++)
            {
                worksheet.Range[currentRow, i + 1].Text = row[i];
            }
            currentRow++;
        }

        for (int i = 1; i <= data.Headers.Count; i++)
        {
            worksheet.AutofitColumn(i);
        }

        using var stream = new FileStream(fullPath, FileMode.Create, FileAccess.Write);
        workbook.SaveAs(stream);

        return fullPath;
    }

    private static string SanitizeSheetName(string name)
    {
        var invalidChars = new[] { '\\', '/', '?', '*', '[', ']', ':' };
        var sanitized = new string(name.Select(ch => invalidChars.Contains(ch) ? '_' : ch).ToArray());
        return sanitized.Length > 31 ? sanitized.Substring(0, 31) : sanitized;
    }


    private static string EscapeCsv(string value)
    {
        if (value.Contains(',') || value.Contains('"'))
        {
            return $"\"{value.Replace("\"", "\"\"")}\"";
        }

        return value;
    }

    private static string GetChartDescription(ChartData chartData, ReportCategory category)
    {
        if (chartData.DataPoints == null || !chartData.DataPoints.Any())
            return string.Empty;

        var total = chartData.DataPoints.Sum(p => p.Value);
        var max = chartData.DataPoints.Max(p => p.Value);
        var maxLabel = chartData.DataPoints.FirstOrDefault(p => p.Value == max)?.Label ?? "N/A";
        var min = chartData.DataPoints.Min(p => p.Value);
        var minLabel = chartData.DataPoints.FirstOrDefault(p => p.Value == min)?.Label ?? "N/A";
        var average = chartData.DataPoints.Average(p => p.Value);
        var dataPointCount = chartData.DataPoints.Count;

        return (category, chartData.ChartType.ToLowerInvariant()) switch
        {
            (ReportCategory.TicketSummary, "pie") => 
                $"This chart shows the distribution of {total:N0} total tickets across different statuses. The largest segment is '{maxLabel}' with {max:N0} tickets ({(max/total*100):F1}%), while '{minLabel}' has the fewest with {min:N0} tickets.",
            
            (ReportCategory.TicketSummary, "column") => 
                $"This comparison shows ticket metrics across {dataPointCount} categories. The highest value is {max:N0} in '{maxLabel}', with an average of {average:F1} across all metrics.",
            
            (ReportCategory.StudentActivity, "line") => 
                $"Over the {dataPointCount}-day period, there were {total:N0} total student enrollments. The peak enrollment day was {maxLabel} with {max:N0} enrollments, while the lowest was {minLabel} with {min:N0} enrollments. Daily average: {average:F1} enrollments.",
            
            (ReportCategory.AdviserPerformance, "column") => 
                $"Across {dataPointCount/2} advisers, there were {total:N0} total tickets. The most active adviser handled {max:N0} tickets, while the least active handled {min:N0} tickets. Average per adviser: {average:F1} tickets.",
            
            (ReportCategory.CommunicationAnalytics, "area") => 
                $"During this {dataPointCount}-day period, there were {total:N0} total messages sent. The busiest day was {maxLabel} with {max:N0} messages, and the quietest was {minLabel} with {min:N0} messages. Daily average: {average:F1} messages.",
            
            _ => string.Empty
        };
    }

    private static List<string>? CalculateTotalRow(ReportExportData data)
    {
        if (data.Rows == null || !data.Rows.Any() || data.Headers == null)
            return null;

        var totalRow = new List<string>();
        
        for (int colIndex = 0; colIndex < data.Headers.Count; colIndex++)
        {
            if (colIndex == 0)
            {
                totalRow.Add("TOTAL");
                continue;
            }

            // Try to sum numeric columns
            var sum = 0.0;
            var hasNumericValues = false;

            foreach (var row in data.Rows)
            {
                if (colIndex < row.Count)
                {
                    var cellValue = row[colIndex];
                    // Remove common formatting characters
                    var cleanValue = cellValue.Replace(",", "").Replace("%", "").Replace("$", "").Trim();
                    
                    if (double.TryParse(cleanValue, out var numValue))
                    {
                        sum += numValue;
                        hasNumericValues = true;
                    }
                }
            }

            if (hasNumericValues)
            {
                // Format the total based on the original column format
                var firstValue = data.Rows.First()[colIndex];
                if (firstValue.Contains("%"))
                    totalRow.Add($"{sum:F1}%");
                else if (firstValue.Contains(","))
                    totalRow.Add($"{sum:N0}");
                else
                    totalRow.Add($"{sum:F0}");
            }
            else
            {
                totalRow.Add("-");
            }
        }

        return totalRow;
    }

    private static string SanitizeFileName(string name)
    {
        var invalidChars = Path.GetInvalidFileNameChars();
        var sanitized = new string(name.Select(ch => invalidChars.Contains(ch) ? '_' : ch).ToArray());
        return string.IsNullOrWhiteSpace(sanitized) ? "report" : sanitized;
    }
}
