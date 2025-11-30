namespace MauiAppIT13.Models;

public class TicketComment
{
    public Guid Id { get; set; }
    public Guid TicketId { get; set; }
    public Guid UserId { get; set; }
    public string Content { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    
    // Display properties
    public string UserName { get; set; } = string.Empty;
    public string UserRole { get; set; } = string.Empty;
}
