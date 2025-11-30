# EduCRM Database Setup

## Quick Start (In-Memory - No Setup Required)

The app works immediately with **in-memory storage**. No database setup needed!

Test credentials:
- Admin: `admin@university.edu` / `admin123`
- Teacher: `teacher@university.edu` / `teacher123`
- Student: `student@university.edu` / `student123`

Data is lost when the app restarts.

---

## SQL Server Setup (Optional - For Persistent Storage)

### Prerequisites
- SQL Server (SQL Server Express or higher)
- SQL Server Management Studio (SSMS) or sqlcmd

### Step 1: Create Database and Schema

Run the SQL script to create the database and users table:

```bash
sqlcmd -S LAPTOP-L1R9L9R3\SQLEXPRESS01 -i Database\InitializeDatabase.sql
```

Or use SQL Server Management Studio:
1. Open SSMS
2. Connect to `LAPTOP-L1R9L9R3\SQLEXPRESS01`
3. Open `Database\InitializeDatabase.sql`
4. Execute the script

### Step 2: Verify Connection String

The connection string is configured in `appsettings.json`:
```json
{
  "ConnectionStrings": {
    "EduCrmSql": "Data Source=LAPTOP-L1R9L9R3\\SQLEXPRESS01;Initial Catalog=EduCRM;Integrated Security=True;Connect Timeout=30;Encrypt=False;Trust Server Certificate=True;Application Intent=ReadWrite;Multi Subnet Failover=False"
  }
}
```

Update the connection string if your SQL Server instance name is different.

### Step 3: Enable SQL Server in MauiProgram.cs

Open `MauiProgram.cs` and uncomment this line (around line 50):

```csharp
builder.Services.AddSingleton<DbConnection>(sp => new SqlServerDbConnection(sp.GetRequiredService<IConfiguration>()) as DbConnection);
```

Comment out the in-memory line:
```csharp
// builder.Services.AddSingleton<DbConnection>();
```

### Step 4: Run the Application

```bash
dotnet run -f net9.0-windows10.0.19041.0
```

The application will automatically seed default users on first run:
- **Admin**: `admin@university.edu` / `admin123`
- **Teacher**: `teacher@university.edu` / `teacher123`
- **Student**: `student@university.edu` / `student123`

## Default Test Credentials

| Role | Email | Password |
|------|-------|----------|
| Admin | admin@university.edu | admin123 |
| Teacher | teacher@university.edu | teacher123 |
| Student | student@university.edu | student123 |

## Troubleshooting

### Connection Failed
- Verify SQL Server is running
- Check the instance name matches your SQL Server configuration
- Ensure Integrated Security is enabled or use SQL authentication

### Database Not Found
- Run the `InitializeDatabase.sql` script to create the database and schema

### Users Not Seeding
- Check the application logs in the debug output
- Verify the database connection is successful
- Ensure the `users` table exists in the `EduCRM` database
