using System.Globalization;

namespace MauiAppIT13.Models;

public sealed class AdminDashboardSummary
{
    public int TotalUsers { get; init; }
    public int NewUsersCurrentPeriod { get; init; }
    public int NewUsersPreviousPeriod { get; init; }
    public int ActiveTickets { get; init; }
    public int OpenTickets { get; init; }
    public int InProgressTickets { get; init; }
    public int AnnouncementsTotal { get; init; }
    public int AnnouncementsThisWeek { get; init; }
    public IReadOnlyList<AdminActivityItem> RecentActivities { get; init; } = Array.Empty<AdminActivityItem>();
    public IReadOnlyList<UserGrowthPoint> UserGrowthTrend { get; init; } = Array.Empty<UserGrowthPoint>();
    public IReadOnlyList<TicketResolutionPoint> TicketResolutionTrend { get; init; } = Array.Empty<TicketResolutionPoint>();
    public IReadOnlyList<AnnouncementViewPoint> AnnouncementViewTrend { get; init; } = Array.Empty<AnnouncementViewPoint>();
    public IReadOnlyList<AnnouncementAudienceStat> AnnouncementAudienceMix { get; init; } = Array.Empty<AnnouncementAudienceStat>();

    public double UserGrowthPercent => ComputePercent(NewUsersCurrentPeriod, NewUsersPreviousPeriod);

    private static double ComputePercent(int current, int previous)
    {
        if (previous == 0)
            return current == 0 ? 0 : 100;

        return (double)(current - previous) / previous * 100;
    }
}

public sealed class AdminActivityItem
{
    public string Icon { get; init; } = "📋";
    public string AccentColor { get; init; } = "#EFF6FF";
    public string Title { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public DateTime TimestampUtc { get; init; }
    public string Metadata { get; init; } = string.Empty;

    public string RelativeTime => FormatRelativeTime(TimestampUtc);

    private static string FormatRelativeTime(DateTime timestampUtc)
    {
        if (timestampUtc == default)
            return "unknown";

        var localTime = timestampUtc.Kind == DateTimeKind.Unspecified
            ? DateTime.SpecifyKind(timestampUtc, DateTimeKind.Utc).ToLocalTime()
            : timestampUtc.ToLocalTime();

        var span = DateTime.Now - localTime;
        if (span.TotalSeconds < 60)
            return "just now";
        if (span.TotalMinutes < 60)
            return $"{Math.Floor(span.TotalMinutes)} min ago";
        if (span.TotalHours < 24)
            return $"{Math.Floor(span.TotalHours)} hr ago";
        if (span.TotalDays < 30)
            return $"{Math.Floor(span.TotalDays)} d ago";

        return localTime.ToString("MMM d", CultureInfo.InvariantCulture);
    }
}

public sealed class UserGrowthPoint
{
    public DateTime PeriodStart { get; init; }
    public string Label { get; init; } = string.Empty;
    public int Total { get; init; }
}

public sealed class TicketResolutionPoint
{
    public DateTime PeriodStart { get; init; }
    public string Label { get; init; } = string.Empty;
    public int OpenCount { get; init; }
    public int InProgressCount { get; init; }
    public int ResolvedCount { get; init; }
}

public sealed class AnnouncementViewPoint
{
    public DateTime PeriodStart { get; init; }
    public string Label { get; init; } = string.Empty;
    public int ViewCount { get; init; }
}

public sealed class AnnouncementAudienceStat
{
    public string Audience { get; init; } = string.Empty;
    public int Count { get; init; }
    public double Percentage { get; set; }
}
