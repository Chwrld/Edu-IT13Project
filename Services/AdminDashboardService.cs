using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using MauiAppIT13.Models;

namespace MauiAppIT13.Services;

public sealed class AdminDashboardService
{
    private readonly string _connectionString;

    public AdminDashboardService(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("EduCrmSql")
            ?? throw new InvalidOperationException("Connection string 'EduCrmSql' not found.");
    }

    public async Task<AdminDashboardSummary> GetSummaryAsync(int periodDays = 30, int activityLimit = 6)
    {
        var now = DateTime.UtcNow;
        var currentStart = now.AddDays(-periodDays);
        var previousStart = currentStart.AddDays(-periodDays);
        var weekStart = now.AddDays(-7);

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();

        var totalUsers = await ExecuteScalarAsync(connection,
            "SELECT COUNT(*) FROM users WHERE status <> 'archived'");

        var newUsersCurrent = await ExecuteScalarAsync(connection,
            @"SELECT COUNT(*) FROM users 
              WHERE status <> 'archived' AND created_at >= @Start AND created_at < @End",
            new SqlParameter("@Start", currentStart),
            new SqlParameter("@End", now));

        var newUsersPrevious = await ExecuteScalarAsync(connection,
            @"SELECT COUNT(*) FROM users 
              WHERE status <> 'archived' AND created_at >= @Start AND created_at < @End",
            new SqlParameter("@Start", previousStart),
            new SqlParameter("@End", currentStart));

        var ticketsSummary = await GetTicketSummaryAsync(connection);
        var announcementsTotal = await ExecuteScalarAsync(connection, "SELECT COUNT(*) FROM announcements");
        var announcementsThisWeek = await ExecuteScalarAsync(connection,
            "SELECT COUNT(*) FROM announcements WHERE created_at >= @WeekStart",
            new SqlParameter("@WeekStart", weekStart));

        var activities = await GetRecentActivitiesAsync(connection, activityLimit);

        return new AdminDashboardSummary
        {
            TotalUsers = totalUsers,
            NewUsersCurrentPeriod = newUsersCurrent,
            NewUsersPreviousPeriod = newUsersPrevious,
            ActiveTickets = ticketsSummary.Active,
            OpenTickets = ticketsSummary.Open,
            InProgressTickets = ticketsSummary.InProgress,
            AnnouncementsTotal = announcementsTotal,
            AnnouncementsThisWeek = announcementsThisWeek,
            RecentActivities = activities
        };
    }

    private static async Task<(int Active, int Open, int InProgress)> GetTicketSummaryAsync(SqlConnection connection)
    {
        const string sql = @"
            SELECT
                SUM(CASE WHEN status IN ('open','in_progress') THEN 1 ELSE 0 END) AS Active,
                SUM(CASE WHEN status = 'open' THEN 1 ELSE 0 END) AS OpenTickets,
                SUM(CASE WHEN status = 'in_progress' THEN 1 ELSE 0 END) AS InProgress
            FROM support_tickets";

        await using var command = new SqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            var active = reader.IsDBNull(0) ? 0 : reader.GetInt32(0);
            var open = reader.IsDBNull(1) ? 0 : reader.GetInt32(1);
            var inProgress = reader.IsDBNull(2) ? 0 : reader.GetInt32(2);
            return (active, open, inProgress);
        }

        return (0, 0, 0);
    }

    private static async Task<IReadOnlyList<AdminActivityItem>> GetRecentActivitiesAsync(SqlConnection connection, int limit)
    {
        const string sql = @"
            SELECT TOP (@Limit) type, title, description, timestamp_utc
            FROM (
                SELECT 
                    'ticket' AS type,
                    CONCAT('Ticket ', COALESCE(ticket_number, LEFT(CONVERT(NVARCHAR(36), ticket_id), 8))) AS title,
                    CONCAT('Status ', COALESCE(status, 'unknown')) AS description,
                    COALESCE(updated_at, created_at, SYSUTCDATETIME()) AS timestamp_utc
                FROM support_tickets
                UNION ALL
                SELECT 
                    'announcement' AS type,
                    title,
                    'Announcement published' AS description,
                    COALESCE(updated_at, created_at, SYSUTCDATETIME()) AS timestamp_utc
                FROM announcements
                UNION ALL
                SELECT 
                    'user' AS type,
                    COALESCE(display_name, email, 'New user') AS title,
                    'New user registered' AS description,
                    COALESCE(created_at, SYSUTCDATETIME()) AS timestamp_utc
                FROM users
            ) activity
            ORDER BY timestamp_utc DESC";

        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@Limit", limit);

        var list = new List<AdminActivityItem>();
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            var type = reader.GetString(0);
            var title = reader.GetString(1);
            var description = reader.GetString(2);
            var timestamp = reader.GetDateTime(3);
            list.Add(MapActivity(type, title, description, timestamp));
        }

        return list;
    }

    private static AdminActivityItem MapActivity(string type, string title, string description, DateTime timestampUtc)
    {
        var (icon, color) = type switch
        {
            "ticket" => ("🎫", "#B7D6F0"),
            "announcement" => ("📢", "#FFBFD2F1"),
            "user" => ("👤", "#FFC4E6DA"),
            _ => ("📋", "#EFF6FF")
        };

        return new AdminActivityItem
        {
            Icon = icon,
            AccentColor = color,
            Title = title,
            Description = description,
            TimestampUtc = timestampUtc
        };
    }

    private static async Task<int> ExecuteScalarAsync(SqlConnection connection, string sql, params SqlParameter[] parameters)
    {
        await using var command = new SqlCommand(sql, connection);
        if (parameters.Length > 0)
        {
            command.Parameters.AddRange(parameters);
        }

        var result = await command.ExecuteScalarAsync();
        return Convert.ToInt32(result ?? 0);
    }
}
