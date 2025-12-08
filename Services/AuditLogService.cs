using System.Diagnostics;
using Microsoft.Data.SqlClient;
using MauiAppIT13.Database;
using MauiAppIT13.Models;

namespace MauiAppIT13.Services;

public sealed class AuditLogService
{
    private readonly DbConnection _dbConnection;

    public AuditLogService(DbConnection dbConnection)
    {
        _dbConnection = dbConnection;
    }

    public async Task<IReadOnlyList<AuditLogEntry>> GetAuditLogsAsync(
        string? tableName = null,
        Guid? recordId = null,
        int limit = 100)
    {
        var logs = new List<AuditLogEntry>();

        if (_dbConnection is not SqlServerDbConnection sqlConnection)
        {
            Debug.WriteLine("AuditLogService: DbConnection is not SqlServerDbConnection");
            return logs;
        }

        const string baseSql = @"
SELECT TOP (@Limit)
    audit_id,
    table_name,
    record_primary_key,
    action,
    changed_by,
    changed_at,
    new_data,
    old_data
FROM audit_logs
WHERE (@TableName IS NULL OR table_name = @TableName)
  AND (@RecordId IS NULL OR record_primary_key = @RecordPrimaryKey)
ORDER BY changed_at DESC";

        await using var connection = sqlConnection.GetConnection() as SqlConnection;
        if (connection is null)
        {
            Debug.WriteLine("AuditLogService: Unable to create SQL connection");
            return logs;
        }

        await connection.OpenAsync();
        await using var command = new SqlCommand(baseSql, connection);
        command.CommandTimeout = 10;
        command.Parameters.AddWithValue("@Limit", limit);
        command.Parameters.AddWithValue("@TableName", (object?)tableName ?? DBNull.Value);
        command.Parameters.AddWithValue("@RecordId", recordId.HasValue ? (object)recordId.Value : DBNull.Value);
        command.Parameters.AddWithValue("@RecordPrimaryKey", recordId.HasValue ? recordId.Value.ToString() : (object)DBNull.Value);

        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            logs.Add(new AuditLogEntry
            {
                Id = reader.GetGuid(0),
                TableName = reader.GetString(1),
                RecordPrimaryKey = reader.GetString(2),
                Action = reader.GetString(3),
                ChangedBy = reader.IsDBNull(4) ? null : reader.GetGuid(4),
                ChangedAt = reader.GetDateTime(5),
                NewDataJson = reader.IsDBNull(6) ? null : reader.GetString(6),
                OldDataJson = reader.IsDBNull(7) ? null : reader.GetString(7)
            });
        }

        return logs;
    }
}
