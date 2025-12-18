namespace MauiAppIT13.Models;

public enum ReportCategory
{
    TicketSummary,
    StudentActivity,
    AdviserPerformance,
    CommunicationAnalytics
}

public sealed class ChartDataPoint
{
    public required string Label { get; init; }
    public required double Value { get; init; }
    public string? Category { get; init; }
}

public sealed class ChartData
{
    public required string Title { get; init; }
    public required string ChartType { get; init; }
    public required IReadOnlyList<ChartDataPoint> DataPoints { get; init; }
    public string? XAxisLabel { get; init; }
    public string? YAxisLabel { get; init; }
}

public sealed class ReportExportData
{
    public required string ReportTitle { get; init; }
    public required ReportCategory Category { get; init; }
    public required DateTime PeriodStartUtc { get; init; }
    public required DateTime PeriodEndUtc { get; init; }
    public required AdminReportMetrics Metrics { get; init; }
    public required IReadOnlyList<string> Headers { get; init; }
    public required IReadOnlyList<IReadOnlyList<string>> Rows { get; init; }
    public IReadOnlyList<ChartData>? Charts { get; init; }
}
