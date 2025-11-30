using System.Collections.ObjectModel;
using System.Diagnostics;
using Microsoft.Data.SqlClient;
using MauiAppIT13.Database;
using MauiAppIT13.Models;

namespace MauiAppIT13.Services;

public class MessageService
{
    private readonly DbConnection _dbConnection;
    private static readonly ObservableCollection<Conversation> Conversations = new();

    public MessageService(DbConnection dbConnection)
    {
        _dbConnection = dbConnection;
    }

    private void InitializeSampleData()
    {
        if (Conversations.Count > 0)
            return;

        // Sample conversations - fallback if database is empty
        Conversations.Add(new Conversation
        {
            Id = Guid.NewGuid(),
            ParticipantId = Guid.NewGuid(),
            ParticipantName = "Dr. Johnson",
            ParticipantRole = "Academic Advisor",
            LastMessage = "Your thesis draft looks great!...",
            LastMessageTime = DateTime.UtcNow.AddMinutes(-10),
            UnreadCount = 3,
            AvatarColor = "#0891B2",
            Initials = "DJ"
        });

        Conversations.Add(new Conversation
        {
            Id = Guid.NewGuid(),
            ParticipantId = Guid.NewGuid(),
            ParticipantName = "Academic Office",
            ParticipantRole = "Announcement",
            LastMessage = "Reminder: Registration deadli...",
            LastMessageTime = DateTime.UtcNow.AddHours(-2),
            UnreadCount = 1,
            AvatarColor = "#FEF3C7",
            Initials = "AO"
        });

        Conversations.Add(new Conversation
        {
            Id = Guid.NewGuid(),
            ParticipantId = Guid.NewGuid(),
            ParticipantName = "Prof. Martinez",
            ParticipantRole = "Course Instructor",
            LastMessage = "Thanks for your presentation...",
            LastMessageTime = DateTime.UtcNow.AddDays(-1),
            UnreadCount = 0,
            AvatarColor = "#E0E7FF",
            Initials = "PM"
        });

        Conversations.Add(new Conversation
        {
            Id = Guid.NewGuid(),
            ParticipantId = Guid.NewGuid(),
            ParticipantName = "Student Services",
            ParticipantRole = "Support",
            LastMessage = "Your request has been processed...",
            LastMessageTime = DateTime.UtcNow.AddDays(-2),
            UnreadCount = 0,
            AvatarColor = "#DBEAFE",
            Initials = "SS"
        });
    }

