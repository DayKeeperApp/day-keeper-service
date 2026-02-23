# Database Migration Workflow

How to create, apply, and manage EF Core migrations for the DayKeeper service.

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download) (pinned in `global.json`)
- [Task](https://taskfile.dev/) (task runner)
- PostgreSQL database (local or containerized)
- EF Core CLI tools (installed automatically via `task setup` or `dotnet tool restore`)

## Naming Convention

Migrations use EF Core's default timestamp prefix with a PascalCase
descriptive suffix:

```text
{Timestamp}_{Description}.cs
```

Examples:

- `20260223043429_InitialCreate.cs`
- `20260301120000_AddSpacesTable.cs`
- `20260315090000_AddCalendarAndEvents.cs`

Guidelines:

- Use **PascalCase** for the description.
- Start with a verb: `Add`, `Remove`, `Rename`, `Alter`, `Create`.
- Be specific: `AddSpacesTable` not `UpdateSchema`.
- One migration per logical change. Avoid bundling unrelated changes.

## Taskfile Commands

| Command                                | Description                                           |
| -------------------------------------- | ----------------------------------------------------- |
| `task db:migrate:add -- MigrationName` | Create a new migration                                |
| `task db:migrate:apply`                | Apply pending migrations to the database              |
| `task db:migrate:script`               | Generate idempotent SQL script (migrations.sql)       |
| `task db:migrate:bundle`               | Create self-contained migration bundle for production |

## Creating a Migration

**Step 1** -- Make changes to domain entities and/or EF Core configurations
(in `src/DayKeeper.Infrastructure/Persistence/Configurations/`).

**Step 2** -- Create the migration:

```bash
task db:migrate:add -- AddSpacesTable
```

**Step 3** -- Review the generated files in
`src/DayKeeper.Infrastructure/Persistence/Migrations/`.
Verify:

- The `Up()` method matches your intent.
- The `Down()` method correctly reverses the changes.
- No unintended changes are included.

**Step 4** -- Apply to your local database:

```bash
task db:migrate:apply
```

**Step 5** -- Commit the migration files (`.cs` and `.Designer.cs`) along with
the updated `DayKeeperDbContextModelSnapshot.cs`.

## Applying Migrations

### Development

Run explicitly after pulling new migrations or creating your own:

```bash
task db:migrate:apply
```

Ensure your connection string is configured in
`appsettings.Development.json`, user secrets, or an environment variable.

### CI/CD

The CI pipeline validates that the migration chain is coherent by
generating an idempotent SQL script. This catches:

- Broken migration snapshots
- Conflicting migrations from parallel branches
- Compilation errors in migration code

Tests run against SQLite in-memory (via `EnsureCreated()`), which
validates schema correctness independently of migrations.

### Production

Production uses a **migration bundle** -- a self-contained executable:

```bash
# Build the bundle
task db:migrate:bundle

# Run it (pass connection string)
./efbundle --connection "Host=...;Database=...;Username=...;Password=..."
```

The bundle is idempotent: it checks `__EFMigrationsHistory` and only
applies pending migrations.

Alternatively, generate and review an idempotent SQL script:

```bash
task db:migrate:script
# Review migrations.sql, then apply manually
psql -h <host> -d <db> -f migrations.sql
```

## Idempotency

All migration strategies are idempotent:

- **EF Core migrations**: The `__EFMigrationsHistory` table tracks
  applied migrations. `database update` skips those already applied.
- **SQL scripts**: Generated with `--idempotent`, wrapping each
  migration in a conditional check.
- **Migration bundles**: Same behavior as `database update`.

## Resolving Conflicts

When two branches create migrations from the same snapshot:

1. Merge the target branch into your branch.
2. Delete your migration files (keep your entity/configuration changes).
3. Recreate: `task db:migrate:add -- YourMigrationName`
4. Verify the regenerated migration is correct.

## Files Overview

```text
src/DayKeeper.Infrastructure/
  Persistence/
    DayKeeperDbContext.cs                  # DbContext definition
    Configurations/                         # IEntityTypeConfiguration<T> files
    Migrations/
      {Timestamp}_{Name}.cs                # Migration Up/Down methods
      {Timestamp}_{Name}.Designer.cs       # Snapshot at migration time
      DayKeeperDbContextModelSnapshot.cs   # Current model snapshot
```

All migration files must be committed to source control.

## Important Rules

- **Never edit a migration that has been applied to any environment.**
  Create a new migration instead.
- **Never delete applied migrations** from the history.
- **Review every generated migration** before committing.
  EF Core can sometimes generate unexpected changes.
- Tests use `EnsureCreated()` (not migrations) against SQLite.
  This is intentional -- tests validate schema, CI validates migrations.
