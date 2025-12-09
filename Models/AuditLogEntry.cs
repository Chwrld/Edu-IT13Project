namespace MauiAppIT13.Models;

public sealed class AuditLogEntry
{
    public Guid Id { get; set; }
    public string TableName { get; set; } = string.Empty;
    public string RecordPrimaryKey { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public Guid? ChangedBy { get; set; }
    public DateTime ChangedAt { get; set; }
    public string? NewDataJson { get; set; }
    public string? OldDataJson { get; set; }

    public bool HasNewData => !string.IsNullOrWhiteSpace(NewDataJson);
    public bool HasOldData => !string.IsNullOrWhiteSpace(OldDataJson);
}
