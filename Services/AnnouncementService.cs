using System.Diagnostics;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using MauiAppIT13.Models;

namespace MauiAppIT13.Services;

public class AnnouncementService
{
    private readonly string _connectionString;

    public AnnouncementService(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("EduCrmSql")
            ?? throw new InvalidOperationException("Connection string 'EduCrmSql' not found in configuration.");
    }

    public async Task<List<Announcement>> GetAnnouncementsAsync(int limit = 100, Guid? currentUserId = null)
    {
        var announcements = new List<Announcement>();

        const string sql = @"
            SELECT TOP (@Limit)
                a.announcement_id,
                a.title,
                a.content,
                a.course_id,
                a.author_id,
                a.visibility,
                a.is_published,
                a.created_at,
                a.created_by,
                a.updated_at,
                a.updated_by,
                author.display_name AS author_name,
                creator.display_name AS created_by_name,
                updater.display_name AS updated_by_name,
                COUNT(v.view_id) AS view_count,
                MAX(CASE WHEN @CurrentUserId IS NOT NULL AND v.user_id = @CurrentUserId THEN 1 ELSE 0 END) AS has_viewed
            FROM announcements a
            LEFT JOIN users author ON a.author_id = author.user_id
            LEFT JOIN users creator ON a.created_by = creator.user_id
            LEFT JOIN users updater ON a.updated_by = updater.user_id
            LEFT JOIN announcement_views v ON v.announcement_id = a.announcement_id
            GROUP BY
                a.announcement_id,
                a.title,
                a.content,
                a.course_id,
                a.author_id,
                a.visibility,
                a.is_published,
                a.created_at,
                a.created_by,
                a.updated_at,
                a.updated_by,
                author.display_name,
                creator.display_name,
                updater.display_name
            ORDER BY a.created_at DESC";

        try
        {
            await using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            await using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@Limit", limit);
            command.Parameters.AddWithValue("@CurrentUserId", currentUserId.HasValue ? currentUserId.Value : (object)DBNull.Value);
            command.CommandTimeout = 8;

            await using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var announcement = new Announcement
                {
                    Id = reader.GetGuid(0),
                    Title = reader.GetString(1),
                    Content = reader.GetString(2),
                    CourseId = reader.IsDBNull(3) ? null : reader.GetGuid(3),
                    AuthorId = reader.GetGuid(4),
                    Visibility = reader.GetString(5),
                    IsPublished = reader.GetBoolean(6),
                    CreatedAt = reader.GetDateTime(7),
                    CreatedBy = reader.GetGuid(8),
                    UpdatedAt = reader.IsDBNull(9) ? null : reader.GetDateTime(9),
                    UpdatedBy = reader.IsDBNull(10) ? null : reader.GetGuid(10),
                    AuthorName = reader.IsDBNull(11) ? "Unknown" : reader.GetString(11),
                    CreatedByName = reader.IsDBNull(12) ? "Unknown" : reader.GetString(12),
                    UpdatedByName = reader.IsDBNull(13) ? null : reader.GetString(13),
                    ViewCount = reader.GetInt32(14),
                    HasViewed = !reader.IsDBNull(15) && reader.GetInt32(15) > 0
                };
                announcements.Add(announcement);
            }

            Debug.WriteLine($"AnnouncementService: Loaded {announcements.Count} announcements (limit {limit}) from DB: {_connectionString}");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"AnnouncementService: Failed to load announcements - {ex.Message}");
        }

        return announcements;
    }

    public async Task<List<Announcement>> GetClassAnnouncementsAsync(Guid courseId, int limit = 50)
    {
        var announcements = new List<Announcement>();
        if (courseId == Guid.Empty)
        {
            return announcements;
        }

        const string sql = @"
            SELECT TOP (@Limit)
                a.announcement_id,
                a.title,
                a.content,
                a.course_id,
                a.author_id,
                a.visibility,
                a.is_published,
                a.created_at,
                a.created_by,
                a.updated_at,
                a.updated_by,
                author.display_name AS author_name,
                creator.display_name AS created_by_name,
                updater.display_name AS updated_by_name
            FROM announcements a
            LEFT JOIN users author ON a.author_id = author.user_id
            LEFT JOIN users creator ON a.created_by = creator.user_id
            LEFT JOIN users updater ON a.updated_by = updater.user_id
            WHERE a.course_id = @CourseId
            ORDER BY a.created_at DESC";

        try
        {
            await using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            await using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@Limit", limit);
            command.Parameters.AddWithValue("@CourseId", courseId);
            command.CommandTimeout = 8;

            await using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                announcements.Add(new Announcement
                {
                    Id = reader.GetGuid(0),
                    Title = reader.GetString(1),
                    Content = reader.GetString(2),
                    CourseId = reader.IsDBNull(3) ? null : reader.GetGuid(3),
                    AuthorId = reader.GetGuid(4),
                    Visibility = reader.GetString(5),
                    IsPublished = reader.GetBoolean(6),
                    CreatedAt = reader.GetDateTime(7),
                    CreatedBy = reader.GetGuid(8),
                    UpdatedAt = reader.IsDBNull(9) ? null : reader.GetDateTime(9),
                    UpdatedBy = reader.IsDBNull(10) ? null : reader.GetGuid(10),
                    AuthorName = reader.IsDBNull(11) ? "Unknown" : reader.GetString(11),
                    CreatedByName = reader.IsDBNull(12) ? "Unknown" : reader.GetString(12),
                    UpdatedByName = reader.IsDBNull(13) ? null : reader.GetString(13)
                });
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"AnnouncementService: Failed to load class announcements - {ex.Message}");
        }

        return announcements;
    }

    public async Task<Guid?> CreateAnnouncementAsync(string title, string content, string visibility, bool isPublished, Guid authorId, Guid createdBy, Guid? courseId = null)
    {
        const string sql = @"
            INSERT INTO announcements (announcement_id, title, content, author_id, course_id, visibility, is_published, created_at, created_by)
            VALUES (@Id, @Title, @Content, @AuthorId, @CourseId, @Visibility, @IsPublished, GETUTCDATE(), @CreatedBy)";

        try
        {
            var id = Guid.NewGuid();
            await using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();
            await using var command = new SqlCommand(sql, connection);
            command.CommandTimeout = 8;

            command.Parameters.AddWithValue("@Id", id);
            command.Parameters.AddWithValue("@Title", title);
            command.Parameters.AddWithValue("@Content", content);
            command.Parameters.AddWithValue("@AuthorId", authorId);
            command.Parameters.AddWithValue("@CourseId", courseId.HasValue ? courseId.Value : (object)DBNull.Value);
            command.Parameters.AddWithValue("@Visibility", visibility);
            command.Parameters.AddWithValue("@IsPublished", isPublished);
            command.Parameters.AddWithValue("@CreatedBy", createdBy);

            var rows = await command.ExecuteNonQueryAsync();
            return rows > 0 ? id : null;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"AnnouncementService: Failed to create announcement - {ex.Message}");
            return null;
        }
    }

    public async Task<bool> UpdateAnnouncementAsync(Guid id, string title, string content, string visibility, bool isPublished, Guid updatedBy)
    {
        const string sql = @"
            UPDATE announcements
            SET title = @Title,
                content = @Content,
                visibility = @Visibility,
                is_published = @IsPublished,
                updated_at = GETUTCDATE(),
                updated_by = @UpdatedBy
            WHERE announcement_id = @Id";

        try
        {
            await using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();
            await using var command = new SqlCommand(sql, connection);
            command.CommandTimeout = 8;

            command.Parameters.AddWithValue("@Id", id);
            command.Parameters.AddWithValue("@Title", title);
            command.Parameters.AddWithValue("@Content", content);
            command.Parameters.AddWithValue("@Visibility", visibility);
            command.Parameters.AddWithValue("@IsPublished", isPublished);
            command.Parameters.AddWithValue("@UpdatedBy", updatedBy);

            var rows = await command.ExecuteNonQueryAsync();
            return rows > 0;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"AnnouncementService: Failed to update announcement - {ex.Message}");
            return false;
        }
    }

    public async Task<bool> DeleteAnnouncementAsync(Guid id)
    {
        const string deleteViews = "DELETE FROM announcement_views WHERE announcement_id = @Id";
        const string deleteAnnouncement = "DELETE FROM announcements WHERE announcement_id = @Id";

        try
        {
            await using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            await using var transaction = await connection.BeginTransactionAsync();
            try
            {
                await using (var deleteViewsCommand = new SqlCommand(deleteViews, connection, (SqlTransaction)transaction))
                {
                    deleteViewsCommand.Parameters.AddWithValue("@Id", id);
                    await deleteViewsCommand.ExecuteNonQueryAsync();
                }

                await using (var deleteAnnouncementCommand = new SqlCommand(deleteAnnouncement, connection, (SqlTransaction)transaction))
                {
                    deleteAnnouncementCommand.Parameters.AddWithValue("@Id", id);
                    var rows = await deleteAnnouncementCommand.ExecuteNonQueryAsync();
                    await transaction.CommitAsync();
                    return rows > 0;
                }
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"AnnouncementService: Failed to delete announcement - {ex.Message}");
            return false;
        }
    }

    public async Task<bool> RecordViewAsync(Guid announcementId, Guid userId)
    {
        const string sql = @"
            IF NOT EXISTS (
                SELECT 1
                FROM announcement_views
                WHERE announcement_id = @AnnouncementId AND user_id = @UserId)
            BEGIN
                INSERT INTO announcement_views (view_id, announcement_id, user_id, viewed_at)
                VALUES (NEWID(), @AnnouncementId, @UserId, GETUTCDATE())
            END";

        try
        {
            await using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();
            await using var command = new SqlCommand(sql, connection);
            command.CommandTimeout = 5;

            command.Parameters.AddWithValue("@AnnouncementId", announcementId);
            command.Parameters.AddWithValue("@UserId", userId);

            var rows = await command.ExecuteNonQueryAsync();
            return rows > 0;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"AnnouncementService: Failed to record view - {ex.Message}");
            return false;
        }
    }
}
