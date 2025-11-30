namespace MauiAppIT13.Models;

public class Ticket
{
    public Guid Id { get; set; }
    public string TicketNumber { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Status { get; set; } = "open"; // open, in_progress, resolved
    public string Priority { get; set; } = "medium"; // low, medium, high
    public DateTime CreatedAt { get; set; }
    public Guid CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public Guid? UpdatedBy { get; set; }
    public Guid StudentId { get; set; }
    public Guid? AssignedToId { get; set; }
    
    // Display properties
    public string CreatedByName { get; set; } = string.Empty;
    public string AssignedToName { get; set; } = string.Empty;
    public string StatusColor { get; set; } = "#6B7280";
    public string PriorityColor { get; set; } = "#FCD34D";
}
