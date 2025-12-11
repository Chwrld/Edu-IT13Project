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

    public async Task<IReadOnlyList<ClassAssignment>> GetClassAssignmentsAsync(Guid courseId, Guid? studentId = null, int limit = 25)
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
),
StudentSubmissions AS (
    SELECT assignment_id, student_id
    FROM assignment_submissions
    WHERE student_id = @StudentId
)
SELECT TOP (@Limit)
    a.assignment_id,
    a.course_id,
    a.title,
    a.description,
    a.deadline,
    a.total_points,
    COALESCE(sc.total_students, 0) AS total_students,
    COALESCE(sub.submitted_count, 0) AS submitted_count,
    CASE WHEN ss.assignment_id IS NOT NULL THEN 1 ELSE 0 END AS has_submitted
FROM class_assignments a
LEFT JOIN StudentCounts sc ON sc.course_id = a.course_id
LEFT JOIN SubmissionCounts sub ON sub.assignment_id = a.assignment_id
LEFT JOIN StudentSubmissions ss ON ss.assignment_id = a.assignment_id
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
        command.Parameters.AddWithValue("@StudentId", studentId ?? Guid.Empty);
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
                SubmittedCount = reader.IsDBNull(7) ? 0 : reader.GetInt32(7),
                StudentHasSubmitted = reader.IsDBNull(8) ? false : reader.GetInt32(8) == 1
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
            Debug.WriteLine("AssignmentService: assignmentId is empty");
            return submissions;
        }

        if (_dbConnection is not SqlServerDbConnection sqlConnection)
        {
            Debug.WriteLine("AssignmentService: DbConnection is not SqlServerDbConnection");
            return submissions;
        }

        // Get all students in the class for this assignment
        const string sql = @"
SELECT 
    s.student_id,
    s.student_number,
    COALESCE(u.display_name, s.student_number) AS display_name,
    sub.submission_id,
    sub.submitted_at,
    sub.score,
    sub.status,
    sub.notes
