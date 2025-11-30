namespace MauiAppIT13.Models;

public class StudentAchievement
{
    public Guid AchievementId { get; set; }
    public Guid StudentId { get; set; }
    public string AchievementName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid? AwardedBy { get; set; }
    public DateTime AwardedDate { get; set; } = DateTime.UtcNow;
}
