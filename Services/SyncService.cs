using System.Data;
using System.Diagnostics;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace MauiAppIT13.Services;

/// <summary>
/// Simple sync service for online/offline functionality.
/// Syncs data between local (offline) and remote (online) databases.
/// When offline: writes to local DB
/// When online: syncs local changes to remote DB
/// </summary>
public class SyncService
{
    private readonly IConfiguration _configuration;
    private readonly string _localConnectionString;
    private readonly string _remoteConnectionString;

    public SyncService(IConfiguration configuration)
    {
        _configuration = configuration;
        _localConnectionString = configuration.GetConnectionString("EduCrmSql")
            ?? throw new InvalidOperationException("Connection string 'EduCrmSql' not found");
        _remoteConnectionString = configuration.GetConnectionString("EduCrmRemote")
            ?? throw new InvalidOperationException("Connection string 'EduCrmRemote' not found");
    }

    /// <summary>
    /// Check if remote database is accessible (online status)
    /// </summary>
    public async Task<bool> IsOnlineAsync()
    {
        try
        {
            await using var connection = new SqlConnection(_remoteConnectionString);
            await connection.OpenAsync();
            connection.Close();
            Debug.WriteLine("SyncService: Online - Remote database is accessible");
            return true;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"SyncService: Offline - {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Sync all data from local database to remote database
    /// Copies entire tables (simple approach - no delta sync)
    /// Optimized with parallel processing for speed
    /// </summary>
    public async Task<bool> SyncToRemoteAsync()
    {
        try
        {
            var isOnline = await IsOnlineAsync();
            if (!isOnline)
            {   
                Debug.WriteLine("SyncService: Cannot sync - offline");
                return false;
            }

            Debug.WriteLine("SyncService: Starting sync to remote database");
            var startTime = DateTime.UtcNow;

            // Tables with dependencies (must sync in order)
            var dependentTables = new[]
            {
                "users",
                "advisers",
                "admins",
                "courses",
                "students"
            };

            // Tables without dependencies (can sync in parallel)
            var independentTables = new[]
            {
                "student_courses",
                "class_assignments",
                "assignment_submissions",
                "student_course_grades",
                "student_achievements",
                "announcements",
                "announcement_views",
                "messages",
                "conversations",
                "support_tickets",
                "ticket_comments"
            };

            // Sync dependent tables first (in order)
            foreach (var table in dependentTables)
            {
                try
                {
                    await SyncTableAsync(table);
                    Debug.WriteLine($"SyncService: Successfully synced table '{table}'");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"SyncService: Failed to sync dependent table '{table}' - {ex.Message}");
                    Debug.WriteLine($"SyncService: Stack trace: {ex.StackTrace}");
                    throw;
                }
            }

            // Sync independent tables in parallel for speed
            try
            {
                await Task.WhenAll(independentTables.Select(table => SyncTableAsync(table)));
                Debug.WriteLine("SyncService: All independent tables synced successfully");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"SyncService: Failed to sync independent tables - {ex.Message}");
                Debug.WriteLine($"SyncService: Stack trace: {ex.StackTrace}");
                throw;
            }

            var elapsed = DateTime.UtcNow - startTime;
            Debug.WriteLine($"SyncService: Sync completed successfully in {elapsed.TotalSeconds:F1}s");
            return true;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"SyncService: Sync failed - {ex.Message}");
            Debug.WriteLine($"SyncService: Full exception: {ex}");
            return false;
        }
    }

    /// <summary>
    /// Sync a single table from local to remote
    /// </summary>
    private async Task SyncTableAsync(string tableName)
    {
        try
        {
            // Read all data from local database
            var localData = await ReadTableFromLocalAsync(tableName);
            
            // Clear remote table
            await ClearRemoteTableAsync(tableName);
            
            // Write data to remote database
            await WriteTableToRemoteAsync(tableName, localData);
            
            Debug.WriteLine($"SyncService: Synced table '{tableName}' - {localData.Rows.Count} rows");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"SyncService: Failed to sync table '{tableName}' - {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Read entire table from local database
    /// </summary>
    private async Task<DataTable> ReadTableFromLocalAsync(string tableName)
    {
        var dataTable = new DataTable();
        
        try
        {
            await using var connection = new SqlConnection(_localConnectionString);
            await connection.OpenAsync();
            
            await using var command = new SqlCommand($"SELECT * FROM {tableName}", connection);
            command.CommandTimeout = 30;
            
            await using var reader = await command.ExecuteReaderAsync();
            dataTable.Load(reader);
            
            Debug.WriteLine($"SyncService: Read {dataTable.Rows.Count} rows from local table '{tableName}'");
            return dataTable;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"SyncService: Error reading table '{tableName}' from local - {ex.Message}");
            Debug.WriteLine($"SyncService: Stack trace: {ex.StackTrace}");
            throw;
        }
    }

    /// <summary>
    /// Clear all data from remote table (preserves schema)
    /// </summary>
    private async Task ClearRemoteTableAsync(string tableName)
    {
        try
        {
            await using var connection = new SqlConnection(_remoteConnectionString);
            await connection.OpenAsync();
            
            // Disable all foreign key constraints temporarily (for this specific table)
            await using var disableCommand = new SqlCommand($"ALTER TABLE {tableName} NOCHECK CONSTRAINT ALL", connection);
            disableCommand.CommandTimeout = 30;
            try
            {
                await disableCommand.ExecuteNonQueryAsync();
            }
            catch
            {
                // Table might not have constraints, continue anyway
            }
            
            // Delete all rows
            await using var deleteCommand = new SqlCommand($"DELETE FROM {tableName}", connection);
            deleteCommand.CommandTimeout = 30;
            await deleteCommand.ExecuteNonQueryAsync();
            
            // Re-enable foreign key constraints
            await using var enableCommand = new SqlCommand($"ALTER TABLE {tableName} WITH CHECK CHECK CONSTRAINT ALL", connection);
            enableCommand.CommandTimeout = 30;
            try
            {
                await enableCommand.ExecuteNonQueryAsync();
            }
            catch
            {
                // Table might not have constraints, continue anyway
            }
            
            Debug.WriteLine($"SyncService: Cleared remote table '{tableName}'");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"SyncService: Error clearing remote table '{tableName}' - {ex.Message}");
            Debug.WriteLine($"SyncService: Stack trace: {ex.StackTrace}");
            throw;
        }
    }

    /// <summary>
    /// Write data to remote table using bulk insert (optimized for speed)
    /// </summary>
    private async Task WriteTableToRemoteAsync(string tableName, DataTable dataTable)
    {
        if (dataTable.Rows.Count == 0)
        {
            Debug.WriteLine($"SyncService: Table '{tableName}' is empty, skipping write");
            return;
        }

        try
        {
            await using var connection = new SqlConnection(_remoteConnectionString);
            await connection.OpenAsync();
            
            using var bulkCopy = new SqlBulkCopy(connection)
            {
                DestinationTableName = tableName,
                BatchSize = 5000,  // Larger batches = faster
                BulkCopyTimeout = 300,  // 5 minute timeout for large datasets
                NotifyAfter = 10000  // Progress notification every 10k rows
            };
            
            // Map columns
            foreach (DataColumn column in dataTable.Columns)
            {
                bulkCopy.ColumnMappings.Add(column.ColumnName, column.ColumnName);
            }
            
            await bulkCopy.WriteToServerAsync(dataTable);
            Debug.WriteLine($"SyncService: Successfully wrote {dataTable.Rows.Count} rows to remote table '{tableName}'");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"SyncService: Error writing to remote table '{tableName}' - {ex.Message}");
            Debug.WriteLine($"SyncService: Stack trace: {ex.StackTrace}");
            throw;
        }
    }

    /// <summary>
    /// Get sync status (online/offline)
    /// </summary>
    public async Task<SyncStatus> GetStatusAsync()
    {
        var isOnline = await IsOnlineAsync();
        return new SyncStatus
        {
            IsOnline = isOnline,
            LastSyncTime = DateTime.UtcNow,
            Status = isOnline ? "Online - Ready to sync" : "Offline - Using local database"
        };
    }
}

/// <summary>
/// Sync status information
/// </summary>
public class SyncStatus
{
    public bool IsOnline { get; set; }
    public DateTime LastSyncTime { get; set; }
    public string Status { get; set; } = string.Empty;
}
