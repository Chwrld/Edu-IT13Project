namespace MauiAppIT13.Models;

public class Student
{
    public Guid StudentId { get; set; }
    public string StudentNumber { get; set; } = string.Empty;
    public string? Program { get; set; }
    public string? YearLevel { get; set; }
    public decimal? GPA { get; set; }
    public string Status { get; set; } = "active";
    public Guid? AdviserID { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
