namespace MauiAppIT13.Models;

public class User
{
    public const string StatusActive = "active";
    public const string StatusInactive = "inactive";
    public const string StatusArchived = "archived";

    private string _status = StatusActive;

    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string PasswordSalt { get; set; } = string.Empty;
    public Role Role { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string? Address { get; set; }
    public string Status
    {
        get => _status;
        set => _status = NormalizeStatus(value);
    }
    public string? ArchiveReason { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public Guid? CreatedBy { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
    public Guid? UpdatedBy { get; set; }

    public static string NormalizeStatus(string? status)
    {
        return string.IsNullOrWhiteSpace(status)
            ? StatusActive
            : status.Trim().ToLowerInvariant();
    }
}
