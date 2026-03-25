# Database Configuration Guide

## Connection String Setup

### Option 1: LocalDB (Development - Default)
No action needed. The default connection string in Program.cs is:
```
Server=(localdb)\mssqllocaldb;Database=AmsaReportingDb;Trusted_Connection=true;
```

This uses Windows authentication on your local machine. The database `AmsaReportingDb` will be created automatically when you run migrations.

### Option 2: SQL Server (Local Instance)
If you have SQL Server running locally, update `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "AmsaReportingDb": "Server=.;Database=AmsaReportingDb;Trusted_Connection=true;"
  }
}
```

### Option 3: SQL Server (Remote/Cloud)
For production or remote SQL Server:

```json
{
  "ConnectionStrings": {
    "AmsaReportingDb": "Server=your-server.database.windows.net;Database=AmsaReportingDb;User Id=sa;Password=YourPassword;"
  }
}
```

### Option 4: Azure SQL Database
For Azure deployment:

```json
{
  "ConnectionStrings": {
    "AmsaReportingDb": "Server=tcp:your-server.database.windows.net,1433;Initial Catalog=AmsaReportingDb;Persist Security Info=False;User ID=admin;Password=YourPassword;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;"
  }
}
```

---

## Creating and Updating the Database

### First-Time Setup (Create Database)
```bash
# Navigate to project root
cd AMSAReportingSystem

# Add initial migration
dotnet ef migrations add InitialCreate --project AMSAReportingSystem

# Create database with all tables
dotnet ef database update --project AMSAReportingSystem
```

### After Model Changes
```bash
# Create migration for changes
dotnet ef migrations add DescriptiveChangeNameHere --project AMSAReportingSystem

# Apply migration
dotnet ef database update --project AMSAReportingSystem
```

### Rollback Database
```bash
# Remove last migration (if not applied yet)
dotnet ef migrations remove --project AMSAReportingSystem

# Or revert to previous migration
dotnet ef database update PreviousMigrationName --project AMSAReportingSystem
```

### View Generated SQL
```bash
# See SQL before applying
dotnet ef migrations script InitialCreate --project AMSAReportingSystem

# See SQL between two migrations
dotnet ef migrations script PreviousMigration CurrentMigration --project AMSAReportingSystem
```

---

## Database Structure

### Tables Created (25+)
**Core Tables:**
- ReportingCycles
- States
- Units
- Reports
- DepartmentReports

**Department-Specific Tables:**
- TaleemReports
- TablighReports
- WelfareReports
- SportReports
- FinanceReports
- HealthReports
- SecondarySchoolReports
- TajneedReports
- GeneralReports

**State-Level Tables:**
- StateReports
- StateReportActivities
- StateReportAttachments
- StateReportActivityLogs

**Supporting Tables:**
- ReportActivityLogs
- ReportAttachments
- Notifications
- ComplianceChecks
- Reminders

### Key Constraints
- **Unique**: (UnitId, CycleId) on Reports, (StateId, CycleId) on StateReports
- **Indexes**: 20+ indexes for performance
- **Foreign Keys**: 25+ with cascade delete behavior
- **Seed Data**: 37 Nigerian states, 1 sample reporting cycle

---

## Checking Database Status

### List All Migrations
```bash
dotnet ef migrations list --project AMSAReportingSystem
```

### Get Current Database Version
```bash
dotnet ef migrations list --project AMSAReportingSystem | grep "Migration"
```

### Verify Tables Exist (via SQL)
```sql
-- List all tables
SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_TYPE = 'BASE TABLE' 
ORDER BY TABLE_NAME;

-- Count tables (should be ~25)
SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_TYPE = 'BASE TABLE';

-- Check seeded states
SELECT * FROM States;
```

---

## Troubleshooting

### Issue: "Cannot connect to LocalDB"
**Solution**: LocalDB may not be running
```bash
# Start LocalDB
sqllocaldb start mssqllocaldb
```

### Issue: "Database 'AmsaReportingDb' already exists"
**Solution**: Either use existing or drop and recreate
```bash
# Drop and recreate (CAUTION: loses data)
dotnet ef database drop --project AMSAReportingSystem
dotnet ef database update --project AMSAReportingSystem

# Or just apply pending migrations
dotnet ef database update --project AMSAReportingSystem
```

### Issue: "Invalid column name"
**Solution**: Migration may not be applied
```bash
# Check pending migrations
dotnet ef migrations list --project AMSAReportingSystem

# Apply pending migrations
dotnet ef database update --project AMSAReportingSystem
```

### Issue: EF Core not finding DbContext
**Solution**: Ensure Program.cs has:
```csharp
using AMSAReportingSystem.Data;
// ...
builder.Services.AddDbContext<AmsaReportingDbContext>(options =>
    options.UseSqlServer(connectionString));
```

---

## Best Practices

1. **Connection Strings in appsettings.json (not hardcoded)**
   - Development: Use LocalDB
   - Production: Use environment variables or Azure Key Vault

2. **Always backup before migrations in production**
   ```bash
   # Backup SQL Server database
   ```

3. **Test migrations locally first**
   - Apply to LocalDB
   - Run automated tests
   - Then deploy to production

4. **Use descriptive migration names**
   ```bash
   dotnet ef migrations add AddTaleemReportComplianceFields --project AMSAReportingSystem
   ```

5. **Keep migrations in source control**
   - Migrations folder tracked in Git
   - Allows team to sync database schema

---

## Next Steps

1. ✅ Ensure appsettings.json has ConnectionStrings section (or use default LocalDB)
2. ✅ Run `dotnet ef migrations add InitialCreate`
3. ✅ Run `dotnet ef database update`
4. ✅ Verify tables created with `SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES`
5. ⏳ Create repository pattern and services
6. ⏳ Build API endpoints
