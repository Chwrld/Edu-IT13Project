using System.Data;
using System.Diagnostics;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using MauiAppIT13.Models;

namespace MauiAppIT13.Database;

/// <summary>
/// SQL Server implementation of database connection.
/// Requires SQL Server to be running and database to be initialized.
/// </summary>
public sealed class SqlServerDbConnection : DbConnection
{
    private readonly string _connectionString;

    public SqlServerDbConnection(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("EduCrmSql")
            ?? throw new InvalidOperationException("Connection string 'EduCrmSql' not found in appsettings.json");
    }

    private SqlConnection CreateConnection() => new(_connectionString);

    public override async Task<bool> HasAnyUsersAsync()
    {
        try
        {
            const string sql = "SELECT COUNT(*) FROM users";
            await using var connection = CreateConnection();
            await connection.OpenAsync();
            await using var command = new SqlCommand(sql, connection);
            command.CommandTimeout = 5;
            var count = (int?)await command.ExecuteScalarAsync() ?? 0;
            return count > 0;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"HasAnyUsersAsync failed: {ex.Message}");
            return false;
        }
    }

    public override async Task<User?> GetUserByEmailAsync(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return null;

        const string sql = @"SELECT TOP (1)
                                user_id,
                                email,
                                password_hash,
                                password_salt,
                                role,
                                display_name,
                                phone_number,
                                address,
                                profile_picture_url,
                                is_active,
                                created_at
                             FROM users
                             WHERE email = @Email";

        try
        {
            await using var connection = CreateConnection();
            await connection.OpenAsync();
            await using var command = new SqlCommand(sql, connection);
            command.CommandTimeout = 5;
            command.Parameters.AddWithValue("@Email", email);

            await using var reader = await command.ExecuteReaderAsync();
            if (!await reader.ReadAsync())
                return null;

            return MapUser(reader);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"GetUserByEmailAsync failed: {ex.Message}");
            return null;
        }
    }

    public override async Task<IReadOnlyCollection<User>> GetUsersAsync()
    {
        const string sql = "SELECT user_id, email, password_hash, password_salt, role, display_name, phone_number, address, profile_picture_url, is_active, created_at FROM users";
        var users = new List<User>();

        try
        {
            await using var connection = CreateConnection();
            await connection.OpenAsync();
            await using var command = new SqlCommand(sql, connection);
            command.CommandTimeout = 5;
            await using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                users.Add(MapUser(reader));
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"GetUsersAsync failed: {ex.Message}");
        }

        return users.AsReadOnly();
    }

    public override async Task SaveUserAsync(User user)
    {
        ArgumentNullException.ThrowIfNull(user);
        if (string.IsNullOrWhiteSpace(user.Email))
            throw new ArgumentException("User email is required", nameof(user));

        const string sql = @"
            IF EXISTS (SELECT 1 FROM users WHERE user_id = @UserId)
                UPDATE users
                SET email = @Email,
                    password_hash = @PasswordHash,
                    password_salt = @PasswordSalt,
                    role = @Role,
                    display_name = @DisplayName,
                    is_active = @IsActive
                WHERE user_id = @UserId
            ELSE
                INSERT INTO users (user_id, email, password_hash, password_salt, role, display_name, is_active, created_at)
                VALUES (@UserId, @Email, @PasswordHash, @PasswordSalt, @Role, @DisplayName, @IsActive, @CreatedAt)";

        try
        {
            await using var connection = CreateConnection();
            await connection.OpenAsync();
            await using var command = new SqlCommand(sql, connection);
            command.CommandTimeout = 5;

            command.Parameters.AddWithValue("@UserId", user.Id);
            command.Parameters.AddWithValue("@Email", user.Email);
            command.Parameters.AddWithValue("@PasswordHash", user.PasswordHash);
            command.Parameters.AddWithValue("@PasswordSalt", user.PasswordSalt);
            command.Parameters.AddWithValue("@Role", user.Role.ToString());
            command.Parameters.AddWithValue("@DisplayName", user.DisplayName);
            command.Parameters.AddWithValue("@IsActive", user.IsActive);
            command.Parameters.AddWithValue("@CreatedAt", user.CreatedAtUtc);

            await command.ExecuteNonQueryAsync();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"SaveUserAsync failed: {ex.Message}");
            throw new InvalidOperationException($"Failed to save user: {ex.Message}", ex);
        }
    }

    private static User MapUser(SqlDataReader reader)
    {
        return new User
        {
            Id = reader.GetGuid(0),
            Email = reader.GetString(1),
            PasswordHash = reader.GetString(2),
            PasswordSalt = reader.GetString(3),
            Role = Enum.Parse<Role>(reader.GetString(4)),
            DisplayName = reader.GetString(5),
            PhoneNumber = reader.IsDBNull(6) ? null : reader.GetString(6),
            Address = reader.IsDBNull(7) ? null : reader.GetString(7),
            ProfilePictureUrl = reader.IsDBNull(8) ? null : reader.GetString(8),
            IsActive = reader.GetBoolean(9),
            CreatedAtUtc = reader.GetDateTime(10)
        };
    }
}
