using System.Text;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace MauiAppIT13.Services;

public sealed record AdminDataExportResult(IReadOnlyList<string> FilePaths);

public sealed class AdminDataExportService
{
    private readonly string _connectionString;

    public AdminDataExportService(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("EduCrmSql")
            ?? throw new InvalidOperationException("Connection string 'EduCrmSql' not found.");
    }

    public async Task<AdminDataExportResult> ExportAsync(string directory)
    {
        Directory.CreateDirectory(directory);

        var tickets = await LoadTicketRowsAsync();
        var teacherClassRows = await LoadTeacherClassRowsAsync();

        var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
        var ticketFile = Path.Combine(directory, $"dashboard_tickets_{timestamp}.csv");
        var teacherFile = Path.Combine(directory, $"dashboard_teachers_{timestamp}.csv");

        await WriteCsvAsync(ticketFile, GetTicketHeaders(), tickets);
        await WriteCsvAsync(teacherFile, GetTeacherHeaders(), teacherClassRows);

        return new AdminDataExportResult(new[] { ticketFile, teacherFile });
    }

    private async Task<List<string[]>> LoadTicketRowsAsync()
    {
        const string sql = @"
            SELECT 
                t.ticket_number,
                t.title,
                t.description,
                t.status,
                t.priority,
                t.created_at,
                t.updated_at,
                student.display_name AS student_name,
                student.email AS student_email,
                assignee.display_name AS assigned_to_name
            FROM support_tickets t
            LEFT JOIN users student ON student.user_id = t.student_id
            LEFT JOIN users assignee ON assignee.user_id = t.assigned_to_id
            ORDER BY t.created_at DESC";

        var rows = new List<string[]>();
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();
        await using var command = new SqlCommand(sql, connection) { CommandTimeout = 15 };
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            rows.Add(new[]
            {
                reader.IsDBNull(0) ? string.Empty : reader.GetString(0),
                reader.IsDBNull(1) ? string.Empty : reader.GetString(1),
                reader.IsDBNull(2) ? string.Empty : reader.GetString(2),
                reader.IsDBNull(3) ? string.Empty : reader.GetString(3),
                reader.IsDBNull(4) ? string.Empty : reader.GetString(4),
                reader.IsDBNull(5) ? string.Empty : reader.GetDateTime(5).ToString("u"),
                reader.IsDBNull(6) ? string.Empty : reader.GetDateTime(6).ToString("u"),
                reader.IsDBNull(7) ? string.Empty : reader.GetString(7),
                reader.IsDBNull(8) ? string.Empty : reader.GetString(8),
                reader.IsDBNull(9) ? string.Empty : reader.GetString(9)
            });
        }

        return rows;
    }

    private async Task<List<string[]>> LoadTeacherClassRowsAsync()
    {
        const string sql = @"
            SELECT 
                teacher.user_id AS teacher_id,
                teacher.display_name AS teacher_name,
                teacher.email AS teacher_email,
                c.course_id,
                c.course_code,
                c.course_name,
                ISNULL(c.schedule, 'To be scheduled') AS schedule,
                c.credits,
                student.user_id AS student_id,
                student.display_name AS student_name,
                student.email AS student_email,
                s.student_number
            FROM users teacher
            LEFT JOIN courses c ON c.created_by = teacher.user_id
            LEFT JOIN students s ON s.adviser_id = teacher.user_id
            LEFT JOIN users student ON student.user_id = s.student_id
            WHERE teacher.role = 'Teacher'
              AND (c.course_id IS NOT NULL OR s.student_id IS NOT NULL)
              AND (
                    c.course_code IS NULL OR LEN(c.course_code) < 5 OR
                    s.student_number IS NULL OR LEN(s.student_number) < 7 OR
                    SUBSTRING(s.student_number, 5, 3) = SUBSTRING(c.course_code, 3, 3)
                  )
            ORDER BY teacher.display_name, c.course_name, student.display_name";

        var rows = new List<string[]>();
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();
        await using var command = new SqlCommand(sql, connection) { CommandTimeout = 30 };
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            rows.Add(new[]
            {
                reader.IsDBNull(0) ? string.Empty : reader.GetGuid(0).ToString(),
                reader.IsDBNull(1) ? string.Empty : reader.GetString(1),
                reader.IsDBNull(2) ? string.Empty : reader.GetString(2),
                reader.IsDBNull(3) ? string.Empty : reader.GetGuid(3).ToString(),
                reader.IsDBNull(4) ? string.Empty : reader.GetString(4),
                reader.IsDBNull(5) ? string.Empty : reader.GetString(5),
                reader.IsDBNull(6) ? string.Empty : reader.GetString(6),
                reader.IsDBNull(7) ? string.Empty : reader.GetInt32(7).ToString(),
                reader.IsDBNull(8) ? string.Empty : reader.GetGuid(8).ToString(),
                reader.IsDBNull(9) ? string.Empty : reader.GetString(9),
                reader.IsDBNull(10) ? string.Empty : reader.GetString(10),
                reader.IsDBNull(11) ? string.Empty : reader.GetString(11)
            });
        }

        return rows;
    }

    private static string[] GetTicketHeaders() =>
        new[]
        {
            "TicketNumber",
            "Title",
            "Description",
            "Status",
            "Priority",
            "CreatedAtUtc",
            "UpdatedAtUtc",
            "StudentName",
            "StudentEmail",
            "AssignedTo"
        };

    private static string[] GetTeacherHeaders() =>
        new[]
        {
            "TeacherId",
            "TeacherName",
            "TeacherEmail",
            "ClassId",
            "CourseCode",
            "CourseName",
            "Schedule",
            "Credits",
            "StudentId",
            "StudentName",
            "StudentEmail",
            "StudentNumber"
        };

    private static async Task WriteCsvAsync(string path, string[] headers, IEnumerable<string[]> rows)
    {
        var sb = new StringBuilder();
        sb.AppendLine(string.Join(",", headers.Select(EscapeCsv)));
        foreach (var row in rows)
        {
            sb.AppendLine(string.Join(",", row.Select(EscapeCsv)));
        }

        await File.WriteAllTextAsync(path, sb.ToString(), Encoding.UTF8);
    }

    private static string EscapeCsv(string value)
    {
        value ??= string.Empty;
        return value.Contains('"') || value.Contains(',')
            ? $"\"{value.Replace("\"", "\"\"")}\""
            : value;
    }
}
