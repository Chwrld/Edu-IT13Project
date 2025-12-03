namespace MauiAppIT13.Models;

public class Announcement
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public Guid AuthorId { get; set; }
    public string Visibility { get; set; } = "all"; // all, students, advisers
    public bool IsPublished { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public Guid CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public Guid? UpdatedBy { get; set; }

    // Display helpers
    public string? AuthorName { get; set; }
    public string? CreatedByName { get; set; }
    public string? UpdatedByName { get; set; }
    public int ViewCount { get; set; }

    public string VisibilityLabel => Visibility switch
    {
        "students" => "Students",
        "advisers" => "Advisers",
        _ => "All Users"
    };

    public string PublishedStatus => IsPublished ? "Published" : "Draft";
}

public class AnnouncementView
{
    public Guid Id { get; set; }
    public Guid AnnouncementId { get; set; }
    public Guid UserId { get; set; }
    public DateTime ViewedAt { get; set; }
}
