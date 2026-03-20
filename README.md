# EF.TemporalExtensions

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)
[![.NET](https://img.shields.io/badge/.NET-10.0-512BD4)](https://dotnet.microsoft.com)

Extends the EF Core `IsTemporal()` fluent API with a `HasRetentionPeriod()` method, so you can configure SQL Server temporal table history retention directly in your model — no more manually editing migration files.

```csharp
entity.ToTable("Orders", schema: "sales",
    o => o.IsTemporal()
           .HasRetentionPeriod(6, TemporalPeriodUnit.Month));
```

The library hooks into EF Core's migration pipeline and automatically emits the required `ALTER TABLE` statement in every migration that creates or alters that table.

---

## The problem

EF Core's built-in `IsTemporal()` does not expose a way to set `HISTORY_RETENTION_PERIOD`. To configure retention you currently have to manually add SQL to every migration file:

```sql
-- hand-written, fragile, easy to forget
ALTER TABLE [sales].[Orders]
    SET (SYSTEM_VERSIONING = ON (
        HISTORY_TABLE = [sales].[OrdersHistory],
        HISTORY_RETENTION_PERIOD = 6 MONTHS));
```

This library eliminates that manual step.

---

## Installation

```xml
<PackageReference Include="EF.TemporalExtensions" Version="1.0.0" />
```

Requires `Microsoft.EntityFrameworkCore.SqlServer` 10.x.

---

## Setup

Register the library's custom SQL generator and annotation provider once when configuring your `DbContext`:

```csharp
services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(connectionString)
           .UseTemporalRetentionPeriods());   // <-- add this
```

---

## Usage

Chain `HasRetentionPeriod` after `IsTemporal()` in `OnModelCreating`:

```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    // 6 months
    modelBuilder.Entity<Order>()
        .ToTable("Orders", schema: "sales",
            o => o.IsTemporal()
                   .HasRetentionPeriod(6, TemporalPeriodUnit.Month));

    // 1 year, with an explicit history table name
    modelBuilder.Entity<Product>()
        .ToTable("Products",
            o => o.IsTemporal()
                   .HasRetentionPeriod(1, TemporalPeriodUnit.Year)
                   .UseHistoryTable("ProductHistory", "audit"));

    // Explicitly infinite (removes any limit)
    modelBuilder.Entity<Customer>()
        .ToTable("Customers",
            o => o.IsTemporal()
                   .HasInfiniteRetention());
}
```

### Available units

| `TemporalPeriodUnit` | SQL keyword |
|----------------------|-------------|
| `Day`                | `DAYS`      |
| `Week`               | `WEEKS`     |
| `Month`              | `MONTHS`    |
| `Year`               | `YEARS`     |
| `Infinite`           | `INFINITE`  |

---

## Generated migration SQL

For a `CreateTableOperation` EF Core produces its normal temporal DDL, and this library appends:

```sql
ALTER TABLE [sales].[Orders]
    SET (SYSTEM_VERSIONING = ON (
        HISTORY_TABLE = [sales].[OrdersHistory],
        HISTORY_RETENTION_PERIOD = 6 MONTHS));
```

If the retention period changes in a later migration, an `AlterTableOperation` triggers the same `ALTER TABLE` with the updated value.

---

## How it works

Three components wire together to make this seamless:

| Component | Role |
|-----------|------|
| `TemporalTableBuilderExtensions` | Adds `HasRetentionPeriod` / `HasInfiniteRetention` to `TemporalTableBuilder` via an extension method. Stores the value as an EF Core annotation on the entity type. |
| `TemporalRetentionAnnotationProvider` | Subclasses `SqlServerMigrationsAnnotationProvider` to ensure the retention annotation is carried from the entity type into `CreateTableOperation` / `AlterTableOperation`. |
| `TemporalRetentionMigrationsSqlGenerator` | Subclasses `SqlServerMigrationsSqlGenerator` to detect the annotation and append the `ALTER TABLE … HISTORY_RETENTION_PERIOD` statement after the standard temporal DDL. |

All three are registered via `UseTemporalRetentionPeriods()` using EF Core's `ReplaceService` mechanism — no scaffolding changes are needed.

> **Note on reflection.** `TemporalTableBuilder` does not expose its underlying `EntityTypeBuilder` publicly. The extension method accesses the private `_entityTypeBuilder` field via reflection. This field name has been stable across EF Core 8, 9, and 10. If a future EF Core release changes it, an `InvalidOperationException` will surface at startup with a clear message.

---

## Requirements

- .NET 10.0
- EF Core 10.x (`Microsoft.EntityFrameworkCore.SqlServer`)
- SQL Server 2016+ or Azure SQL Database with temporal table support
- `TEMPORAL_HISTORY_RETENTION` must be enabled on the database:
  ```sql
  ALTER DATABASE MyDatabase SET TEMPORAL_HISTORY_RETENTION ON;
  ```

---

## License

[MIT](LICENSE)
