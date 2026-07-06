# Entity Framework Core Migrations Guide for MixFlow

## What We've Done So Far

1. ? Installed `dotnet-ef` CLI tool globally
2. ? Added `Microsoft.EntityFrameworkCore.Design` v9.0.17
3. ? Added `Microsoft.EntityFrameworkCore.SqlServer` v9.0.17
4. ? Fixed version mismatch (all EF packages now at 9.0.17)
5. ? Created initial migration (then removed it due to existing tables)

## Common Migration Commands

### 1. Create a New Migration
```powershell
dotnet ef migrations add <MigrationName>
```
Example:
```powershell
dotnet ef migrations add AddPlayerSkillRating
```

### 2. Apply Migrations to Database
```powershell
dotnet ef database update
```

### 3. Remove Last Migration (Before Applied)
```powershell
dotnet ef migrations remove
```

### 4. View Migration Status
```powershell
dotnet ef migrations list
```

### 5. Revert to Previous Migration
```powershell
dotnet ef database update <MigrationName>
```
Example (revert to initial):
```powershell
dotnet ef database update 0
```

## Current Situation

Your tables already exist in the database. You have two options:

### Option A: Fresh Start (Development Only - Deletes All Data)
```powershell
dotnet ef database drop
dotnet ef migrations add InitialCreate
dotnet ef database update
```

### Option B: Keep Existing Data (Production)
Since your tables exist, just mark the migration as applied without running it:
```powershell
dotnet ef migrations add InitialCreate
dotnet ef migrations remove  # Removes without applying (already done)
```

The tables are already created, so migrations will work for future schema changes.

## How to Run Migrations in Program.cs

For automatic migration on startup (optional):

```csharp
// In Program.cs, after building the app but before app.Run()
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<MixFlowDbContext>();
    db.Database.Migrate(); // Applies any pending migrations
}

app.Run();
```

## Fixing Decimal Precision Warnings

The warnings about decimal properties can be fixed by adding precision to the Player and QueueEntry models:

### In OnModelCreating (MixFlowDbContext.cs):
```csharp
modelBuilder.Entity<Player>()
    .Property(p => p.SkillLevel)
    .HasPrecision(5, 2);

modelBuilder.Entity<Player>()
    .Property(p => p.DUPR)
    .HasPrecision(5, 2);

modelBuilder.Entity<Player>()
    .Property(p => p.WinPercentage)
    .HasPrecision(5, 2);

modelBuilder.Entity<QueueEntry>()
    .Property(q => q.PriorityScore)
    .HasPrecision(10, 2);
```

## Next Steps

1. Your database is ready to use
2. For future changes, create migrations with `dotnet ef migrations add <Name>`
3. Apply them with `dotnet ef database update`
4. Consider adding automatic migration in Program.cs for deployments

## Troubleshooting

| Issue | Solution |
|-------|----------|
| "dotnet-ef not found" | Run `dotnet tool install --global dotnet-ef` |
| "Tables already exist" | Your migration won't apply; that's fine - keep using existing tables |
| "Design package required" | Run `dotnet add package Microsoft.EntityFrameworkCore.Design` |
| "Version mismatch" | Ensure all EF packages use the same version (currently 9.0.17) |

