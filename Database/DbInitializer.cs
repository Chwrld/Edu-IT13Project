using System.Diagnostics;
using MauiAppIT13.Models;
using MauiAppIT13.Utils;

namespace MauiAppIT13.Database;

public static class DbInitializer
{
    public static async Task EnsureSeedDataAsync(DbConnection dbConnection, PasswordHasher passwordHasher)
    {
        try
        {
            Debug.WriteLine("DbInitializer: Starting seed data check...");
            var hasUsers = await dbConnection.HasAnyUsersAsync();
            
            if (hasUsers)
            {
                Debug.WriteLine("DbInitializer: Database already has users, skipping seed.");
                return;
            }

            Debug.WriteLine("DbInitializer: No users found, seeding default users...");

            // Seed default users
            var (adminHash, adminSalt) = passwordHasher.HashPassword("admin123");
            var admin = new User
            {
                Id = Guid.NewGuid(),
                Email = "admin@university.edu",
                PasswordHash = adminHash,
                PasswordSalt = adminSalt,
                Role = Role.Admin,
                DisplayName = "Administrator",
                IsActive = true,
                CreatedAtUtc = DateTime.UtcNow
            };

            var (teacherHash, teacherSalt) = passwordHasher.HashPassword("teacher123");
            var teacher = new User
            {
                Id = Guid.NewGuid(),
                Email = "teacher@university.edu",
                PasswordHash = teacherHash,
                PasswordSalt = teacherSalt,
                Role = Role.Teacher,
                DisplayName = "John Teacher",
                IsActive = true,
                CreatedAtUtc = DateTime.UtcNow
            };

            var (studentHash, studentSalt) = passwordHasher.HashPassword("student123");
            var student = new User
            {
                Id = Guid.NewGuid(),
                Email = "student@university.edu",
                PasswordHash = studentHash,
                PasswordSalt = studentSalt,
                Role = Role.Student,
                DisplayName = "Jane Student",
                IsActive = true,
                CreatedAtUtc = DateTime.UtcNow
            };

            await dbConnection.SaveUserAsync(admin);
            Debug.WriteLine("DbInitializer: Saved admin user");
            
            await dbConnection.SaveUserAsync(teacher);
            Debug.WriteLine("DbInitializer: Saved teacher user");
            
            await dbConnection.SaveUserAsync(student);
            Debug.WriteLine("DbInitializer: Saved student user");
            
            Debug.WriteLine("DbInitializer: Seed data completed successfully!");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"DbInitializer: ERROR - {ex.GetType().Name}: {ex.Message}");
            Debug.WriteLine($"DbInitializer: Stack trace: {ex.StackTrace}");
        }
    }
}
