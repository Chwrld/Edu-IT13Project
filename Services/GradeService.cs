using System.Diagnostics;
using Microsoft.Data.SqlClient;
using MauiAppIT13.Database;
using MauiAppIT13.Models;

namespace MauiAppIT13.Services;

public sealed class GradeService
{
    private readonly DbConnection _dbConnection;

    public GradeService(DbConnection dbConnection)
    {
        _dbConnection = dbConnection;
    }

    public async Task<IReadOnlyList<StudentGradeSummary>> GetClassGradesAsync(Guid courseId)
    {
        var grades = new List<StudentGradeSummary>();
        if (courseId == Guid.Empty)
        {
            return grades;
        }

        if (_dbConnection is not SqlServerDbConnection sqlConnection)
        {
            Debug.WriteLine("GradeService: DbConnection is not SqlServerDbConnection");
            return grades;
        }

        const string sql = @"
SELECT 
    g.grade_id,
    g.student_id,
    u.display_name,
    g.assignments_score,
    g.activities_score,
    g.exams_score,
    g.projects_score
FROM student_course_grades g
INNER JOIN users u ON u.user_id = g.student_id
WHERE g.course_id = @CourseId
ORDER BY u.display_name";

        await using var connection = sqlConnection.GetConnection() as SqlConnection;
        if (connection is null)
        {
            Debug.WriteLine("GradeService: Unable to create SQL connection");
            return grades;
        }

        await connection.OpenAsync();
        await using var command = new SqlCommand(sql, connection);
        command.CommandTimeout = 10;
        command.Parameters.AddWithValue("@CourseId", courseId);

        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            grades.Add(new StudentGradeSummary
            {
                GradeId = reader.GetGuid(0),
                StudentId = reader.GetGuid(1),
                StudentName = reader.IsDBNull(2) ? "Student" : reader.GetString(2),
                AssignmentsScore = (double)reader.GetDecimal(3),
                ActivitiesScore = (double)reader.GetDecimal(4),
                ExamsScore = (double)reader.GetDecimal(5),
                ProjectsScore = (double)reader.GetDecimal(6)
            });
        }

        return grades;
    }

    public async Task<bool> UpdateStudentGradeAsync(Guid gradeId, double assignmentsScore, double activitiesScore, double examsScore, double projectsScore)
    {
        if (gradeId == Guid.Empty)
        {
            return false;
        }

        if (_dbConnection is not SqlServerDbConnection sqlConnection)
        {
            Debug.WriteLine("GradeService: DbConnection is not SqlServerDbConnection");
            return false;
        }

        const string sql = @"
UPDATE student_course_grades
SET assignments_score = @Assignments,
    activities_score = @Activities,
    exams_score = @Exams,
    projects_score = @Projects,
    updated_at = GETUTCDATE()
WHERE grade_id = @GradeId";

        await using var connection = sqlConnection.GetConnection() as SqlConnection;
        if (connection is null)
        {
            Debug.WriteLine("GradeService: Unable to create SQL connection for update");
            return false;
        }

        await connection.OpenAsync();
        await using var command = new SqlCommand(sql, connection);
        command.CommandTimeout = 10;
        command.Parameters.AddWithValue("@GradeId", gradeId);
        command.Parameters.AddWithValue("@Assignments", assignmentsScore);
        command.Parameters.AddWithValue("@Activities", activitiesScore);
        command.Parameters.AddWithValue("@Exams", examsScore);
        command.Parameters.AddWithValue("@Projects", projectsScore);

        var rows = await command.ExecuteNonQueryAsync();
        return rows > 0;
    }
}
