using System.Globalization;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using MauiAppIT13.Models;

namespace MauiAppIT13.Services;

public enum ReportPeriod
{
    Last7Days = 7,
    Last30Days = 30,
    Last90Days = 90
}

public sealed class ReportsService
{
    private readonly string _connectionString;

    public ReportsService(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("EduCrmSql")
            ?? throw new InvalidOperationException("Connection string 'EduCrmSql' not found.");
    }

    public async Task<AdminReportMetrics> GetMetricsAsync(ReportPeriod period)
    {
        var (periodStart, periodEnd, previousStart, previousEnd) = GetPeriodBounds(period);

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();

        var totalTicketsCurrent = await ExecuteCountAsync(connection,
            @"SELECT COUNT(*) FROM support_tickets WHERE created_at >= @Start AND created_at < @End",
            periodStart, periodEnd);

        var totalTicketsPrevious = await ExecuteCountAsync(connection,
            @"SELECT COUNT(*) FROM support_tickets WHERE created_at >= @Start AND created_at < @End",
            previousStart, previousEnd);

        var resolvedTicketsCurrent = await ExecuteCountAsync(connection,
            @"SELECT COUNT(*) FROM support_tickets 
              WHERE status IN ('resolved','closed') 
                AND updated_at IS NOT NULL
                AND updated_at >= @Start AND updated_at < @End",
            periodStart, periodEnd);

        var resolvedTicketsPrevious = await ExecuteCountAsync(connection,
            @"SELECT COUNT(*) FROM support_tickets 
              WHERE status IN ('resolved','closed') 
                AND updated_at IS NOT NULL
                AND updated_at >= @Start AND updated_at < @End",
            previousStart, previousEnd);

        var avgResponseMinutesCurrent = await ExecuteAverageAsync(connection,
            @"SELECT AVG(CAST(DATEDIFF(MINUTE, created_at, updated_at) AS FLOAT))
              FROM support_tickets
              WHERE updated_at IS NOT NULL
                AND updated_at >= @Start AND updated_at < @End",
            periodStart, periodEnd);

        var avgResponseMinutesPrevious = await ExecuteAverageAsync(connection,
            @"SELECT AVG(CAST(DATEDIFF(MINUTE, created_at, updated_at) AS FLOAT))
              FROM support_tickets
              WHERE updated_at IS NOT NULL
                AND updated_at >= @Start AND updated_at < @End",
            previousStart, previousEnd);

        var activeUsersTotal = await ExecuteScalarAsync(connection,
            @"SELECT COUNT(*) FROM users WHERE status = 'active'");

        var activeUsersCreatedCurrent = await ExecuteCountAsync(connection,
            @"SELECT COUNT(*) FROM users 
              WHERE status = 'active' 
                AND created_at >= @Start AND created_at < @End",
            periodStart, periodEnd);

        var activeUsersCreatedPrevious = await ExecuteCountAsync(connection,
            @"SELECT COUNT(*) FROM users 
              WHERE status = 'active' 
                AND created_at >= @Start AND created_at < @End",
            previousStart, previousEnd);

        var messagesCurrent = await ExecuteCountAsync(connection,
            @"SELECT COUNT(*) FROM messages 
              WHERE created_at >= @Start AND created_at < @End",
            periodStart, periodEnd);

        var messagesPrevious = await ExecuteCountAsync(connection,
            @"SELECT COUNT(*) FROM messages 
              WHERE created_at >= @Start AND created_at < @End",
            previousStart, previousEnd);

        var studentEngagementCurrent = await ExecuteCountAsync(connection,
            @"SELECT COUNT(*) FROM student_courses 
              WHERE enrolled_at >= @Start AND enrolled_at < @End",
            periodStart, periodEnd);

        var studentEngagementPrevious = await ExecuteCountAsync(connection,
            @"SELECT COUNT(*) FROM student_courses 
              WHERE enrolled_at >= @Start AND enrolled_at < @End",
            previousStart, previousEnd);

        return new AdminReportMetrics
        {
            PeriodStartUtc = periodStart,
            PeriodEndUtc = periodEnd,
            ReportGeneratedUtc = DateTime.UtcNow,
            TotalTicketsCurrent = totalTicketsCurrent,
            TotalTicketsPrevious = totalTicketsPrevious,
            AvgResponseMinutesCurrent = avgResponseMinutesCurrent,
            AvgResponseMinutesPrevious = avgResponseMinutesPrevious,
            ResolvedTicketsCurrent = resolvedTicketsCurrent,
            ResolvedTicketsPrevious = resolvedTicketsPrevious,
            ActiveUsersTotal = activeUsersTotal,
            ActiveUsersCreatedCurrent = activeUsersCreatedCurrent,
            ActiveUsersCreatedPrevious = activeUsersCreatedPrevious,
            MessagesCurrent = messagesCurrent,
            MessagesPrevious = messagesPrevious,
            StudentEngagementCurrent = studentEngagementCurrent,
            StudentEngagementPrevious = studentEngagementPrevious
        };
    }

