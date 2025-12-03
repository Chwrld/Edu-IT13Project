using System.Collections.ObjectModel;
using System.Diagnostics;
using Microsoft.Data.SqlClient;
using MauiAppIT13.Database;
using MauiAppIT13.Models;

namespace MauiAppIT13.Services;

public class TicketService
{
    private readonly DbConnection _dbConnection;

    public TicketService(DbConnection dbConnection)
    {
        _dbConnection = dbConnection;
    }

    public async Task<ObservableCollection<Ticket>> GetStudentTicketsAsync(Guid studentId)
    {
        try
        {
            Debug.WriteLine($"TicketService: Loading tickets for student {studentId}");
            var tickets = new ObservableCollection<Ticket>();

            const string sql = @"
                SELECT TOP 50
                    t.ticket_id,
                    t.ticket_number,
                    t.title,
                    t.description,
                    t.status,
                    t.priority,
                    t.created_at,
                    t.created_by,
                    t.updated_at,
                    t.updated_by,
                    t.student_id,
                    t.assigned_to_id,
                    u_creator.display_name as created_by_name,
                    u_assigned.display_name as assigned_to_name
                FROM support_tickets t
                LEFT JOIN users u_creator ON t.created_by = u_creator.user_id
                LEFT JOIN users u_assigned ON t.assigned_to_id = u_assigned.user_id
                WHERE t.student_id = @StudentId
                ORDER BY t.created_at DESC";

            const string connectionString = "Data Source=LAPTOP-L1R9L9R3\\SQLEXPRESS01;Initial Catalog=EduCRM;Integrated Security=True;Connect Timeout=10;Encrypt=False;Trust Server Certificate=True;";
            
            await using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();
            
            await using var command = new SqlCommand(sql, connection);
            command.CommandTimeout = 10;
            command.Parameters.AddWithValue("@StudentId", studentId);

            await using var reader = await command.ExecuteReaderAsync();
            
            while (await reader.ReadAsync())
            {
                var ticket = new Ticket
                {
                    Id = reader.GetGuid(0),
                    TicketNumber = reader.GetString(1),
                    Title = reader.GetString(2),
                    Description = reader.GetString(3),
                    Status = reader.GetString(4),
                    Priority = reader.GetString(5),
                    CreatedAt = reader.GetDateTime(6),
                    CreatedBy = reader.GetGuid(7),
                    UpdatedAt = reader.IsDBNull(8) ? null : reader.GetDateTime(8),
                    UpdatedBy = reader.IsDBNull(9) ? null : reader.GetGuid(9),
                    StudentId = reader.GetGuid(10),
                    AssignedToId = reader.IsDBNull(11) ? null : reader.GetGuid(11),
                    CreatedByName = reader.IsDBNull(12) ? "Unknown" : reader.GetString(12),
                    AssignedToName = reader.IsDBNull(13) ? "Unassigned" : reader.GetString(13),
                    StatusColor = GetStatusColor(reader.GetString(4)),
                    PriorityColor = GetPriorityColor(reader.GetString(5))
                };
                tickets.Add(ticket);
                Debug.WriteLine($"TicketService: Loaded ticket {ticket.TicketNumber}: {ticket.Title}");
            }

            Debug.WriteLine($"TicketService: Total tickets loaded: {tickets.Count}");
            return tickets;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"TicketService: Error loading tickets - {ex.GetType().Name}: {ex.Message}");
            Debug.WriteLine($"TicketService: Stack trace: {ex.StackTrace}");
            return new ObservableCollection<Ticket>();
        }
    }

    public async Task<ObservableCollection<Ticket>> GetAllTicketsAsync(int limit = 100)
    {
        try
        {
            Debug.WriteLine("TicketService: Loading tickets for admin view");
            var tickets = new ObservableCollection<Ticket>();

            string sql = $@"SELECT TOP {limit} " +
                @"
                    t.ticket_id,
                    t.ticket_number,
                    t.title,
                    t.description,
                    t.status,
                    t.priority,
                    t.created_at,
                    t.created_by,
                    t.updated_at,
                    t.updated_by,
                    t.student_id,
                    t.assigned_to_id,
                    u_student.display_name as student_name,
                    u_assigned.display_name as assigned_to_name
                FROM support_tickets t
                LEFT JOIN users u_student ON t.student_id = u_student.user_id
                LEFT JOIN users u_assigned ON t.assigned_to_id = u_assigned.user_id
                ORDER BY t.created_at DESC";

            const string connectionString = "Data Source=LAPTOP-L1R9L9R3\\SQLEXPRESS01;Initial Catalog=EduCRM;Integrated Security=True;Connect Timeout=10;Encrypt=False;Trust Server Certificate=True;";

            await using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();

            await using var command = new SqlCommand(sql, connection);
            command.CommandTimeout = 10;

            await using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                var ticket = new Ticket
                {
                    Id = reader.GetGuid(0),
                    TicketNumber = reader.GetString(1),
                    Title = reader.GetString(2),
                    Description = reader.GetString(3),
                    Status = reader.GetString(4),
                    Priority = reader.GetString(5),
                    CreatedAt = reader.GetDateTime(6),
                    CreatedBy = reader.GetGuid(7),
                    UpdatedAt = reader.IsDBNull(8) ? null : reader.GetDateTime(8),
                    UpdatedBy = reader.IsDBNull(9) ? null : reader.GetGuid(9),
                    StudentId = reader.GetGuid(10),
                    AssignedToId = reader.IsDBNull(11) ? null : reader.GetGuid(11),
                    CreatedByName = reader.IsDBNull(12) ? "Unknown" : reader.GetString(12),
                    AssignedToName = reader.IsDBNull(13) ? "Unassigned" : reader.GetString(13),
                    StatusColor = GetStatusColor(reader.GetString(4)),
                    PriorityColor = GetPriorityColor(reader.GetString(5))
                };

                tickets.Add(ticket);
            }

            Debug.WriteLine($"TicketService: Total admin tickets loaded: {tickets.Count}");
            return tickets;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"TicketService: Error loading admin tickets - {ex.GetType().Name}: {ex.Message}");
            return new ObservableCollection<Ticket>();
        }
    }

    public async Task<List<TicketComment>> GetTicketCommentsAsync(Guid ticketId)
    {
        try
        {
            Debug.WriteLine($"TicketService: Loading comments for ticket {ticketId}");
            var comments = new List<TicketComment>();

            const string sql = @"
                SELECT 
                    c.comment_id,
                    c.ticket_id,
                    c.user_id,
                    c.content,
                    c.created_at,
                    u.display_name,
                    u.role
                FROM ticket_comments c
                INNER JOIN users u ON c.user_id = u.user_id
                WHERE c.ticket_id = @TicketId
                ORDER BY c.created_at ASC";

            const string connectionString = "Data Source=LAPTOP-L1R9L9R3\\SQLEXPRESS01;Initial Catalog=EduCRM;Integrated Security=True;Connect Timeout=10;Encrypt=False;Trust Server Certificate=True;";
            
            await using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();
            
            await using var command = new SqlCommand(sql, connection);
            command.CommandTimeout = 5;
            command.Parameters.AddWithValue("@TicketId", ticketId);

            await using var reader = await command.ExecuteReaderAsync();
            
            while (await reader.ReadAsync())
            {
                var comment = new TicketComment
                {
                    Id = reader.GetGuid(0),
                    TicketId = reader.GetGuid(1),
                    UserId = reader.GetGuid(2),
                    Content = reader.GetString(3),
                    CreatedAt = reader.GetDateTime(4),
                    UserName = reader.GetString(5),
                    UserRole = reader.GetString(6)
                };
                comments.Add(comment);
                Debug.WriteLine($"TicketService: Loaded comment: {comment.Content.Substring(0, Math.Min(50, comment.Content.Length))}...");
            }

            Debug.WriteLine($"TicketService: Total comments loaded: {comments.Count}");
            return comments;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"TicketService: Error loading comments - {ex.GetType().Name}: {ex.Message}");
            return new List<TicketComment>();
        }
    }

    public async Task<bool> CreateTicketAsync(Guid studentId, string title, string description, string priority)
    {
        try
        {
            Debug.WriteLine($"TicketService: Creating ticket for student {studentId}");
            
            const string connectionString = "Data Source=LAPTOP-L1R9L9R3\\SQLEXPRESS01;Initial Catalog=EduCRM;Integrated Security=True;Connect Timeout=10;Encrypt=False;Trust Server Certificate=True;";
            
            const string sql = @"
                INSERT INTO support_tickets (ticket_id, ticket_number, title, description, status, priority, created_at, created_by, student_id)
                VALUES (@TicketId, @TicketNumber, @Title, @Description, 'open', @Priority, GETUTCDATE(), @CreatedBy, @StudentId)";

            var ticketId = Guid.NewGuid();
            var ticketNumber = $"TKT-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString().Substring(0, 8).ToUpper()}";
            
            await using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();
            
            await using var command = new SqlCommand(sql, connection);
            command.CommandTimeout = 5;
            
            command.Parameters.AddWithValue("@TicketId", ticketId);
            command.Parameters.AddWithValue("@TicketNumber", ticketNumber);
            command.Parameters.AddWithValue("@Title", title);
            command.Parameters.AddWithValue("@Description", description);
            command.Parameters.AddWithValue("@Priority", priority);
            command.Parameters.AddWithValue("@CreatedBy", studentId);
            command.Parameters.AddWithValue("@StudentId", studentId);

            await command.ExecuteNonQueryAsync();
            
            Debug.WriteLine($"TicketService: Ticket created successfully: {ticketNumber}");
            return true;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"TicketService: Error creating ticket - {ex.GetType().Name}: {ex.Message}");
            Debug.WriteLine($"TicketService: Stack trace: {ex.StackTrace}");
            return false;
        }
    }

    public async Task<bool> AddCommentAsync(Guid ticketId, Guid userId, string content)
    {
        try
        {
            Debug.WriteLine($"TicketService: Adding comment to ticket {ticketId}");
            
            const string connectionString = "Data Source=LAPTOP-L1R9L9R3\\SQLEXPRESS01;Initial Catalog=EduCRM;Integrated Security=True;Connect Timeout=10;Encrypt=False;Trust Server Certificate=True;";
            
            const string sql = @"
                INSERT INTO ticket_comments (comment_id, ticket_id, user_id, content, created_at)
                VALUES (@CommentId, @TicketId, @UserId, @Content, GETUTCDATE())";

            var commentId = Guid.NewGuid();
            
            await using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();
            
            await using var command = new SqlCommand(sql, connection);
            command.CommandTimeout = 5;
            
            command.Parameters.AddWithValue("@CommentId", commentId);
            command.Parameters.AddWithValue("@TicketId", ticketId);
            command.Parameters.AddWithValue("@UserId", userId);
            command.Parameters.AddWithValue("@Content", content);

            await command.ExecuteNonQueryAsync();
            
            Debug.WriteLine($"TicketService: Comment added successfully");
            return true;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"TicketService: Error adding comment - {ex.GetType().Name}: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> UpdateTicketStatusAsync(Guid ticketId, string status, Guid? updatedBy)
    {
        try
        {
            Debug.WriteLine($"TicketService: Updating ticket {ticketId} to status {status}");

            const string connectionString = "Data Source=LAPTOP-L1R9L9R3\\SQLEXPRESS01;Initial Catalog=EduCRM;Integrated Security=True;Connect Timeout=10;Encrypt=False;Trust Server Certificate=True;";

            const string sql = @"
                UPDATE support_tickets
                SET status = @Status,
                    updated_at = GETUTCDATE(),
                    updated_by = @UpdatedBy
                WHERE ticket_id = @TicketId";

            await using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();

            await using var command = new SqlCommand(sql, connection);
            command.CommandTimeout = 5;

            command.Parameters.AddWithValue("@TicketId", ticketId);
            command.Parameters.AddWithValue("@Status", status);
            command.Parameters.AddWithValue("@UpdatedBy", updatedBy.HasValue ? updatedBy.Value : (object)DBNull.Value);

            var rows = await command.ExecuteNonQueryAsync();
            Debug.WriteLine(rows > 0
                ? "TicketService: Status updated successfully"
                : "TicketService: Status update affected 0 rows");
            return rows > 0;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"TicketService: Error updating status - {ex.GetType().Name}: {ex.Message}");
            return false;
        }
    }

    private static string GetStatusColor(string status)
    {
        return status switch
        {
            "open" => "#EF4444",
            "in_progress" => "#F59E0B",
            "resolved" => "#10B981",
            _ => "#6B7280"
        };
    }

    private static string GetPriorityColor(string priority)
    {
        return priority switch
        {
            "low" => "#10B981",
            "medium" => "#F59E0B",
            "high" => "#EF4444",
            _ => "#6B7280"
        };
    }
}
