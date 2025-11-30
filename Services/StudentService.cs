using System.Collections.ObjectModel;
using Microsoft.Data.SqlClient;
using MauiAppIT13.Models;

namespace MauiAppIT13.Services;

public class StudentService
{
    public StudentService()
    {
    }

    public async Task<Student?> GetStudentByUserIdAsync(Guid userId)
    {
        try
        {
            System.Diagnostics.Debug.WriteLine($"StudentService: Fetching student for userId: {userId}");

            const string sql = @"
                SELECT 
                    student_id,
                    student_number,
                    program,
                    year_level,
                    gpa,
                    status,
                    adviser_id,
                    created_at
                FROM students
                WHERE student_id = @UserId";

            const string connectionString = "Data Source=LAPTOP-L1R9L9R3\\SQLEXPRESS01;Initial Catalog=EduCRM;Integrated Security=True;Connect Timeout=10;Encrypt=False;Trust Server Certificate=True;";
            
            System.Diagnostics.Debug.WriteLine($"StudentService: Connecting to database...");
            await using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();
            System.Diagnostics.Debug.WriteLine($"StudentService: Connection opened successfully");
            
            using var command = connection.CreateCommand();
            command.CommandText = sql;
            command.Parameters.AddWithValue("@UserId", userId);

            using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                var student = new Student
                {
                    StudentId = reader.GetGuid(0),
                    StudentNumber = reader.GetString(1),
                    Program = reader.IsDBNull(2) ? null : reader.GetString(2),
                    YearLevel = reader.IsDBNull(3) ? null : reader.GetString(3),
                    GPA = reader.IsDBNull(4) ? null : reader.GetDecimal(4),
                    Status = reader.GetString(5),
                    AdviserID = reader.IsDBNull(6) ? null : reader.GetGuid(6),
                    CreatedAt = reader.GetDateTime(7)
                };
                System.Diagnostics.Debug.WriteLine($"StudentService: Student found - {student.StudentNumber}");
                return student;
            }

            System.Diagnostics.Debug.WriteLine($"StudentService: No student record found for userId: {userId}");
            return null;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"StudentService: Error getting student - {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"StudentService: Stack trace - {ex.StackTrace}");
            return null;
        }
    }

    public async Task<ObservableCollection<StudentAchievement>> GetStudentAchievementsAsync(Guid studentId)
    {
        try
        {
            System.Diagnostics.Debug.WriteLine($"StudentService: Fetching achievements for studentId: {studentId}");

            const string sql = @"
                SELECT 
                    achievement_id,
                    student_id,
                    achievement_name,
                    description,
                    awarded_by,
                    awarded_date
                FROM student_achievements
                WHERE student_id = @StudentId
                ORDER BY awarded_date DESC";

            const string connectionString = "Data Source=LAPTOP-L1R9L9R3\\SQLEXPRESS01;Initial Catalog=EduCRM;Integrated Security=True;Connect Timeout=10;Encrypt=False;Trust Server Certificate=True;";
            
            var achievements = new ObservableCollection<StudentAchievement>();

            System.Diagnostics.Debug.WriteLine($"StudentService: Connecting to database for achievements...");
            await using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();
            System.Diagnostics.Debug.WriteLine($"StudentService: Connection opened successfully for achievements");
            
            using var command = connection.CreateCommand();
            command.CommandText = sql;
            command.Parameters.AddWithValue("@StudentId", studentId);

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                achievements.Add(new StudentAchievement
                {
                    AchievementId = reader.GetGuid(0),
                    StudentId = reader.GetGuid(1),
                    AchievementName = reader.GetString(2),
                    Description = reader.IsDBNull(3) ? null : reader.GetString(3),
                    AwardedBy = reader.IsDBNull(4) ? null : reader.GetGuid(4),
                    AwardedDate = reader.GetDateTime(5)
                });
            }

            System.Diagnostics.Debug.WriteLine($"StudentService: Found {achievements.Count} achievements for student");
            return achievements;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"StudentService: Error getting achievements - {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"StudentService: Stack trace - {ex.StackTrace}");
            return new ObservableCollection<StudentAchievement>();
        }
    }
}
