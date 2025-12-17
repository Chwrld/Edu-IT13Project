using System.Data;
using Microsoft.Data.SqlClient;
using MauiAppIT13.Database;
using MauiAppIT13.Models;

namespace MauiAppIT13.Services;

public class TeacherDashboardService
{
    private readonly DbConnection _dbConnection;

    public TeacherDashboardService(DbConnection dbConnection)
    {
        _dbConnection = dbConnection ?? throw new ArgumentNullException(nameof(dbConnection));
    }

    public async Task<List<ClassPerformanceData>> GetClassPerformanceAsync(Guid teacherId)
    {
        try
        {
            var performanceData = new List<ClassPerformanceData>();

            if (_dbConnection is not SqlServerDbConnection sqlDb)
                return performanceData;

            System.Diagnostics.Debug.WriteLine($"[TeacherDashboard] Getting class performance for teacher: {teacherId}");

            const string sql = @"
                SELECT
                    c.course_code,
                    COALESCE(AVG(CAST(scg.assignments_score AS FLOAT)), 0) as avg_grade,
                    COUNT(DISTINCT sc.student_id) as student_count
                FROM dbo.courses c
                INNER JOIN dbo.student_courses sc ON c.course_id = sc.course_id
                LEFT JOIN dbo.student_course_grades scg ON sc.course_id = scg.course_id AND sc.student_id = scg.student_id
                WHERE c.created_by = @TeacherId
                GROUP BY c.course_code
                ORDER BY c.course_code";

            await using var connection = sqlDb.GetConnection() as SqlConnection;
            if (connection == null) return performanceData;

            await connection.OpenAsync();
            await using var command = new SqlCommand(sql, connection);
            command.CommandTimeout = 5;
            command.Parameters.AddWithValue("@TeacherId", teacherId);

            await using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var className = reader.GetString(0);
                var avgGrade = reader.GetDouble(1);
                var studentCount = reader.GetInt32(2);

                System.Diagnostics.Debug.WriteLine($"[TeacherDashboard] Class: {className} - Avg Grade: {avgGrade:F2}, Students: {studentCount}");

                performanceData.Add(new ClassPerformanceData
                {
                    ClassName = className,
                    AverageGrade = Math.Round(avgGrade / 25.0, 2),
                    StudentCount = studentCount
                });
            }

            System.Diagnostics.Debug.WriteLine($"[TeacherDashboard] Total classes fetched: {performanceData.Count}");
            return performanceData;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"TeacherDashboardService: Error getting class performance - {ex.Message}\n{ex.StackTrace}");
            return new List<ClassPerformanceData>();
        }
    }

    public async Task<List<SubmissionStatusData>> GetSubmissionStatusAsync(Guid teacherId)
    {
        try
        {
            var submissionData = new List<SubmissionStatusData>();

            if (_dbConnection is not SqlServerDbConnection sqlDb)
                return submissionData;

            System.Diagnostics.Debug.WriteLine($"[TeacherDashboard] Getting submission status for teacher: {teacherId}");

            const string sql = @"
                SELECT
                    c.course_code,
                    COUNT(DISTINCT ca.assignment_id) as total_assignments,
                    COUNT(DISTINCT CASE WHEN asub.submission_id IS NOT NULL THEN asub.submission_id ELSE NULL END) as submitted_count,
                    COUNT(DISTINCT sc.student_id) as student_count
                FROM dbo.courses c
                INNER JOIN dbo.student_courses sc ON c.course_id = sc.course_id
                LEFT JOIN dbo.class_assignments ca ON c.course_id = ca.course_id
                LEFT JOIN dbo.assignment_submissions asub ON ca.assignment_id = asub.assignment_id
                WHERE c.created_by = @TeacherId
                GROUP BY c.course_code
                ORDER BY c.course_code";

            await using var connection = sqlDb.GetConnection() as SqlConnection;
            if (connection == null) return submissionData;

            await connection.OpenAsync();
            await using var command = new SqlCommand(sql, connection);
            command.CommandTimeout = 5;
            command.Parameters.AddWithValue("@TeacherId", teacherId);

            await using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var className = reader.GetString(0);
                var totalAssignments = reader.GetInt32(1);
                var submittedCount = reader.GetInt32(2);
                var studentCount = reader.GetInt32(3);

                var totalExpected = totalAssignments * studentCount;

                System.Diagnostics.Debug.WriteLine($"[TeacherDashboard] Submission: {className} - Total: {totalExpected}, Submitted: {submittedCount}");

                submissionData.Add(new SubmissionStatusData
                {
                    ClassName = className,
                    Submitted = submittedCount,
                    Total = totalExpected > 0 ? totalExpected : 1
                });
            }

            System.Diagnostics.Debug.WriteLine($"[TeacherDashboard] Total submission statuses fetched: {submissionData.Count}");
            return submissionData;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"TeacherDashboardService: Error getting submission status - {ex.Message}\n{ex.StackTrace}");
            return new List<SubmissionStatusData>();
        }
    }

    public async Task<List<StudentAcademicRiskData>> GetStudentsAtAcademicRiskAsync(Guid teacherId)
    {
        try
        {
            var riskData = new List<StudentAcademicRiskData>();

            if (_dbConnection is not SqlServerDbConnection sqlDb)
                return riskData;

            const string sql = @"
                SELECT
                    u.display_name,
                    c.course_code,
                    ISNULL(scg.assignments_score, 0)
                FROM dbo.courses c
                INNER JOIN dbo.student_courses sc ON c.course_id = sc.course_id
                INNER JOIN dbo.users u ON sc.student_id = u.user_id
                LEFT JOIN dbo.student_course_grades scg ON sc.course_id = scg.course_id AND sc.student_id = scg.student_id
                WHERE c.created_by = @TeacherId
                  AND ISNULL(scg.assignments_score, 0) < 70
                ORDER BY ISNULL(scg.assignments_score, 0) ASC";

            await using var connection = sqlDb.GetConnection() as SqlConnection;
            if (connection == null) return riskData;

            await connection.OpenAsync();
            await using var command = new SqlCommand(sql, connection);
            command.CommandTimeout = 5;
            command.Parameters.AddWithValue("@TeacherId", teacherId);

            await using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var studentName = reader.GetString(0);
                var className = reader.GetString(1);
                var gradeValue = reader.GetDecimal(2);
                var grade = (double)gradeValue;

                var riskLevel = grade switch
                {
                    < 50 => "Critical",
                    < 60 => "At Risk",
                    _ => "Low"
                };

                riskData.Add(new StudentAcademicRiskData
                {
                    StudentName = studentName,
                    ClassName = className,
                    Grade = Math.Round(grade / 25.0, 2),
                    RiskLevel = riskLevel
                });
            }

            return riskData;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"TeacherDashboardService: Error getting at-risk students - {ex.Message}\n{ex.StackTrace}");
            return new List<StudentAcademicRiskData>();
        }
    }
}
