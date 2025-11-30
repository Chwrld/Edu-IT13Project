# EduCRM Setup Instructions

## Prerequisites
- SQL Server (SQL Server Express or higher) - **Must be running**
- SQL Server Management Studio (SSMS)

## Step 1: Create Database and Tables

**IMPORTANT: Do this FIRST before running the app!**

1. Open **SQL Server Management Studio (SSMS)**
2. Connect to: `LAPTOP-L1R9L9R3\SQLEXPRESS01`
3. Open the file: `Database\InitializeDatabase.sql`
4. Click **Execute** (or press F5)

This will:
- Create the `EduCRM` database
- Create the `users` table with proper schema

## Step 2: Run the Application

```bash
dotnet run -f net9.0-windows10.0.19041.0
```

The app will:
- Connect to SQL Server
- Automatically seed 3 test users on first run
- Display debug messages in the output

## Step 3: Test Login

Use these credentials:

| Role | Email | Password |
|------|-------|----------|
| Admin | admin@university.edu | admin123 |
| Teacher | teacher@university.edu | teacher123 |
| Student | student@university.edu | student123 |

## Troubleshooting

### "Invalid email or password" error
- **Cause**: Database is empty (seeding failed)
- **Solution**: Make sure you ran the SQL script in Step 1

### "Connection timeout" error
- **Cause**: SQL Server is not running
- **Solution**: Start SQL Server service

### Check Debug Output
Run the app and look for debug messages like:
```
DbInitializer: Starting seed data check...
DbInitializer: No users found, seeding default users...
DbInitializer: Saved admin user
DbInitializer: Seed data completed successfully!
```

If you see errors, they will be printed in the debug output.

## Verify Database

To verify users were created:

1. Open SSMS
2. Connect to `LAPTOP-L1R9L9R3\SQLEXPRESS01`
3. Expand: Databases → EduCRM → Tables → dbo.users
4. Right-click `users` → Select Top 1000 Rows
5. You should see 3 users (admin, teacher, student)
