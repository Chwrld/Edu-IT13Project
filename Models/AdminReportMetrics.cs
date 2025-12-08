namespace MauiAppIT13.Models;

public sealed class AdminReportMetrics
{
    public DateTime PeriodStartUtc { get; init; }
    public DateTime PeriodEndUtc { get; init; }
    public DateTime ReportGeneratedUtc { get; init; }

    public int TotalTicketsCurrent { get; init; }
    public int TotalTicketsPrevious { get; init; }

    public double AvgResponseMinutesCurrent { get; init; }
    public double AvgResponseMinutesPrevious { get; init; }

    public int ResolvedTicketsCurrent { get; init; }
    public int ResolvedTicketsPrevious { get; init; }

    public int ActiveUsersTotal { get; init; }
    public int ActiveUsersCreatedCurrent { get; init; }
    public int ActiveUsersCreatedPrevious { get; init; }

    public int MessagesCurrent { get; init; }
    public int MessagesPrevious { get; init; }

    public int StudentEngagementCurrent { get; init; }
    public int StudentEngagementPrevious { get; init; }

    public double TotalTicketsChangePercent => ComputePercentChange(TotalTicketsCurrent, TotalTicketsPrevious);
    public double AvgResponseChangePercent => ComputePercentChange(AvgResponseMinutesCurrent, AvgResponseMinutesPrevious, invert: true);
    public double ResolutionRateCurrentPercent => TotalTicketsCurrent == 0 ? 0 :
        (double)ResolvedTicketsCurrent / TotalTicketsCurrent * 100;
    public double ResolutionRatePreviousPercent => TotalTicketsPrevious == 0 ? 0 :
        (double)ResolvedTicketsPrevious / Math.Max(TotalTicketsPrevious, 1) * 100;
    public double ResolutionRateChangePercent => ComputePercentChange(ResolutionRateCurrentPercent, ResolutionRatePreviousPercent);

    public double AvgResponseHours => AvgResponseMinutesCurrent <= 0 ? 0 : AvgResponseMinutesCurrent / 60d;

    public double ActiveUsersChangePercent => ComputePercentChange(ActiveUsersCreatedCurrent, ActiveUsersCreatedPrevious);
    public double MessageVolumeChangePercent => ComputePercentChange(MessagesCurrent, MessagesPrevious);
    public double StudentEngagementChangePercent => ComputePercentChange(StudentEngagementCurrent, StudentEngagementPrevious);

    private static double ComputePercentChange(double current, double previous, bool invert = false)
    {
        if (invert)
        {
            // When invert is true, a decrease in current compared to previous is positive (improvement)
            var delta = previous - current;
            return previous == 0 ? (current == 0 ? 0 : -100) : delta / previous * 100;
        }

        if (Math.Abs(previous) < double.Epsilon)
        {
            return current <= 0 ? 0 : 100;
        }

        return (current - previous) / previous * 100;
    }
}
