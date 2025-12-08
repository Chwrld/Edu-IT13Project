namespace MauiAppIT13.Models;

public sealed class ClassModel
{
    public Guid Id { get; set; }
    public Guid CreatedBy { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Schedule { get; set; } = string.Empty;
    public int Credits { get; set; }
    public int StudentCount { get; set; }
    public string AcademicTerm { get; set; } = "AY 2024-2025 • 1st Sem";
    public string Status { get; set; } = "Active";
    public string StatusColor { get; set; } = "#D1FAE5";
    public string StatusTextColor { get; set; } = "#059669";
    public string ClassKey { get; set; } = string.Empty;

    public string StudentCountDisplay => $"{StudentCount} students";
    public string CreditsDisplay => $"{Credits} credits";
}

public sealed class ClassStudent
{
    public Guid StudentId { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string StudentNumber { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Status { get; set; } = "active";

    public string Initials
    {
        get
        {
            if (string.IsNullOrWhiteSpace(DisplayName))
            {
                return "--";
            }

            var parts = DisplayName.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (parts.Length >= 2)
            {
                return $"{parts[0][0]}{parts[1][0]}".ToUpperInvariant();
            }

            return DisplayName.Substring(0, Math.Min(2, DisplayName.Length)).ToUpperInvariant();
        }
    }
}

public sealed class ClassAssignment
{
    public Guid Id { get; set; }
    public Guid ClassId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime Deadline { get; set; }
    public int TotalPoints { get; set; }
    public int SubmittedCount { get; set; }
    public int TotalStudents { get; set; }

    public string DeadlineDisplay => Deadline.ToLocalTime().ToString("MMM dd, yyyy • hh:mm tt");
    public string SubmissionSummary => $"📝 {SubmittedCount}/{TotalStudents} Submitted";
    public string DeadlineBadge => $"📅 Due {Deadline.ToLocalTime():MMM dd}";
}

public sealed class StudentGradeSummary
{
    public Guid GradeId { get; set; }
    public Guid StudentId { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public double AssignmentsScore { get; set; }
    public double ActivitiesScore { get; set; }
    public double ExamsScore { get; set; }
    public double ProjectsScore { get; set; }

    public double FinalAverage => Math.Round((AssignmentsScore + ActivitiesScore + ExamsScore + ProjectsScore) / 4.0, 2);

    public string AssignmentsDisplay => $"{AssignmentsScore:0}%";
    public string ActivitiesDisplay => $"{ActivitiesScore:0}%";
    public string ExamsDisplay => $"{ExamsScore:0}%";
    public string ProjectsDisplay => $"{ProjectsScore:0}%";
    public string FinalAverageDisplay => $"{FinalAverage:0}%";
}
