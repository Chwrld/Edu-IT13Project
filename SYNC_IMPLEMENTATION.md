# Online/Offline Sync Implementation

## Overview
Simple sync mechanism for online/offline functionality that syncs data between local (offline) and remote (online) databases.

## Architecture

### Connection Strings
Two databases are configured in `appsettings.json`:

1. **Local Database** (Offline)
   - Connection: `EduCrmSql`
   - Server: `LAPTOP-L1R9L9R3\SQLEXPRESS01`
   - Database: `EduCRM`
   - Used when offline

2. **Remote Database** (Online)
   - Connection: `EduCrmRemote`
   - Server: `db34874.public.databaseasp.net`
   - Database: `db34874`
   - User: `db34874`
   - Used for syncing when online

### Database Schema
Both databases have **identical table structures**:
- users
- advisers
- admins
- courses
- students
- student_courses
- class_assignments
- assignment_submissions
- student_course_grades
- student_achievements
- announcements
- announcement_views
- messages
- conversations
- support_tickets
- ticket_comments

## Usage

### Inject SyncService
```csharp
public class MyPage : ContentPage
{
    private readonly SyncService _syncService;

    public MyPage(SyncService syncService)
    {
        _syncService = syncService;
    }
}
```

### Check Online Status
```csharp
bool isOnline = await _syncService.IsOnlineAsync();
if (isOnline)
{
    // Use remote database
}
else
{
    // Use local database
}
```

### Get Sync Status
```csharp
var status = await _syncService.GetStatusAsync();
Debug.WriteLine($"Status: {status.Status}");
Debug.WriteLine($"Online: {status.IsOnline}");
Debug.WriteLine($"Last Sync: {status.LastSyncTime}");
```

### Sync to Remote
When user comes back online, sync local changes to remote:
```csharp
bool success = await _syncService.SyncToRemoteAsync();
if (success)
{
    await DisplayAlert("Success", "Data synced to remote database", "OK");
}
else
{
    await DisplayAlert("Error", "Sync failed - still offline", "OK");
}
```

## How It Works

### Offline Mode
- App writes all data to **local database** (SQL Server Express)
- No network calls needed
- Data persists locally

### Online Mode
- App detects internet connectivity
- Can call `SyncToRemoteAsync()` to push local data to remote
- Syncs entire tables (simple approach, not delta sync)

### Sync Process
1. Check if remote database is accessible
2. For each table (in dependency order):
   - Read all rows from local database
   - Clear remote table (preserves schema)
   - Bulk insert local data into remote
3. Log progress and errors

## Configuration

### Update Connection Strings
Edit `appsettings.json`:
```json
{
  "ConnectionStrings": {
    "EduCrmSql": "Data Source=YOUR_LOCAL_SERVER;Initial Catalog=EduCRM;...",
    "EduCrmRemote": "Server=YOUR_REMOTE_SERVER;Database=YOUR_DB;User Id=YOUR_USER;Password=YOUR_PASSWORD;..."
  }
}
```

### Ensure Schema Matches
Both databases must have identical table structures. Use `InitializeDatabase.sql` to create schema on both servers.

## Limitations & Future Improvements

### Current Limitations
- **Full sync only**: Syncs entire tables, not just changes
- **No conflict resolution**: Last write wins
- **No selective sync**: All tables synced together
- **No background sync**: Manual trigger required

### Recommended Future Enhancements
1. **Delta Sync**: Track changes with timestamps, sync only modified rows
2. **Selective Tables**: Allow syncing specific tables
3. **Background Sync**: Auto-sync when online
4. **Conflict Resolution**: Handle concurrent edits
5. **Sync Queue**: Queue changes while offline, batch sync when online
6. **Compression**: Compress data for faster transfer
7. **Retry Logic**: Automatic retry with exponential backoff

## Files Modified

- `Services/SyncService.cs` - New sync service
- `appsettings.json` - Added remote connection string
- `MauiProgram.cs` - Registered SyncService in DI

## Testing

```csharp
// Test offline detection
var isOnline = await syncService.IsOnlineAsync();
Assert.False(isOnline); // When disconnected

// Test sync
var success = await syncService.SyncToRemoteAsync();
Assert.True(success); // When connected
```

## Notes

- Existing offline functionality is **not affected**
- Local database continues to work as before
- Sync is **optional** - app works fine without it
- No changes to existing services or pages required
