# EF.TemporalExtensions

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)
[![.NET](https://img.shields.io/badge/.NET-10.0-512BD4)](https://dotnet.microsoft.com)

A .NET 10 class library that provides fluent extension methods for working with **SQL Server system-versioned (temporal) tables** via Entity Framework Core. It wraps EF Core's built-in temporal operators with strongly-typed helpers, adds audit trail utilities, diff support, and model configuration shortcuts.

---

## Features

- Fluent wrappers for all five EF Core temporal operators (`AsOf`, `FromTo`, `Between`, `ContainedIn`, `All`)
- `TemporalPeriod` value type for expressing time ranges cleanly
- `TemporalSnapshot<T>` — pairs a queried entity with its validity period
- `TemporalDiff<T>` — detects `Created` / `Modified` / `Deleted` between two instants
- `GetAuditTrailAsync` — full, chronologically ordered history for a single entity
- `FindAsOfAsync` / `GetDiffAsync` / `CountVersionsAsync` on `DbContext`
- Model builder extensions for configuring temporal tables in `OnModelCreating`
- `ITemporalEntity` marker interface for convention-based bulk configuration
- Automatic UTC normalisation of all `DateTime` inputs
- Targets `net10.0`; no additional runtime dependencies beyond EF Core

---

## Installation

```xml
<PackageReference Include="EF.TemporalExtensions" Version="1.0.0" />
```

> Requires `Microsoft.EntityFrameworkCore` and `Microsoft.EntityFrameworkCore.Relational` 10.x.

---

## Quick start

### 1. Mark entities

Implement `ITemporalEntity` on any entity that maps to a temporal table:

```csharp
public class Order : ITemporalEntity
{
    public int    Id     { get; set; }
    public string Status { get; set; } = string.Empty;
}
```

### 2. Configure the model

```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    // Option A — configure all ITemporalEntity types automatically
    modelBuilder.UseTemporalTablesForTemporalEntities();

    // Option B — configure a single entity with a custom history table
    modelBuilder.Entity<Order>()
                .HasTemporalTable("OrderHistory", schema: "audit");

    // Option C — customise period column names
    modelBuilder.Entity<Order>()
                .HasTemporalTableWithPeriodColumns("ValidFrom", "ValidTo");
}
```

### 3. Query temporal data

```csharp
// All five SQL Server temporal operators:
var asOf        = ctx.Orders.AsOf(DateTime.UtcNow.AddDays(-7));
var fromTo      = ctx.Orders.FromTo(start, end);
var between     = ctx.Orders.Between(start, end);
var containedIn = ctx.Orders.ContainedIn(start, end);
var all         = ctx.Orders.All();

// Use a TemporalPeriod instead of raw DateTimes:
var period = TemporalPeriod.Between(start, end);
var orders = ctx.Orders.Between(period);
```

---

## TemporalPeriod

`TemporalPeriod` is an immutable UTC time-range value type that can be passed to any of the query helpers.

```csharp
var period = new TemporalPeriod(start, end);

// Factory helpers
var instant = TemporalPeriod.At(DateTime.UtcNow);
var window  = TemporalPeriod.Starting(DateTime.UtcNow.AddHours(-1), TimeSpan.FromHours(1));

// Range tests
bool inside    = period.Contains(somePoint);
bool overlaps  = period.Overlaps(otherPeriod);
var  intersect = period.Intersect(otherPeriod); // TemporalPeriod?

// Deconstruct
var (from, to) = period;
```

---

## Audit trail

Retrieve every recorded version of a single entity, ordered chronologically:

```csharp
IReadOnlyList<TemporalSnapshot<Order>> history = await ctx.GetAuditTrailAsync(
    keySelector:           o => o.Id,
    key:                   42,
    periodStartSelector:   o => o.ValidFrom,
    periodEndSelector:     o => o.ValidTo,
    period:                new TemporalPeriod(sixMonthsAgo, DateTime.UtcNow));

foreach (var (order, period) in history)
{
    Console.WriteLine($"[{period}] Status = {order.Status}");
}
```

---

## Diff between two points in time

```csharp
TemporalDiff<Order> diff = await ctx.GetDiffAsync(
    keySelector: o => o.Id,
    key:         42,
    from:        yesterday,
    to:          DateTime.UtcNow);

Console.WriteLine(diff.ChangeType);   // Created | Modified | Deleted
Console.WriteLine(diff.Before?.Status);
Console.WriteLine(diff.After?.Status);
```

---

## Snapshot projection

When querying `TemporalAll()` (or any temporal operator), you can project each row into a `TemporalSnapshot<T>` that carries its validity period alongside the entity:

```csharp
var snapshots = ctx.Orders
                   .All()
                   .Where(o => o.Id == 42)
                   .WithSnapshot(o => o.ValidFrom, o => o.ValidTo);

foreach (var snap in snapshots)
{
    Console.WriteLine($"{snap.Entity.Status} was valid {snap.Period}");
}
```

---

## History for a specific entity

```csharp
// All versions within the last year
var versions = ctx.Orders
                  .HistoryFor(o => o.Id, 42, period: lastYear);

// Count how many versions existed
int count = await ctx.CountVersionsAsync(o => o.Id, 42);
```

---

## Pagination

```csharp
var page = ctx.Orders
              .All()
              .OrderBy(o => EF.Property<DateTime>(o, "ValidFrom"))
              .PagedTemporal(page: 0, pageSize: 25);
```

---

## API reference

### `TemporalQueryExtensions` (on `IQueryable<T>`)

| Method | SQL Server operator |
|--------|-------------------|
| `AsOf(DateTime)` | `FOR SYSTEM_TIME AS OF` |
| `AsOf(TemporalPeriod)` | `FOR SYSTEM_TIME AS OF` (instant period) |
| `FromTo(DateTime, DateTime)` | `FOR SYSTEM_TIME FROM … TO` |
| `FromTo(TemporalPeriod)` | `FOR SYSTEM_TIME FROM … TO` |
| `Between(DateTime, DateTime)` | `FOR SYSTEM_TIME BETWEEN … AND` |
| `Between(TemporalPeriod)` | `FOR SYSTEM_TIME BETWEEN … AND` |
| `ContainedIn(DateTime, DateTime)` | `FOR SYSTEM_TIME CONTAINED IN` |
| `ContainedIn(TemporalPeriod)` | `FOR SYSTEM_TIME CONTAINED IN` |
| `All()` | `FOR SYSTEM_TIME ALL` |
| `WithSnapshot(startExpr, endExpr)` | Projects to `TemporalSnapshot<T>` |
| `HistoryFor(keyExpr, key, period?)` | History for a single key |
| `GetDiffAsync(keyExpr, key, from, to)` | Async diff between two instants |
| `PagedTemporal(page, pageSize)` | Offset pagination |
| `ExistedDuringAsync(keyExpr, key, period)` | Existence check |

### `TemporalDbContextExtensions` (on `DbContext`)

| Method | Description |
|--------|-------------|
| `GetAuditTrailAsync(...)` | Full ordered history as `TemporalSnapshot<T>` list |
| `FindAsOfAsync(keyExpr, key, point)` | Single entity as-of a point |
| `GetDiffAsync(keyExpr, key, from, to)` | Diff between two points |
| `CountVersionsAsync(keyExpr, key, period?)` | Count historical versions |

### `TemporalModelBuilderExtensions` (on `ModelBuilder`)

| Method | Description |
|--------|-------------|
| `UseTemporalTablesForTemporalEntities()` | Configure all `ITemporalEntity` types |
| `UseTemporalTablesForAllEntities()` | Configure every entity type |

### `TemporalEntityTypeBuilderExtensions` (on `EntityTypeBuilder<T>`)

| Method | Description |
|--------|-------------|
| `HasTemporalTable()` | Default history table name |
| `HasTemporalTable(name, schema?)` | Custom history table name |
| `HasTemporalTable(Action<TemporalTableBuilder>)` | Full callback control |
| `HasTemporalTableWithPeriodColumns(start, end, historyTable?)` | Custom period column names |

---

## Requirements

- .NET 10.0
- EF Core 10.x with SQL Server provider (`Microsoft.EntityFrameworkCore.SqlServer`)
- SQL Server 2016+ or Azure SQL Database (system-versioned temporal table support)

---

## License

This project is licensed under the [MIT License](LICENSE).
