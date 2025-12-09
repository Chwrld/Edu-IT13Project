using System;

namespace MauiAppIT13.Models;

public class AssignmentSubmission
{
    public Guid SubmissionId { get; set; }
    public Guid AssignmentId { get; set; }
    public Guid StudentId { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public string StudentNumber { get; set; } = string.Empty;
    public DateTime SubmittedAt { get; set; }
    public int? Score { get; set; }
    public string Status { get; set; } = "submitted";
    public string? Notes { get; set; }

    // Display properties
    public string SubmittedAtDisplay => SubmittedAt.ToLocalTime().ToString("MMM dd, yyyy h:mm tt");
    public string ScoreDisplay => Score.HasValue ? $"{Score}/{MaxScore}" : "Not graded";
    public string StatusBadgeColor => Status.ToLower() switch
    {
        "submitted" => "#3B82F6",
        "graded" => "#10B981",
        "late" => "#F59E0B",
        _ => "#6B7280"
    };
    
    // For display purposes
    public int MaxScore { get; set; } = 100;
}