    private static async Task<int> ExecuteCountAsync(SqlConnection connection, string sql, DateTime start, DateTime end)
    {
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.Add(new SqlParameter("@Start", System.Data.SqlDbType.DateTime2) { Value = start });
        command.Parameters.Add(new SqlParameter("@End", System.Data.SqlDbType.DateTime2) { Value = end });
        var result = await command.ExecuteScalarAsync();
        return Convert.ToInt32(result ?? 0);
    }

    private static async Task<double> ExecuteAverageAsync(SqlConnection connection, string sql, DateTime start, DateTime end)
    {
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.Add(new SqlParameter("@Start", System.Data.SqlDbType.DateTime2) { Value = start });
        command.Parameters.Add(new SqlParameter("@End", System.Data.SqlDbType.DateTime2) { Value = end });
        var result = await command.ExecuteScalarAsync();
        if (result is null || result == DBNull.Value)
            return 0;

        return Convert.ToDouble(result);
    }

    private static async Task<int> ExecuteScalarAsync(SqlConnection connection, string sql)
    {
        await using var command = new SqlCommand(sql, connection);
        var result = await command.ExecuteScalarAsync();
        return Convert.ToInt32(result ?? 0);
    }

    public async Task<ReportExportData> BuildReportAsync(ReportCategory category, ReportPeriod period, AdminReportMetrics? cachedMetrics = null)
    {
        var bounds = GetPeriodBounds(period);
        var metrics = cachedMetrics ?? await GetMetricsAsync(period);

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();

        return category switch
        {
            ReportCategory.TicketSummary => await BuildTicketSummaryAsync(connection, metrics, bounds.periodStart, bounds.periodEnd),
            ReportCategory.StudentActivity => await BuildStudentActivityReportAsync(connection, metrics, bounds.periodStart, bounds.periodEnd),
            ReportCategory.AdviserPerformance => await BuildAdviserPerformanceReportAsync(connection, metrics, bounds.periodStart, bounds.periodEnd),
            ReportCategory.CommunicationAnalytics => await BuildCommunicationAnalyticsReportAsync(connection, metrics, bounds.periodStart, bounds.periodEnd),
            _ => await BuildTicketSummaryAsync(connection, metrics, bounds.periodStart, bounds.periodEnd)
        };
    }

    private async Task<ReportExportData> BuildTicketSummaryAsync(SqlConnection connection, AdminReportMetrics metrics, DateTime periodStart, DateTime periodEnd)
    {
        var headers = new[] { "Metric", "Current Period", "Previous Period", "Change" };
        var rows = new List<IReadOnlyList<string>>
        {
            Row("Total Tickets", FormatNumber(metrics.TotalTicketsCurrent), FormatNumber(metrics.TotalTicketsPrevious), FormatPercent(metrics.TotalTicketsChangePercent)),
            Row("Resolved Tickets", FormatNumber(metrics.ResolvedTicketsCurrent), FormatNumber(metrics.ResolvedTicketsPrevious), FormatPercent(metrics.ResolutionRateChangePercent)),
            Row("Resolution Rate", $"{metrics.ResolutionRateCurrentPercent:0.#}%", $"{metrics.ResolutionRatePreviousPercent:0.#}%", FormatPercent(metrics.ResolutionRateChangePercent)),
            Row("Avg Response Time", FormatDuration(metrics.AvgResponseMinutesCurrent), FormatDuration(metrics.AvgResponseMinutesPrevious), FormatPercent(metrics.AvgResponseChangePercent, true))
        };

        const string sql = @"SELECT COALESCE(status, 'unknown') AS status, COUNT(*) AS total
                             FROM support_tickets
                             WHERE created_at >= @Start AND created_at < @End
                             GROUP BY status
                             ORDER BY total DESC";
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.Add(new SqlParameter("@Start", System.Data.SqlDbType.DateTime2) { Value = periodStart });
        command.Parameters.Add(new SqlParameter("@End", System.Data.SqlDbType.DateTime2) { Value = periodEnd });
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            var status = reader.GetString(0);
            var total = reader.GetInt32(1);
            rows.Add(Row($"Status: {status}", FormatNumber(total), "-", "-"));
        }

