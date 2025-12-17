using System.Data;
using Microsoft.Data.SqlClient;
using MauiAppIT13.Database;
using MauiAppIT13.Models;

namespace MauiAppIT13.Services;

public class StudentDashboardService
{
    private readonly DbConnection _dbConnection;

    public StudentDashboardService(DbConnection dbConnection)
    {
        _dbConnection = dbConnection ?? throw new ArgumentNullException(nameof(dbConnection));
    }

    public async Task<List<GpaTrendData>> GetGradePerformanceByCoursesAsync(Guid studentId)
    {
        try
        {
            var gradeData = new List<GpaTrendData>();

            if (_dbConnection is not SqlServerDbConnection sqlDb)
                return gradeData;

            System.Diagnostics.Debug.WriteLine($"[Dashboard] Getting grade performance for student: {studentId}");

            const string sql = @"
                SELECT TOP 6
                    c.course_code,
                    COALESCE(scg.assignments_score, 0) as score,
                    scg.updated_at
                FROM dbo.student_courses sc
                INNER JOIN dbo.courses c ON sc.course_id = c.course_id
                LEFT JOIN dbo.student_course_grades scg ON sc.course_id = scg.course_id AND sc.student_id = scg.student_id
                WHERE sc.student_id = @StudentId
                ORDER BY scg.updated_at DESC";

            await using var connection = sqlDb.GetConnection() as SqlConnection;
            if (connection == null) return gradeData;

            await connection.OpenAsync();
            await using var command = new SqlCommand(sql, connection);
            command.CommandTimeout = 5;
            command.Parameters.AddWithValue("@StudentId", studentId);

            await using var reader = await command.ExecuteReaderAsync();
            int index = 0;
            while (await reader.ReadAsync())
            {
                var courseName = reader.GetString(0);
                var score = reader.GetDecimal(1);
                var updatedAt = reader.IsDBNull(2) ? DateTime.Now : reader.GetDateTime(2);

                // Convert 0-100 scale to 4.0 scale
                double gpaValue = score > 0 ? (double)score / 25.0 : 0;
                gpaValue = Math.Min(4.0, Math.Max(0, gpaValue)); // Clamp to 0-4.0

                System.Diagnostics.Debug.WriteLine($"[Dashboard] Grade: {courseName} = {score} (GPA: {gpaValue:F2})");

                gradeData.Add(new GpaTrendData
                {
                    Month = courseName,
                    Gpa = Math.Round(gpaValue, 2),
                    Date = updatedAt
                });
                index++;
            }

            System.Diagnostics.Debug.WriteLine($"[Dashboard] Total grades fetched: {gradeData.Count}");
            return gradeData;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"StudentDashboardService: Error getting grade performance - {ex.Message}\n{ex.StackTrace}");
            return new List<GpaTrendData>();
        }
    }

    public async Task<List<AssignmentCompletionData>> GetAssignmentCompletionByCoursesAsync(Guid studentId)
    {
        try
        {
            var completionData = new List<AssignmentCompletionData>();

            if (_dbConnection is not SqlServerDbConnection sqlDb)
                return completionData;

            System.Diagnostics.Debug.WriteLine($"[Dashboard] Getting assignment completion for student: {studentId}");

            const string sql = @"
                SELECT
                    c.course_code,
                    COUNT(DISTINCT ca.assignment_id) as total_assignments,
                    SUM(CASE WHEN asub.submission_id IS NOT NULL THEN 1 ELSE 0 END) as submitted_count
                FROM dbo.student_courses sc
                INNER JOIN dbo.courses c ON sc.course_id = c.course_id
                LEFT JOIN dbo.class_assignments ca ON sc.course_id = ca.course_id
                LEFT JOIN dbo.assignment_submissions asub ON ca.assignment_id = asub.assignment_id 
                    AND asub.student_id = sc.student_id
                WHERE sc.student_id = @StudentId
                GROUP BY c.course_code
                ORDER BY c.course_code";

            await using var connection = sqlDb.GetConnection() as SqlConnection;
            if (connection == null) return completionData;

            await connection.OpenAsync();
            await using var command = new SqlCommand(sql, connection);
            command.CommandTimeout = 5;
            command.Parameters.AddWithValue("@StudentId", studentId);

            await using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var courseName = reader.GetString(0);
                var totalAssignments = reader.GetInt32(1);
                var submittedCount = reader.IsDBNull(2) ? 0 : reader.GetInt32(2);

                System.Diagnostics.Debug.WriteLine($"[Dashboard] Assignment: {courseName} - Total: {totalAssignments}, Submitted: {submittedCount}");

                if (totalAssignments > 0)
                {
                    completionData.Add(new AssignmentCompletionData
                    {
                        ClassName = courseName,
                        Submitted = submittedCount,
                        Total = totalAssignments
                    });
                }
            }

            System.Diagnostics.Debug.WriteLine($"[Dashboard] Total assignment courses fetched: {completionData.Count}");
            return completionData;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"StudentDashboardService: Error getting assignment completion - {ex.Message}\n{ex.StackTrace}");
            return new List<AssignmentCompletionData>();
        }
    }

    // Fallback method for when no real data is available
    public async Task<List<GpaTrendData>> GetGpaTrendAsync(double currentGpa, int months = 6)
    {
        try
        {
            var trendData = new List<GpaTrendData>();
            var now = DateTime.Now;

            for (int i = months - 1; i >= 0; i--)
            {
                var date = now.AddMonths(-i);
                var variation = (Math.Sin(i * 0.5) * 0.3);
                var gpa = Math.Min(4.0, Math.Max(2.0, currentGpa - 0.2 + variation));

                trendData.Add(new GpaTrendData
                {
                    Month = date.ToString("MMM"),
                    Gpa = Math.Round(gpa, 2),
                    Date = date
                });
            }

            return await Task.FromResult(trendData);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"StudentDashboardService: Error getting GPA trend - {ex.Message}");
            return new List<GpaTrendData>();
        }
    }
}
