namespace MauiAppIT13.Models;

public enum ReportCategory
{
    TicketSummary,
    StudentActivity,
    AdviserPerformance,
    CommunicationAnalytics
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
}
