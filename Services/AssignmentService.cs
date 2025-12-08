using System.Diagnostics;
using Microsoft.Data.SqlClient;
using MauiAppIT13.Database;
using MauiAppIT13.Models;

namespace MauiAppIT13.Services;

public sealed class AssignmentService
{
    private readonly DbConnection _dbConnection;

    public AssignmentService(DbConnection dbConnection)
    {
        _dbConnection = dbConnection;
    }

    public async Task<IReadOnlyList<ClassAssignment>> GetClassAssignmentsAsync(Guid courseId, int limit = 25)
    {
        var assignments = new List<ClassAssignment>();
        if (courseId == Guid.Empty)
        {
            return assignments;
        }

        if (_dbConnection is not SqlServerDbConnection sqlConnection)
        {
            Debug.WriteLine("AssignmentService: DbConnection is not SqlServerDbConnection");
            return assignments;
        }

        const string sql = @"
WITH StudentCounts AS (
    SELECT course_id, COUNT(*) AS total_students
    FROM student_courses
    GROUP BY course_id
),
SubmissionCounts AS (
    SELECT assignment_id, COUNT(*) AS submitted_count
    FROM assignment_submissions
    GROUP BY assignment_id
)
SELECT TOP (@Limit)
    a.assignment_id,
    a.course_id,
    a.title,
    a.description,
    a.deadline,
    a.total_points,
    COALESCE(sc.total_students, 0) AS total_students,
    COALESCE(sub.submitted_count, 0) AS submitted_count
FROM class_assignments a
LEFT JOIN StudentCounts sc ON sc.course_id = a.course_id
LEFT JOIN SubmissionCounts sub ON sub.assignment_id = a.assignment_id
WHERE a.course_id = @CourseId
ORDER BY a.deadline ASC";

        await using var connection = sqlConnection.GetConnection() as SqlConnection;
        if (connection is null)
        {
            Debug.WriteLine("AssignmentService: Unable to create SQL connection");
            return assignments;
        }

        await connection.OpenAsync();
        await using var command = new SqlCommand(sql, connection);
        command.CommandTimeout = 10;
        command.Parameters.AddWithValue("@CourseId", courseId);
        command.Parameters.AddWithValue("@Limit", limit);

        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            assignments.Add(new ClassAssignment
            {
                Id = reader.GetGuid(0),
                ClassId = reader.GetGuid(1),
                Title = reader.GetString(2),
                Description = reader.GetString(3),
                Deadline = reader.GetDateTime(4),
                TotalPoints = reader.GetInt32(5),
                TotalStudents = reader.IsDBNull(6) ? 0 : reader.GetInt32(6),
                SubmittedCount = reader.IsDBNull(7) ? 0 : reader.GetInt32(7)
            });
        }

        return assignments;
    }

    public async Task<Guid?> CreateAssignmentAsync(Guid courseId, string title, string description, DateTime deadlineUtc, int totalPoints, Guid createdBy)
    {
        if (courseId == Guid.Empty || string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(description))
        {
            return null;
        }

        if (_dbConnection is not SqlServerDbConnection sqlConnection)
        {
            Debug.WriteLine("AssignmentService: DbConnection is not SqlServerDbConnection");
            return null;
        }

        const string sql = @"
INSERT INTO class_assignments (assignment_id, course_id, title, description, deadline, total_points, created_by, created_at)
VALUES (@Id, @CourseId, @Title, @Description, @DeadlineUtc, @TotalPoints, @CreatedBy, GETUTCDATE())";

        var assignmentId = Guid.NewGuid();

        await using var connection = sqlConnection.GetConnection() as SqlConnection;
        if (connection is null)
        {
            Debug.WriteLine("AssignmentService: Unable to create SQL connection");
            return null;
        }

        await connection.OpenAsync();
        await using var command = new SqlCommand(sql, connection);
        command.CommandTimeout = 10;
        command.Parameters.AddWithValue("@Id", assignmentId);
        command.Parameters.AddWithValue("@CourseId", courseId);
        command.Parameters.AddWithValue("@Title", title.Trim());
        command.Parameters.AddWithValue("@Description", description.Trim());
        command.Parameters.AddWithValue("@DeadlineUtc", deadlineUtc);
        command.Parameters.AddWithValue("@TotalPoints", totalPoints);
        command.Parameters.AddWithValue("@CreatedBy", createdBy);

        var rows = await command.ExecuteNonQueryAsync();
        return rows > 0 ? assignmentId : null;
    }
}
