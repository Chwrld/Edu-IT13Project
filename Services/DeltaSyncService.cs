using System.Data;
using System.Diagnostics;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace MauiAppIT13.Services;

/// <summary>
/// Delta sync service for efficient offline/online synchronization.
/// Only syncs records that have been created or modified since last sync.
/// Tracks sync state to enable incremental updates.
/// </summary>
public class DeltaSyncService
{
    private readonly IConfiguration _configuration;
    private readonly string _localConnectionString;
    private readonly string _remoteConnectionString;
    private const string SyncStateTable = "sync_state";
    private const string LastSyncKey = "last_sync_timestamp";

    public DeltaSyncService(IConfiguration configuration)
    {
        _configuration = configuration;
        _localConnectionString = configuration.GetConnectionString("EduCrmSql")
            ?? throw new InvalidOperationException("Connection string 'EduCrmSql' not found");
        _remoteConnectionString = configuration.GetConnectionString("EduCrmRemote")
            ?? throw new InvalidOperationException("Connection string 'EduCrmRemote' not found");
    }

    /// <summary>
    /// Get the timestamp of the last successful sync
    /// </summary>
    public async Task<DateTime?> GetLastSyncTimestampAsync()
    {
        try
        {
            await using var connection = new SqlConnection(_localConnectionString);
            await connection.OpenAsync();

            // Check if sync_state table exists
            var checkTableCommand = new SqlCommand(
                "SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'sync_state'",
                connection);
            var tableExists = await checkTableCommand.ExecuteScalarAsync() != null;

            if (!tableExists)
            {
                Debug.WriteLine("DeltaSyncService: sync_state table doesn't exist, first sync");
                return null;
            }

            var command = new SqlCommand(
                $"SELECT sync_value FROM {SyncStateTable} WHERE sync_key = @key",
                connection);
            command.Parameters.AddWithValue("@key", LastSyncKey);

            var result = await command.ExecuteScalarAsync();
            if (result != null && DateTime.TryParse(result.ToString(), out var lastSync))
            {
                Debug.WriteLine($"DeltaSyncService: Last sync was at {lastSync:O}");
                return lastSync;
            }

            return null;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"DeltaSyncService: Error getting last sync timestamp - {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Update the last sync timestamp after successful sync
    /// </summary>
    private async Task UpdateLastSyncTimestampAsync(DateTime syncTime)
    {
        try
        {
            await using var connection = new SqlConnection(_localConnectionString);
            await connection.OpenAsync();

            // Create sync_state table if it doesn't exist
            var createTableCommand = new SqlCommand(
                $@"IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = '{SyncStateTable}')
                   CREATE TABLE {SyncStateTable} (
                       sync_key NVARCHAR(255) NOT NULL PRIMARY KEY,
                       sync_value NVARCHAR(MAX) NOT NULL,
                       updated_at DATETIME2 NOT NULL DEFAULT GETUTCDATE()
                   )",
                connection);
            await createTableCommand.ExecuteNonQueryAsync();

            // Upsert the last sync timestamp
            var upsertCommand = new SqlCommand(
                $@"MERGE INTO {SyncStateTable} AS target
                   USING (SELECT @key AS sync_key, @value AS sync_value) AS source
                   ON target.sync_key = source.sync_key
                   WHEN MATCHED THEN UPDATE SET sync_value = source.sync_value, updated_at = GETUTCDATE()
                   WHEN NOT MATCHED THEN INSERT (sync_key, sync_value) VALUES (source.sync_key, source.sync_value);",
                connection);
            upsertCommand.Parameters.AddWithValue("@key", LastSyncKey);
            upsertCommand.Parameters.AddWithValue("@value", syncTime.ToString("O"));

            await upsertCommand.ExecuteNonQueryAsync();
            Debug.WriteLine($"DeltaSyncService: Updated last sync timestamp to {syncTime:O}");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"DeltaSyncService: Error updating last sync timestamp - {ex.Message}");
        }
    }

    /// <summary>
    /// Check if remote database is accessible
    /// </summary>
    public async Task<bool> IsOnlineAsync()
    {
        try
        {
            await using var connection = new SqlConnection(_remoteConnectionString);
            await connection.OpenAsync();
            connection.Close();
            Debug.WriteLine("DeltaSyncService: Online - Remote database is accessible");
            return true;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"DeltaSyncService: Offline - {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Sync only changed records from local to remote (delta sync)
    /// Returns count of records synced
    /// </summary>
    public async Task<(bool Success, int RecordsSynced)> DeltaSyncToRemoteAsync()
    {
        try
        {
            var isOnline = await IsOnlineAsync();
            if (!isOnline)
            {
                Debug.WriteLine("DeltaSyncService: Cannot sync - offline");
                return (false, 0);
            }

            Debug.WriteLine("DeltaSyncService: Starting delta sync to remote database");
            var startTime = DateTime.UtcNow;
            var lastSync = await GetLastSyncTimestampAsync();
            var totalRecordsSynced = 0;

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
                    var recordsCount = await DeltaSyncTableAsync(table, lastSync);
                    totalRecordsSynced += recordsCount;
                    Debug.WriteLine($"DeltaSyncService: Successfully synced {recordsCount} records from table '{table}'");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"DeltaSyncService: Failed to sync dependent table '{table}' - {ex.Message}");
                    Debug.WriteLine($"DeltaSyncService: Stack trace: {ex.StackTrace}");
                    throw;
                }
            }

            // Sync independent tables in parallel for speed
            try
            {
                var tasks = independentTables.Select(table => DeltaSyncTableAsync(table, lastSync)).ToList();
                var results = await Task.WhenAll(tasks);
                totalRecordsSynced += results.Sum();
                Debug.WriteLine($"DeltaSyncService: All independent tables synced successfully - {results.Sum()} records");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"DeltaSyncService: Failed to sync independent tables - {ex.Message}");
                Debug.WriteLine($"DeltaSyncService: Stack trace: {ex.StackTrace}");
                throw;
            }

            // Update last sync timestamp
            await UpdateLastSyncTimestampAsync(startTime);

            var elapsed = DateTime.UtcNow - startTime;
            Debug.WriteLine($"DeltaSyncService: Delta sync completed successfully in {elapsed.TotalSeconds:F1}s - {totalRecordsSynced} records synced");
            return (true, totalRecordsSynced);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"DeltaSyncService: Delta sync failed - {ex.Message}");
            Debug.WriteLine($"DeltaSyncService: Full exception: {ex}");
            return (false, 0);
        }
    }

    /// <summary>
    /// Sync only changed records from a single table
    /// Returns count of records synced
    /// </summary>
    private async Task<int> DeltaSyncTableAsync(string tableName, DateTime? lastSync)
    {
        try
        {
            // Quick check: does this table have any changes?
            var hasChanges = await TableHasChangesAsync(tableName, lastSync);
            if (!hasChanges)
            {
                Debug.WriteLine($"DeltaSyncService: No changes in table '{tableName}', skipping");
                return 0;
            }

            // Read only changed data from local database
            var changedData = await ReadChangedRecordsFromLocalAsync(tableName, lastSync);
            
            if (changedData.Rows.Count == 0)
            {
                Debug.WriteLine($"DeltaSyncService: No changes in table '{tableName}', skipping");
                return 0;
            }

            // Upsert data to remote database (insert new, update existing)
            await UpsertTableToRemoteAsync(tableName, changedData);
            
            Debug.WriteLine($"DeltaSyncService: Synced table '{tableName}' - {changedData.Rows.Count} changed records");
            return changedData.Rows.Count;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"DeltaSyncService: Failed to delta sync table '{tableName}' - {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Quick check if a table has any changes since last sync
    /// </summary>
    private async Task<bool> TableHasChangesAsync(string tableName, DateTime? lastSync)
    {
        try
        {
            if (!lastSync.HasValue)
            {
                // First sync - all records are "changes"
                return true;
            }

            await using var connection = new SqlConnection(_localConnectionString);
            await connection.OpenAsync();

            // Check if table has updated_at or created_at column
            var hasUpdatedAt = await TableHasColumnAsync(connection, tableName, "updated_at");
            var hasCreatedAt = await TableHasColumnAsync(connection, tableName, "created_at");

            if (!hasUpdatedAt && !hasCreatedAt)
            {
                // No audit columns, assume all records need syncing
                return true;
            }

            // Quick count query to check if any records changed
            var dateColumn = hasUpdatedAt ? "updated_at" : "created_at";
            var countCommand = new SqlCommand(
                $"SELECT COUNT(*) FROM {tableName} WHERE {dateColumn} > @lastSync",
                connection);
            countCommand.Parameters.AddWithValue("@lastSync", lastSync.Value);
            countCommand.CommandTimeout = 10;

            var count = (int?)await countCommand.ExecuteScalarAsync() ?? 0;
            return count > 0;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"DeltaSyncService: Error checking changes in table '{tableName}' - {ex.Message}");
            // If check fails, assume changes exist to be safe
            return true;
        }
    }

    /// <summary>
    /// Read only records that were created or modified since last sync
    /// </summary>
    private async Task<DataTable> ReadChangedRecordsFromLocalAsync(string tableName, DateTime? lastSync)
    {
        var dataTable = new DataTable();
        
        try
        {
            await using var connection = new SqlConnection(_localConnectionString);
            await connection.OpenAsync();

            // Build query to get only changed records
            string query = $"SELECT * FROM {tableName}";
            
            if (lastSync.HasValue)
            {
                // Check if table has updated_at column
                var hasUpdatedAt = await TableHasColumnAsync(connection, tableName, "updated_at");
                var hasCreatedAt = await TableHasColumnAsync(connection, tableName, "created_at");

                if (hasUpdatedAt || hasCreatedAt)
                {
                    // Get records created or updated since last sync
                    var dateColumn = hasUpdatedAt ? "updated_at" : "created_at";
                    query += $" WHERE {dateColumn} > @lastSync";
                }
            }

            await using var command = new SqlCommand(query, connection);
            command.CommandTimeout = 30;
            
            if (lastSync.HasValue)
            {
                command.Parameters.AddWithValue("@lastSync", lastSync.Value);
            }

            await using var reader = await command.ExecuteReaderAsync();
            dataTable.Load(reader);
            
            Debug.WriteLine($"DeltaSyncService: Read {dataTable.Rows.Count} changed records from local table '{tableName}'");
            return dataTable;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"DeltaSyncService: Error reading changed records from table '{tableName}' - {ex.Message}");
            Debug.WriteLine($"DeltaSyncService: Stack trace: {ex.StackTrace}");
            throw;
        }
    }

    /// <summary>
    /// Check if a table has a specific column
    /// </summary>
    private async Task<bool> TableHasColumnAsync(SqlConnection connection, string tableName, string columnName)
    {
        try
        {
            var command = new SqlCommand(
                $"SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = @table AND COLUMN_NAME = @column",
                connection);
            command.Parameters.AddWithValue("@table", tableName);
            command.Parameters.AddWithValue("@column", columnName);

            var result = await command.ExecuteScalarAsync();
            return result != null;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Upsert (insert or update) records to remote table using fast batch operation
    /// </summary>
    private async Task UpsertTableToRemoteAsync(string tableName, DataTable dataTable)
    {
        if (dataTable.Rows.Count == 0)
        {
            Debug.WriteLine($"DeltaSyncService: Table '{tableName}' has no records to sync");
            return;
        }

        try
        {
            await using var connection = new SqlConnection(_remoteConnectionString);
            await connection.OpenAsync();

            // Get primary key column name
            var primaryKeyColumn = await GetPrimaryKeyColumnAsync(connection, tableName);
            
            if (string.IsNullOrEmpty(primaryKeyColumn))
            {
                Debug.WriteLine($"DeltaSyncService: Could not determine primary key for table '{tableName}'");
                throw new InvalidOperationException($"Cannot determine primary key for table {tableName}");
            }

            // Use SqlBulkCopy for fast insert, then handle updates separately
            // This is much faster than row-by-row operations
            
            // First, create a temporary table with the new data
            var tempTableName = $"#{tableName}_temp_{Guid.NewGuid():N}";
            await CreateTempTableAsync(connection, tableName, tempTableName, dataTable);
            
            try
            {
                // Bulk insert into temp table (very fast)
                using (var bulkCopy = new SqlBulkCopy(connection))
                {
                    bulkCopy.DestinationTableName = tempTableName;
                    bulkCopy.BatchSize = 5000;
                    bulkCopy.BulkCopyTimeout = 300;
                    await bulkCopy.WriteToServerAsync(dataTable);
                }

                Debug.WriteLine($"DeltaSyncService: Bulk inserted {dataTable.Rows.Count} records into temp table for '{tableName}'");

                // Merge from temp table to actual table (handles both insert and update)
                var mergeCommand = new SqlCommand(
                    $@"MERGE INTO {tableName} AS target
                       USING {tempTableName} AS source
                       ON target.[{primaryKeyColumn}] = source.[{primaryKeyColumn}]
                       WHEN MATCHED THEN
                           UPDATE SET {BuildUpdateSetClause(dataTable, primaryKeyColumn)}
                       WHEN NOT MATCHED THEN
                           INSERT ({string.Join(", ", dataTable.Columns.Cast<DataColumn>().Select(c => $"[{c.ColumnName}]"))})
                           VALUES ({string.Join(", ", dataTable.Columns.Cast<DataColumn>().Select(c => $"source.[{c.ColumnName}]"))});",
                    connection);
                
                await mergeCommand.ExecuteNonQueryAsync();
                Debug.WriteLine($"DeltaSyncService: Merged {dataTable.Rows.Count} records into '{tableName}'");
            }
            finally
            {
                // Drop temp table
                var dropCommand = new SqlCommand($"DROP TABLE IF EXISTS {tempTableName}", connection);
                await dropCommand.ExecuteNonQueryAsync();
            }

            Debug.WriteLine($"DeltaSyncService: Successfully upserted {dataTable.Rows.Count} records to remote table '{tableName}'");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"DeltaSyncService: Error upserting to remote table '{tableName}' - {ex.Message}");
            Debug.WriteLine($"DeltaSyncService: Stack trace: {ex.StackTrace}");
            throw;
        }
    }

    /// <summary>
    /// Create a temporary table with the same schema as the source table
    /// </summary>
    private async Task CreateTempTableAsync(SqlConnection connection, string sourceTable, string tempTableName, DataTable dataTable)
    {
        try
        {
            // Create temp table with same structure
            var createCommand = new SqlCommand(
                $"SELECT TOP 0 * INTO {tempTableName} FROM {sourceTable}",
                connection);
            await createCommand.ExecuteNonQueryAsync();
            Debug.WriteLine($"DeltaSyncService: Created temp table '{tempTableName}'");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"DeltaSyncService: Error creating temp table - {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Build the UPDATE SET clause for MERGE statement
    /// </summary>
    private string BuildUpdateSetClause(DataTable dataTable, string primaryKeyColumn)
    {
        var setClauses = dataTable.Columns.Cast<DataColumn>()
            .Where(c => c.ColumnName != primaryKeyColumn)
            .Select(c => $"target.[{c.ColumnName}] = source.[{c.ColumnName}]");
        
        return string.Join(", ", setClauses);
    }

    /// <summary>
    /// Get the primary key column name for a table
    /// </summary>
    private async Task<string> GetPrimaryKeyColumnAsync(SqlConnection connection, string tableName)
    {
        try
        {
            var command = new SqlCommand(
                @"SELECT COLUMN_NAME FROM INFORMATION_SCHEMA.KEY_COLUMN_USAGE 
                  WHERE TABLE_NAME = @table AND CONSTRAINT_NAME LIKE 'PK_%'",
                connection);
            command.Parameters.AddWithValue("@table", tableName);

            var result = await command.ExecuteScalarAsync();
            return result?.ToString() ?? string.Empty;
        }
        catch
        {
            return string.Empty;
        }
    }
}
