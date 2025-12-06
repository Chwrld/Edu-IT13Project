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
