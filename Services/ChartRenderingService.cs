using MauiAppIT13.Models;
using SkiaSharp;

namespace MauiAppIT13.Services;

public sealed class ChartRenderingService
{
    private static readonly SKColor[] ChartColors = new[]
    {
        SKColor.Parse("#0078D4"), // Blue
        SKColor.Parse("#00B294"), // Teal
        SKColor.Parse("#8764B8"), // Purple
        SKColor.Parse("#E74856"), // Red
        SKColor.Parse("#F7630C"), // Orange
        SKColor.Parse("#FFB900"), // Yellow
        SKColor.Parse("#00CC6A"), // Green
        SKColor.Parse("#CA5010")  // Dark Orange
    };

    public Task<MemoryStream> RenderChartToStreamAsync(ChartData chartData, int width, int height)
    {
        var stream = new MemoryStream();

        try
        {
            var chartType = chartData.ChartType.ToLowerInvariant();

            if (chartType == "pie" || chartType == "doughnut")
            {
                RenderPieChart(stream, chartData, width, height);
            }
            else
            {
                RenderBarChart(stream, chartData, width, height, chartType);
            }

            stream.Position = 0;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Chart rendering failed: {ex.Message}");
            stream = new MemoryStream();
        }

        return Task.FromResult(stream);
    }

    private void RenderPieChart(MemoryStream stream, ChartData chartData, int width, int height)
    {
        using var surface = SKSurface.Create(new SKImageInfo(width, height));
        var canvas = surface.Canvas;
        canvas.Clear(SKColors.White);

        var total = chartData.DataPoints.Sum(p => p.Value);
        var centerX = width / 2f;
        var centerY = height / 2f;
        var radius = Math.Min(width, height) / 2.5f;

        var startAngle = -90f;
        for (int i = 0; i < chartData.DataPoints.Count; i++)
        {
            var point = chartData.DataPoints[i];
            var sweepAngle = (float)(point.Value / total * 360);
            var color = ChartColors[i % ChartColors.Length];

            using var paint = new SKPaint
            {
                Color = color,
                IsAntialias = true,
                Style = SKPaintStyle.Fill
            };

            var rect = new SKRect(centerX - radius, centerY - radius, centerX + radius, centerY + radius);
            using var path = new SKPath();
            path.MoveTo(centerX, centerY);
            path.ArcTo(rect, startAngle, sweepAngle, false);
            path.Close();
            canvas.DrawPath(path, paint);

            startAngle += sweepAngle;
        }

        // Draw legend
        var legendY = height - 80f;
        var legendX = 20f;
        using var textPaint = new SKPaint
        {
            Color = SKColors.Black,
            IsAntialias = true,
            TextSize = 14
        };

        for (int i = 0; i < chartData.DataPoints.Count; i++)
        {
            var point = chartData.DataPoints[i];
            var color = ChartColors[i % ChartColors.Length];

            using var legendPaint = new SKPaint { Color = color, Style = SKPaintStyle.Fill };
            canvas.DrawRect(legendX, legendY + (i * 20), 12, 12, legendPaint);
            canvas.DrawText($"{point.Label}: {point.Value:N0}", legendX + 20, legendY + (i * 20) + 10, textPaint);
        }

        using var image = surface.Snapshot();
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        data.SaveTo(stream);
    }

    private void RenderBarChart(MemoryStream stream, ChartData chartData, int width, int height, string chartType)
    {
        using var surface = SKSurface.Create(new SKImageInfo(width, height));
        var canvas = surface.Canvas;
        canvas.Clear(SKColors.White);

        var margin = 60f;
        var bottomMargin = 80f; // Increased for rotated labels
        var chartWidth = width - (margin * 2);
        var chartHeight = height - margin - bottomMargin;
        var maxValue = chartData.DataPoints.Max(p => p.Value);
        var barWidth = chartWidth / chartData.DataPoints.Count * 0.7f;
        var spacing = chartWidth / chartData.DataPoints.Count;

        // Draw axes
        using var axisPaint = new SKPaint
        {
            Color = SKColors.Black,
            StrokeWidth = 2,
            IsAntialias = true
        };
        canvas.DrawLine(margin, height - bottomMargin, width - margin, height - bottomMargin, axisPaint);
        canvas.DrawLine(margin, margin, margin, height - bottomMargin, axisPaint);

        // Draw bars
        for (int i = 0; i < chartData.DataPoints.Count; i++)
        {
            var point = chartData.DataPoints[i];
            var barHeight = (float)(point.Value / maxValue * chartHeight);
            var x = margin + (i * spacing) + (spacing - barWidth) / 2;
            var y = height - bottomMargin - barHeight;
            var color = ChartColors[i % ChartColors.Length];

            using var barPaint = new SKPaint
            {
                Color = color,
                Style = SKPaintStyle.Fill,
                IsAntialias = true
            };
            canvas.DrawRect(x, y, barWidth, barHeight, barPaint);

            // Draw value labels above bars
            using var valuePaint = new SKPaint
            {
                Color = SKColors.Black,
                TextSize = 12,
                IsAntialias = true,
                TextAlign = SKTextAlign.Center
            };
            canvas.DrawText(point.Value.ToString("N0"), x + barWidth / 2, y - 5, valuePaint);

            // Draw x-axis labels rotated at 45 degrees
            using var labelPaint = new SKPaint
            {
                Color = SKColors.Black,
                TextSize = 10,
                IsAntialias = true,
                TextAlign = SKTextAlign.Right
            };
            
            canvas.Save();
            canvas.Translate(x + barWidth / 2, height - bottomMargin + 10);
            canvas.RotateDegrees(-45);
            canvas.DrawText(point.Label, 0, 0, labelPaint);
            canvas.Restore();
        }

        // Draw title
        using var titlePaint = new SKPaint
        {
            Color = SKColors.Black,
            TextSize = 18,
            IsAntialias = true,
            TextAlign = SKTextAlign.Center,
            FakeBoldText = true
        };
        canvas.DrawText(chartData.Title, width / 2, 30, titlePaint);

        using var image = surface.Snapshot();
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        data.SaveTo(stream);
    }
}
