using System.Collections.ObjectModel;
using System.Diagnostics;
using Microsoft.Data.SqlClient;
using MauiAppIT13.Database;
using MauiAppIT13.Models;

namespace MauiAppIT13.Services;

public class ClassService
{
    private readonly DbConnection _dbConnection;

    public ClassService(DbConnection dbConnection)
    {
        _dbConnection = dbConnection;
    }

    public async Task<ObservableCollection<ClassModel>> GetTeacherClassesAsync(Guid teacherId)
    {
        var classes = new ObservableCollection<ClassModel>();

        if (_dbConnection is not SqlServerDbConnection sqlConnection)
        {
            Debug.WriteLine("ClassService: DbConnection is not SqlServerDbConnection");
            return classes;
        }

        const string sql = @"
            SELECT
                c.course_id,
                c.course_code,
                c.course_name,
                c.schedule,
                c.credits,
                SUM(
                    CASE 
                        WHEN SUBSTRING(s.student_number, 5, 3) = SUBSTRING(c.course_code, 3, 3)
                        THEN 1 ELSE 0
                    END
                ) AS matched_students
            FROM courses c
            LEFT JOIN students s ON s.adviser_id = c.created_by
            WHERE c.created_by = @TeacherId
            GROUP BY c.course_id, c.course_code, c.course_name, c.schedule, c.credits
            ORDER BY c.course_name";

        await using var connection = sqlConnection.GetConnection() as SqlConnection;
        if (connection is null)
        {
            Debug.WriteLine("ClassService: Unable to create SQL connection");
            return classes;
        }

        await connection.OpenAsync();
        await using var command = new SqlCommand(sql, connection);
        command.CommandTimeout = 10;
        command.Parameters.AddWithValue("@TeacherId", teacherId);

        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            var courseCode = reader.GetString(1);
            var classKey = courseCode.Length >= 5 ? courseCode.Substring(2, 3) : string.Empty;

            classes.Add(new ClassModel
            {
                Id = reader.GetGuid(0),
                Code = courseCode,
                Name = reader.GetString(2),
                Schedule = reader.IsDBNull(3) ? "To be scheduled" : reader.GetString(3),
                Credits = reader.GetInt32(4),
                StudentCount = reader.IsDBNull(5) ? 0 : reader.GetInt32(5),
                ClassKey = classKey
            });
        }

        return classes;
    }

    public async Task<ClassModel?> GetClassByIdAsync(Guid classId)
    {
        if (_dbConnection is not SqlServerDbConnection sqlConnection)
            return null;

        const string sql = @"
            SELECT TOP 1
                c.course_id,
                c.course_code,
                c.course_name,
                c.schedule,
                c.credits,
                c.created_by,
                SUM(
                    CASE 
                        WHEN SUBSTRING(s.student_number, 5, 3) = SUBSTRING(c.course_code, 3, 3)
                        THEN 1 ELSE 0
                    END
                ) AS matched_students
            FROM courses c
            LEFT JOIN students s ON s.adviser_id = c.created_by
            WHERE c.course_id = @CourseId
            GROUP BY c.course_id, c.course_code, c.course_name, c.schedule, c.credits, c.created_by";

        await using var connection = sqlConnection.GetConnection() as SqlConnection;
        if (connection is null)
            return null;

        await connection.OpenAsync();
        await using var command = new SqlCommand(sql, connection);
        command.CommandTimeout = 10;
        command.Parameters.AddWithValue("@CourseId", classId);

        await using var reader = await command.ExecuteReaderAsync();
        if (!await reader.ReadAsync())
            return null;

        var courseCode = reader.GetString(1);
        var classKey = courseCode.Length >= 5 ? courseCode.Substring(2, 3) : string.Empty;

        return new ClassModel
        {
            Id = reader.GetGuid(0),
            Code = courseCode,
            Name = reader.GetString(2),
            Schedule = reader.IsDBNull(3) ? "To be scheduled" : reader.GetString(3),
            Credits = reader.GetInt32(4),
            StudentCount = reader.IsDBNull(6) ? 0 : reader.GetInt32(6),
            ClassKey = classKey,
            CreatedBy = reader.GetGuid(5)
        };
    }

    public async Task<ObservableCollection<ClassStudent>> GetClassStudentsAsync(Guid classId)
    {
        var students = new ObservableCollection<ClassStudent>();

        if (_dbConnection is not SqlServerDbConnection sqlConnection)
            return students;

        const string sql = @"
            SELECT 
                u.user_id,
                u.display_name,
                u.email,
                s.student_number,
                s.status
            FROM courses c
            INNER JOIN students s ON s.adviser_id = c.created_by
            INNER JOIN users u ON u.user_id = s.student_id
            WHERE c.course_id = @CourseId
              AND SUBSTRING(s.student_number, 5, 3) = SUBSTRING(c.course_code, 3, 3)
            ORDER BY u.display_name";

        await using var connection = sqlConnection.GetConnection() as SqlConnection;
        if (connection is null)
            return students;

        await connection.OpenAsync();
        await using var command = new SqlCommand(sql, connection);
        command.CommandTimeout = 10;
        command.Parameters.AddWithValue("@CourseId", classId);

        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            students.Add(new ClassStudent
            {
                StudentId = reader.GetGuid(0),
                DisplayName = reader.IsDBNull(1) ? "Student" : reader.GetString(1),
                Email = reader.IsDBNull(2) ? string.Empty : reader.GetString(2),
                StudentNumber = reader.IsDBNull(3) ? string.Empty : reader.GetString(3),
                Status = reader.IsDBNull(4) ? "active" : reader.GetString(4)
            });
        }

        return students;
    }

    public async Task<ObservableCollection<ClassModel>> GetStudentClassesAsync(Guid studentId)
    {
        var classes = new ObservableCollection<ClassModel>();

        if (_dbConnection is not SqlServerDbConnection sqlConnection)
        {
            Debug.WriteLine("ClassService: DbConnection is not SqlServerDbConnection");
            return classes;
        }

        const string sql = @"
            SELECT DISTINCT
                c.course_id,
                c.course_code,
                c.course_name,
                c.schedule,
                c.credits
            FROM courses c
            INNER JOIN students s ON s.adviser_id = c.created_by
            WHERE s.student_id = @StudentId
              AND SUBSTRING(s.student_number, 5, 3) = SUBSTRING(c.course_code, 3, 3)
            ORDER BY c.course_name";

        await using var connection = sqlConnection.GetConnection() as SqlConnection;
        if (connection is null)
        {
            Debug.WriteLine("ClassService: Unable to create SQL connection");
            return classes;
        }

        await connection.OpenAsync();
        await using var command = new SqlCommand(sql, connection);
        command.CommandTimeout = 10;
        command.Parameters.AddWithValue("@StudentId", studentId);

        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            var courseCode = reader.GetString(1);
            var classKey = courseCode.Length >= 5 ? courseCode.Substring(2, 3) : string.Empty;

            classes.Add(new ClassModel
            {
                Id = reader.GetGuid(0),
                Code = courseCode,
                Name = reader.GetString(2),
                Schedule = reader.IsDBNull(3) ? "To be scheduled" : reader.GetString(3),
                Credits = reader.GetInt32(4),
                ClassKey = classKey
            });
        }

        return classes;
    }
}