FROM class_assignments a
INNER JOIN student_courses sc ON sc.course_id = a.course_id
INNER JOIN students s ON s.student_id = sc.student_id
LEFT JOIN users u ON u.user_id = s.student_id
LEFT JOIN assignment_submissions sub ON sub.assignment_id = a.assignment_id AND sub.student_id = s.student_id
WHERE a.assignment_id = @AssignmentId
ORDER BY 
    CASE WHEN sub.submission_id IS NOT NULL THEN 0 ELSE 1 END,
    sub.submitted_at DESC,
    COALESCE(u.display_name, s.student_number) ASC";

        try
        {
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

            Debug.WriteLine($"AssignmentService: Executing GetAssignmentSubmissionsAsync for assignment {assignmentId}");
            await using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var studentId = reader.GetGuid(0);
                var studentNumber = reader.GetString(1);
                var displayName = reader.IsDBNull(2) ? "" : reader.GetString(2);

                // Column order from SQL:
                // 0: student_id, 1: student_number, 2: display_name, 3: submission_id,
                // 4: submitted_at, 5: score, 6: status, 7: notes
                var hasSubmission = !reader.IsDBNull(3);
                
                submissions.Add(new AssignmentSubmission
                {
                    SubmissionId = hasSubmission ? reader.GetGuid(3) : Guid.Empty,
                    AssignmentId = assignmentId,
                    StudentId = studentId,
                    SubmittedAt = hasSubmission ? reader.GetDateTime(4) : DateTime.MinValue,
                    Score = hasSubmission && !reader.IsDBNull(5) ? reader.GetInt32(5) : null,
                    Status = hasSubmission ? reader.GetString(6) : "not submitted",
                    Notes = hasSubmission && !reader.IsDBNull(7) ? reader.GetString(7) : null,
                    StudentNumber = studentNumber,
                    StudentName = displayName,
                    MaxScore = maxScore,
                    HasSubmitted = hasSubmission
                });
            }

            Debug.WriteLine($"AssignmentService: Retrieved {submissions.Count} submissions for assignment {assignmentId}");
            return submissions;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"AssignmentService: GetAssignmentSubmissionsAsync failed - {ex.GetType().Name}: {ex.Message}");
            Debug.WriteLine($"AssignmentService: Stack trace: {ex.StackTrace}");
            throw;
        }
    }

    public async Task<string?> GetSubmissionContentAsync(Guid submissionId)
    {
        if (submissionId == Guid.Empty)
        {
            return null;
        }

        if (_dbConnection is not SqlServerDbConnection sqlConnection)
        {
            Debug.WriteLine("AssignmentService: DbConnection is not SqlServerDbConnection");
            return null;
        }

        const string sql = @"
SELECT submission_content
FROM assignment_submissions
WHERE submission_id = @SubmissionId";

        await using var connection = sqlConnection.GetConnection() as SqlConnection;
        if (connection is null)
        {
            Debug.WriteLine("AssignmentService: Unable to create SQL connection");
            return null;
        }

        await connection.OpenAsync();
        await using var command = new SqlCommand(sql, connection);
        command.CommandTimeout = 10;
        command.Parameters.AddWithValue("@SubmissionId", submissionId);

        var result = await command.ExecuteScalarAsync();
        return result?.ToString();
    }

    public async Task<bool> UpdateSubmissionGradeAsync(Guid submissionId, int score, string? notes)
    {
        if (submissionId == Guid.Empty)
        {
            return false;
        }

        if (_dbConnection is not SqlServerDbConnection sqlConnection)
        {
            Debug.WriteLine("AssignmentService: DbConnection is not SqlServerDbConnection");
            return false;
        }

        const string sql = @"
UPDATE assignment_submissions
SET score = @Score,
    notes = @Notes,
    status = 'graded'
WHERE submission_id = @SubmissionId";

        await using var connection = sqlConnection.GetConnection() as SqlConnection;
        if (connection is null)
        {
            Debug.WriteLine("AssignmentService: Unable to create SQL connection");
            return false;
        }

        await connection.OpenAsync();
        await using var command = new SqlCommand(sql, connection);
        command.CommandTimeout = 10;
        command.Parameters.AddWithValue("@SubmissionId", submissionId);
        command.Parameters.AddWithValue("@Score", score);
        command.Parameters.AddWithValue("@Notes", (object?)notes ?? DBNull.Value);

        var rows = await command.ExecuteNonQueryAsync();
        return rows > 0;
    }

    public async Task<AssignmentSubmission?> GetStudentSubmissionAsync(Guid assignmentId, Guid studentId)
    {
        if (assignmentId == Guid.Empty || studentId == Guid.Empty)
        {
            return null;
        }

        if (_dbConnection is not SqlServerDbConnection sqlConnection)
        {
            Debug.WriteLine("AssignmentService: DbConnection is not SqlServerDbConnection");
            return null;
        }

        const string sql = @"
SELECT submission_id, submission_content, notes, submitted_at, status
FROM assignment_submissions
WHERE assignment_id = @AssignmentId AND student_id = @StudentId";

        await using var connection = sqlConnection.GetConnection() as SqlConnection;
        if (connection is null)
        {
            Debug.WriteLine("AssignmentService: Unable to create SQL connection");
            return null;
        }

        await connection.OpenAsync();
        await using var command = new SqlCommand(sql, connection);
        command.CommandTimeout = 10;
        command.Parameters.AddWithValue("@AssignmentId", assignmentId);
        command.Parameters.AddWithValue("@StudentId", studentId);

        await using var reader = await command.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            return new AssignmentSubmission
            {
                SubmissionId = reader.GetGuid(0),
                AssignmentId = assignmentId,
                StudentId = studentId,
                SubmissionContent = reader.IsDBNull(1) ? null : reader.GetString(1),
                Notes = reader.IsDBNull(2) ? null : reader.GetString(2),
                SubmittedAt = reader.GetDateTime(3),
                Status = reader.GetString(4)
            };
        }

        return null;
    }

    public async Task<Guid?> SubmitAssignmentAsync(Guid assignmentId, Guid studentId, string submissionLink, string? comments)
    {
        if (assignmentId == Guid.Empty || studentId == Guid.Empty || string.IsNullOrWhiteSpace(submissionLink))
        {
            return null;
        }

        if (_dbConnection is not SqlServerDbConnection sqlConnection)
        {
            Debug.WriteLine("AssignmentService: DbConnection is not SqlServerDbConnection");
            return null;
        }

        await using var connection = sqlConnection.GetConnection() as SqlConnection;
        if (connection is null)
        {
            Debug.WriteLine("AssignmentService: Unable to create SQL connection");
            return null;
        }

        await connection.OpenAsync();

        try
        {
            // Check if submission already exists
            const string checkSql = @"
SELECT submission_id FROM assignment_submissions 
WHERE assignment_id = @AssignmentId AND student_id = @StudentId";

            await using var checkCommand = new SqlCommand(checkSql, connection);
            checkCommand.CommandTimeout = 10;
            checkCommand.Parameters.AddWithValue("@AssignmentId", assignmentId);
            checkCommand.Parameters.AddWithValue("@StudentId", studentId);

            var existingSubmissionId = await checkCommand.ExecuteScalarAsync();

            if (existingSubmissionId != null && existingSubmissionId != DBNull.Value)
            {
                // Update existing submission
                const string updateSql = @"
UPDATE assignment_submissions 
SET submitted_at = GETUTCDATE(), 
    status = 'submitted', 
    submission_content = @SubmissionLink, 
    notes = @Comments
WHERE assignment_id = @AssignmentId AND student_id = @StudentId";

                await using var updateCommand = new SqlCommand(updateSql, connection);
                updateCommand.CommandTimeout = 10;
                updateCommand.Parameters.AddWithValue("@AssignmentId", assignmentId);
                updateCommand.Parameters.AddWithValue("@StudentId", studentId);
                updateCommand.Parameters.AddWithValue("@SubmissionLink", submissionLink.Trim());
                updateCommand.Parameters.AddWithValue("@Comments", (object?)comments?.Trim() ?? DBNull.Value);

                var rows = await updateCommand.ExecuteNonQueryAsync();
                Debug.WriteLine($"AssignmentService: Updated existing submission for assignment {assignmentId}, student {studentId}");
                return rows > 0 ? (Guid)existingSubmissionId : null;
            }
            else
            {
                // Insert new submission
                const string insertSql = @"
INSERT INTO assignment_submissions (submission_id, assignment_id, student_id, submitted_at, status, submission_content, notes)
VALUES (@Id, @AssignmentId, @StudentId, GETUTCDATE(), 'submitted', @SubmissionLink, @Comments)";

                var submissionId = Guid.NewGuid();

                await using var insertCommand = new SqlCommand(insertSql, connection);
                insertCommand.CommandTimeout = 10;
                insertCommand.Parameters.AddWithValue("@Id", submissionId);
                insertCommand.Parameters.AddWithValue("@AssignmentId", assignmentId);
                insertCommand.Parameters.AddWithValue("@StudentId", studentId);
                insertCommand.Parameters.AddWithValue("@SubmissionLink", submissionLink.Trim());
                insertCommand.Parameters.AddWithValue("@Comments", (object?)comments?.Trim() ?? DBNull.Value);

                var rows = await insertCommand.ExecuteNonQueryAsync();
                Debug.WriteLine($"AssignmentService: Created new submission for assignment {assignmentId}, student {studentId}");
                return rows > 0 ? submissionId : null;
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"AssignmentService: SubmitAssignmentAsync failed - {ex.GetType().Name}: {ex.Message}");
            Debug.WriteLine($"AssignmentService: Stack trace: {ex.StackTrace}");
            throw;
        }
    }
}
