namespace MauiAppIT13.Models;

public class Message
{
    public Guid Id { get; set; }
    public Guid SenderId { get; set; }
    public Guid ReceiverId { get; set; }
    public string Content { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
    public bool IsRead { get; set; }
    public DateTime CreatedAtLocal => DateTimeHelper.ToLocal(CreatedAtUtc);
    
    // For display purposes
    public string SenderName { get; set; } = string.Empty;
    public string SenderInitials { get; set; } = string.Empty;
    public string SenderRole { get; set; } = string.Empty;
    public string AvatarColor { get; set; } = "#0891B2";
    public string TimeAgo { get; set; } = string.Empty;
    public int UnreadCount { get; set; }
}

public class Conversation
{
    public Guid Id { get; set; }
    public Guid ParticipantId { get; set; }
    public string ParticipantName { get; set; } = string.Empty;
    public string ParticipantRole { get; set; } = string.Empty;
    public string LastMessage { get; set; } = string.Empty;
    public DateTime LastMessageTime { get; set; }
    public DateTime LastMessageLocalTime => DateTimeHelper.ToLocal(LastMessageTime);
    public int UnreadCount { get; set; }
    public string AvatarColor { get; set; } = "#0891B2";
    public string Initials { get; set; } = string.Empty;
}

internal static class DateTimeHelper
{
    public static DateTime ToLocal(DateTime value)
    {
        return value.Kind switch
        {
            DateTimeKind.Local => value,
            DateTimeKind.Utc => value.ToLocalTime(),
            _ => DateTime.SpecifyKind(value, DateTimeKind.Utc).ToLocalTime()
        };
    }
}