    public async Task<ObservableCollection<Conversation>> GetConversationsAsync(Guid userId)
    {
        try
        {
            Debug.WriteLine($"MessageService: Loading conversations for user {userId}");
            var conversations = new ObservableCollection<Conversation>();
            
            // Simple query to test if we can fetch any conversations
            const string sql = @"
                SELECT TOP 10
                    c.conversation_id,
                    c.participant1_id,
                    c.participant2_id,
                    u.display_name,
                    u.role,
                    ISNULL(m.content, 'No messages yet') as last_message,
                    ISNULL(c.last_message_time, c.created_at) as last_message_time
                FROM conversations c
                LEFT JOIN users u ON (CASE WHEN c.participant1_id = @UserId THEN c.participant2_id ELSE c.participant1_id END) = u.user_id
                LEFT JOIN messages m ON c.last_message_id = m.message_id
                WHERE c.participant1_id = @UserId OR c.participant2_id = @UserId";

            Debug.WriteLine($"MessageService: Executing SQL query...");
            
            await using var connection = new SqlConnection("Data Source=LAPTOP-L1R9L9R3\\SQLEXPRESS01;Initial Catalog=EduCRM;Integrated Security=True;Connect Timeout=10;Encrypt=False;Trust Server Certificate=True;");
            await connection.OpenAsync();
            Debug.WriteLine("MessageService: Connection opened");
            
            await using var command = new SqlCommand(sql, connection);
            command.CommandTimeout = 10;
            command.Parameters.AddWithValue("@UserId", userId);

            await using var reader = await command.ExecuteReaderAsync();
            Debug.WriteLine("MessageService: Query executed, reading results...");
            
            int rowCount = 0;
            while (await reader.ReadAsync())
            {
                rowCount++;
                var participantId = reader.GetGuid(2); // participant2_id if participant1 is current user
                var participantName = reader.IsDBNull(3) ? "Unknown" : reader.GetString(3);
                
                var conversation = new Conversation
                {
                    Id = reader.GetGuid(0),
                    ParticipantId = participantId,
                    ParticipantName = participantName,
                    ParticipantRole = reader.IsDBNull(4) ? "User" : reader.GetString(4),
                    LastMessage = reader.GetString(5),
                    LastMessageTime = reader.GetDateTime(6),
                    UnreadCount = 0,
                    Initials = GetInitials(participantName),
                    AvatarColor = GetAvatarColor(participantId)
                };
                conversations.Add(conversation);
                Debug.WriteLine($"MessageService: Added conversation {rowCount}: {participantName}");
            }
            
            Debug.WriteLine($"MessageService: Total conversations loaded: {conversations.Count}");
            return conversations;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"MessageService: ERROR - {ex.GetType().Name}: {ex.Message}");
            Debug.WriteLine($"MessageService: Stack trace: {ex.StackTrace}");
            return new ObservableCollection<Conversation>();
        }
    }

    public async Task<List<Message>> GetConversationMessagesAsync(Guid conversationId)
    {
        try
        {
            Debug.WriteLine($"MessageService: Loading messages for conversation {conversationId}");
            var messages = new List<Message>();
            
            const string sql = @"
                SELECT 
                    m.message_id,
                    m.sender_id,
                    m.receiver_id,
                    m.content,
                    m.is_read,
                    m.created_at,
                    u.display_name,
                    u.role
                FROM messages m
                INNER JOIN conversations c ON 
                    (m.sender_id = c.participant1_id OR m.sender_id = c.participant2_id) AND
                    (m.receiver_id = c.participant1_id OR m.receiver_id = c.participant2_id)
                INNER JOIN users u ON m.sender_id = u.user_id
                WHERE c.conversation_id = @ConversationId
                ORDER BY m.created_at ASC";

            const string connectionString = "Data Source=LAPTOP-L1R9L9R3\\SQLEXPRESS01;Initial Catalog=EduCRM;Integrated Security=True;Connect Timeout=10;Encrypt=False;Trust Server Certificate=True;";
            await using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();
            await using var command = new SqlCommand(sql, connection);
            command.CommandTimeout = 5;
            command.Parameters.AddWithValue("@ConversationId", conversationId);

            await using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var senderId = reader.GetGuid(1);
                var message = new Message
                {
                    Id = reader.GetGuid(0),
                    SenderId = senderId,
                    ReceiverId = reader.GetGuid(2),
                    Content = reader.GetString(3),
                    IsRead = reader.GetBoolean(4),
                    CreatedAtUtc = reader.GetDateTime(5),
                    SenderName = reader.GetString(6),
                    SenderRole = reader.GetString(7),
                    SenderInitials = GetInitials(reader.GetString(6)),
                    AvatarColor = GetAvatarColor(senderId)
                };
                messages.Add(message);
                Debug.WriteLine($"MessageService: Loaded message: {message.Content}");
            }

            Debug.WriteLine($"MessageService: Total messages loaded: {messages.Count}");
            return messages;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"MessageService: Error loading messages - {ex.GetType().Name}: {ex.Message}");
            Debug.WriteLine($"MessageService: Stack trace: {ex.StackTrace}");
            return new List<Message>();
        }
    }

    public async Task<bool> SendMessageAsync(Guid senderId, Guid receiverId, string content)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(content))
                return false;

            Debug.WriteLine($"MessageService: Sending message from {senderId} to {receiverId}");
            
            const string connectionString = "Data Source=LAPTOP-L1R9L9R3\\SQLEXPRESS01;Initial Catalog=EduCRM;Integrated Security=True;Connect Timeout=10;Encrypt=False;Trust Server Certificate=True;";
            
            const string sql = @"
                INSERT INTO messages (message_id, sender_id, receiver_id, content, is_read, created_at)
                VALUES (@MessageId, @SenderId, @ReceiverId, @Content, 0, GETUTCDATE());
                
                UPDATE conversations 
                SET last_message_id = @MessageId, last_message_time = GETUTCDATE()
                WHERE (participant1_id = @SenderId AND participant2_id = @ReceiverId) 
                   OR (participant1_id = @ReceiverId AND participant2_id = @SenderId);";

            var messageId = Guid.NewGuid();
            
            await using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();
            await using var command = new SqlCommand(sql, connection);
            command.CommandTimeout = 5;
            
            command.Parameters.AddWithValue("@MessageId", messageId);
            command.Parameters.AddWithValue("@SenderId", senderId);
            command.Parameters.AddWithValue("@ReceiverId", receiverId);
            command.Parameters.AddWithValue("@Content", content);

            await command.ExecuteNonQueryAsync();
            
            Debug.WriteLine($"MessageService: Message sent successfully");
            return true;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"MessageService: Error sending message - {ex.GetType().Name}: {ex.Message}");
            Debug.WriteLine($"MessageService: Stack trace: {ex.StackTrace}");
            return false;
        }
    }

    private static string GetInitials(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return "?";
        
        var parts = name.Split(' ');
        if (parts.Length >= 2)
            return $"{parts[0][0]}{parts[1][0]}".ToUpper();
        
        return name.Substring(0, Math.Min(2, name.Length)).ToUpper();
    }

    private static string GetAvatarColor(Guid id)
    {
        var colors = new[] { "#0891B2", "#FEF3C7", "#E0E7FF", "#DBEAFE", "#10B981", "#F87171" };
        return colors[id.GetHashCode() % colors.Length];
    }
}
