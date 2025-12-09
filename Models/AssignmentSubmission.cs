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
    public bool HasSubmitted { get; set; } = true;

    // Display properties
    public string SubmittedAtDisplay => HasSubmitted && SubmittedAt != DateTime.MinValue 
        ? SubmittedAt.ToLocalTime().ToString("MMM dd, yyyy h:mm tt") 
        : "Not submitted";
    
    public string ScoreDisplay => HasSubmitted && Score.HasValue ? $"{Score}/{MaxScore}" : "-";
    
    public string StatusBadgeColor => Status.ToLower() switch
    {
        "submitted" => "#3B82F6",
        "graded" => "#10B981",
        "late" => "#F59E0B",
        "not submitted" => "#9CA3AF",
        _ => "#6B7280"
    };
    
    public string StatusDisplay => Status switch
    {
        "not submitted" => "Not Submitted",
        "submitted" => "Submitted",
        "graded" => "Graded",
        "late" => "Late",
        _ => Status
    };
    
    // For display purposes
    public int MaxScore { get; set; } = 100;
}
