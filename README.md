# EduCRM Setup Instructions

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

### Step 3: Create & seed the database

`Database\InitializeDatabase.sql` now contains **all schema + seed data**. Running it will *drop and recreate* the `EduCRM` database, then populate every table with deterministic demo data (admin/teacher/student, plus 10×3×30 bulk accounts, sample announcements, conversations, etc.).

#### Option A – `sqlcmd` (recommended)
```powershell
sqlcmd -S "LAPTOP-L1R9L9R3\SQLEXPRESS01" -i ".\Database\InitializeDatabase.sql"
```
Expected output:
```
Changed database context to 'EduCRM'.
EduCRM database created and fully seeded.
```

#### Option B – SSMS
1. Open the script in SSMS.
2. Make sure no other queries are connected to `EduCRM`.
3. Execute (F5). The script automatically runs:
   ```sql
   ALTER DATABASE [EduCRM] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
   DROP DATABASE [EduCRM];
   CREATE DATABASE [EduCRM];
   ```
   followed by all table/seed statements.

> **If the DROP fails** (e.g., another app still has connections), manually run  
> `ALTER DATABASE [EduCRM] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [EduCRM];`  
> then re-run the initializer.

### Step 4: Run the Application

```bash
dotnet run -f net9.0-windows10.0.19041.0
```

The app will:
- Connect to SQL Server
- Automatically seed 3 test users on first run
- Display debug messages in the output

### Step 5: Test login

| Role    | Email                     | Password    | Notes                               |
|---------|---------------------------|-------------|-------------------------------------|
| Admin   | `admin@university.edu`    | `admin@123` | Full system access                  |
| Teacher | `teacher@university.edu`  | `teacher@123` | Adviser for the baseline student |
| Teacher | `dr.smith@university.edu` | `teacher@123` | Second faculty demo account      |
| Student | `student@university.edu`  | `student@123` | Linked to John Teacher           |

Additional demo users:
- **Teachers:** `teacher01@university.edu` → `teacher10@university.edu` (password `teacher@123`)
- **Students:** `student{TT}{C}{SS}@university.edu` (password `student@123`) where:
  - `{TT}` = teacher index `01`–`10`
  - `{C}` = class `1`–`3`
  - `{SS}` = student number `01`–`30`
  - Example: `student01105@university.edu` = Teacher 01, Class 1, Student 05.

---

## Troubleshooting

### "Invalid email or password" error
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