        return new ReportExportData
        {
            ReportTitle = "Ticket Summary",
            Category = ReportCategory.TicketSummary,
            PeriodStartUtc = periodStart,
            PeriodEndUtc = periodEnd,
            Metrics = metrics,
            Headers = headers,
            Rows = rows
        };
    }

    private async Task<ReportExportData> BuildStudentActivityReportAsync(SqlConnection connection, AdminReportMetrics metrics, DateTime periodStart, DateTime periodEnd)
    {
        var headers = new[] { "Date", "Activities" };
        var rows = new List<IReadOnlyList<string>>();

        const string sql = @"SELECT CAST(enrolled_at AS DATE) AS activity_date, COUNT(*) AS activities
                             FROM student_courses
                             WHERE enrolled_at >= @Start AND enrolled_at < @End
                             GROUP BY CAST(enrolled_at AS DATE)
                             ORDER BY activity_date";
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.Add(new SqlParameter("@Start", System.Data.SqlDbType.DateTime2) { Value = periodStart });
        command.Parameters.Add(new SqlParameter("@End", System.Data.SqlDbType.DateTime2) { Value = periodEnd });
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            var date = reader.GetDateTime(0);
            var count = reader.GetInt32(1);
            rows.Add(Row(date.ToString("MMM dd, yyyy", CultureInfo.InvariantCulture), FormatNumber(count)));
        }

        if (rows.Count == 0)
        {
            rows.Add(Row("No activity recorded", "0"));
        }

        return new ReportExportData
        {
            ReportTitle = "Student Activity",
            Category = ReportCategory.StudentActivity,
            PeriodStartUtc = periodStart,
            PeriodEndUtc = periodEnd,
            Metrics = metrics,
            Headers = headers,
            Rows = rows
        };
    }

    private async Task<ReportExportData> BuildAdviserPerformanceReportAsync(SqlConnection connection, AdminReportMetrics metrics, DateTime periodStart, DateTime periodEnd)
    {
        var headers = new[] { "Adviser", "Tickets Assigned", "Resolved" };
        var rows = new List<IReadOnlyList<string>>();

        const string sql = @"SELECT COALESCE(u.display_name, 'Unassigned') AS adviser,
                                    COUNT(*) AS total,
                                    SUM(CASE WHEN t.status IN ('resolved','closed') THEN 1 ELSE 0 END) AS resolved
                             FROM support_tickets t
                             LEFT JOIN users u ON t.assigned_to_id = u.user_id
                             WHERE t.created_at >= @Start AND t.created_at < @End
                             GROUP BY u.display_name
                             ORDER BY total DESC";
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.Add(new SqlParameter("@Start", System.Data.SqlDbType.DateTime2) { Value = periodStart });
        command.Parameters.Add(new SqlParameter("@End", System.Data.SqlDbType.DateTime2) { Value = periodEnd });
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            var adviser = reader.GetString(0);
            var total = reader.GetInt32(1);
            var resolved = reader.IsDBNull(2) ? 0 : reader.GetInt32(2);
            rows.Add(Row(adviser, FormatNumber(total), FormatNumber(resolved)));
        }

        if (rows.Count == 0)
        {
            rows.Add(Row("No adviser assignments in this period", "0", "0"));
        }

        return new ReportExportData
        {
            ReportTitle = "Adviser Performance",
            Category = ReportCategory.AdviserPerformance,
            PeriodStartUtc = periodStart,
            PeriodEndUtc = periodEnd,
            Metrics = metrics,
            Headers = headers,
            Rows = rows
        };
    }

    private async Task<ReportExportData> BuildCommunicationAnalyticsReportAsync(SqlConnection connection, AdminReportMetrics metrics, DateTime periodStart, DateTime periodEnd)
    {
        var headers = new[] { "Date", "Messages" };
        var rows = new List<IReadOnlyList<string>>();

        const string sql = @"SELECT CAST(created_at AS DATE) AS message_date, COUNT(*) AS total
                             FROM messages
                             WHERE created_at >= @Start AND created_at < @End
                             GROUP BY CAST(created_at AS DATE)
                             ORDER BY message_date";
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.Add(new SqlParameter("@Start", System.Data.SqlDbType.DateTime2) { Value = periodStart });
        command.Parameters.Add(new SqlParameter("@End", System.Data.SqlDbType.DateTime2) { Value = periodEnd });
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            var date = reader.GetDateTime(0);
            var total = reader.GetInt32(1);
            rows.Add(Row(date.ToString("MMM dd, yyyy", CultureInfo.InvariantCulture), FormatNumber(total)));
        }

        if (rows.Count == 0)
        {
            rows.Add(Row("No messages recorded", "0"));
        }

        return new ReportExportData
        {
            ReportTitle = "Communication Analytics",
            Category = ReportCategory.CommunicationAnalytics,
            PeriodStartUtc = periodStart,
            PeriodEndUtc = periodEnd,
            Metrics = metrics,
            Headers = headers,
            Rows = rows
        };
    }

    private static (DateTime periodStart, DateTime periodEnd, DateTime previousStart, DateTime previousEnd) GetPeriodBounds(ReportPeriod period)
    {
        var periodLengthDays = (int)period;
        var periodEnd = DateTime.UtcNow;
        var periodStart = periodEnd.AddDays(-periodLengthDays);
        var previousEnd = periodStart;
        var previousStart = previousEnd.AddDays(-periodLengthDays);
        return (periodStart, periodEnd, previousStart, previousEnd);
    }

    private static IReadOnlyList<string> Row(params string[] values) => values;

    private static string FormatNumber(double value) => value.ToString("N0", CultureInfo.InvariantCulture);

    private static string FormatPercent(double value, bool invert = false)
    {
        if (double.IsNaN(value) || double.IsInfinity(value))
            return "0%";

        var adjusted = invert ? -value : value;
        return $"{adjusted:0.#}%";
    }

    private static string FormatDuration(double minutes)
    {
        if (minutes <= 0)
            return "n/a";

        if (minutes < 60)
            return $"{minutes:0.#} mins";

        return $"{minutes / 60d:0.#} hrs";
    }
}
