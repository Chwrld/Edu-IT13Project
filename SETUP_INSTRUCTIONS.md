# EduCRM Setup Instructions

## SQL Server Setup (Required)

### Prerequisites
- **SQL Server 2019 or later** (SQL Server Express is fine)
- **SQL Server Management Studio (SSMS)** - for running SQL scripts
- **.NET 9.0 SDK** - to run the application

### Step 1: Find Your SQL Server Instance Name

1. Open **SQL Server Management Studio (SSMS)**
2. In the "Connect to Server" dialog, note your **Server name** (e.g., `LAPTOP-ABC123\SQLEXPRESS`)
3. This is your **SQL Server instance name** - you'll need it in the next step

### Step 2: Update Connection String

1. Open `appsettings.json` in the project root
2. Find the `ConnectionStrings` section
3. Update the `Data Source` to match your SQL Server instance:

```json
{
  "ConnectionStrings": {
    "EduCrmSql": "Data Source=YOUR_SERVER_NAME_HERE\\SQLEXPRESS;Initial Catalog=EduCRM;Integrated Security=True;Connect Timeout=30;Encrypt=False;Trust Server Certificate=True;Application Intent=ReadWrite;Multi Subnet Failover=False"
  }
}
```

**Example:** If your server is `LAPTOP-ABC123\SQLEXPRESS`, the connection string should be:
```
Data Source=LAPTOP-ABC123\SQLEXPRESS;Initial Catalog=EduCRM;...
```

### Step 3: Create Database and Tables

1. Open **SQL Server Management Studio (SSMS)**
2. Connect to your SQL Server instance
3. Open the file: `Database\InitializeDatabase.sql`
4. Click **Execute** (or press F5)

This will:
- Create the `EduCRM` database
- Create all required tables (users, conversations, messages, etc.)
- Prepare the database for the application

### Step 4: Run the Application

```bash
dotnet run -f net9.0-windows10.0.19041.0
```

The app will:
- Connect to SQL Server
- Automatically seed 3 test users on first run
- Display debug messages in the output

### Step 5: Test Login

Use these credentials:

| Role | Email | Password |
|------|-------|----------|
| Admin | admin@university.edu | admin@123 |
| Teacher | teacher@university.edu | teacher@123 |
| Student | student@university.edu | student@123 |

---

## Troubleshooting

### ❌ "Invalid email or password" error
**Cause:** Database is empty (seeding failed)

**Solutions:**
1. Check debug output for errors (see below)
2. Verify you ran the SQL script in Step 3
3. Verify the connection string in `appsettings.json` matches your SQL Server instance
4. Verify SQL Server is running

### ❌ "Connection timeout" or "Cannot connect to server" error
**Cause:** SQL Server is not running or connection string is wrong

**Solutions:**
1. Start SQL Server service (Windows Services)
2. Verify your SQL Server instance name in SSMS
3. Update `appsettings.json` with the correct instance name
4. Test the connection in SSMS first

### ❌ "Database not found" error
**Cause:** The SQL script was not executed

**Solutions:**
1. Open SSMS
2. Connect to your SQL Server instance
3. Run `Database\InitializeDatabase.sql`
4. Verify the `EduCRM` database appears in SSMS

## Verify Database Setup

To confirm everything is working:

1. Open **SQL Server Management Studio (SSMS)**
2. Connect to your SQL Server instance
3. Expand: **Databases → EduCRM → Tables → dbo.users**
4. Right-click `users` → Select **Top 1000 Rows**
5. You should see 3 users (admin, teacher, student)

If the table is empty, the seeding failed. Check the debug output for errors.

---