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

    public async Task<IReadOnlyList<AssignmentSubmission>> GetAssignmentSubmissionsAsync(Guid assignmentId, int maxScore = 100)
    {
        var submissions = new List<AssignmentSubmission>();
        if (assignmentId == Guid.Empty)
        {
            return submissions;
        }

        if (_dbConnection is not SqlServerDbConnection sqlConnection)
        {
            Debug.WriteLine("AssignmentService: DbConnection is not SqlServerDbConnection");
            return submissions;
        }

        const string sql = @"
SELECT 
    sub.submission_id,
    sub.assignment_id,
    sub.student_id,
    sub.submitted_at,
    sub.score,
    sub.status,
    sub.notes,
    s.student_number,
    u.first_name,
    u.last_name
FROM assignment_submissions sub
INNER JOIN students s ON s.student_id = sub.student_id
INNER JOIN users u ON u.user_id = s.user_id
WHERE sub.assignment_id = @AssignmentId
ORDER BY sub.submitted_at DESC";

        await using var connection = sqlConnection.GetConnection() as SqlConnection;
        if (connection is null)
        {
            Debug.WriteLine("AssignmentService: Unable to create SQL connection");
            return submissions;
        }

        await connection.OpenAsync();
        await using var command = new SqlCommand(sql, connection);
        command.CommandTimeout = 10;
        command.Parameters.AddWithValue("@AssignmentId", assignmentId);

        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            var firstName = reader.IsDBNull(8) ? "" : reader.GetString(8);
            var lastName = reader.IsDBNull(9) ? "" : reader.GetString(9);
            
            submissions.Add(new AssignmentSubmission
            {
                SubmissionId = reader.GetGuid(0),
                AssignmentId = reader.GetGuid(1),
                StudentId = reader.GetGuid(2),
                SubmittedAt = reader.GetDateTime(3),
                Score = reader.IsDBNull(4) ? null : reader.GetInt32(4),
                Status = reader.GetString(5),
                Notes = reader.IsDBNull(6) ? null : reader.GetString(6),
                StudentNumber = reader.GetString(7),
                StudentName = $"{firstName} {lastName}".Trim(),
                MaxScore = maxScore
            });
        }

        return submissions;
    }
}
