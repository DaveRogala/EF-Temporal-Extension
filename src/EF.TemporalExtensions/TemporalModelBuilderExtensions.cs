using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EF.TemporalExtensions;

/// <summary>
/// Extension methods on <see cref="ModelBuilder"/> for bulk-configuring temporal tables.
/// </summary>
public static class TemporalModelBuilderExtensions
{
    /// <summary>
    /// Marks every entity type in the model that implements <see cref="ITemporalEntity"/>
    /// as a SQL Server system-versioned temporal table.
    /// </summary>
    /// <param name="modelBuilder">The model builder.</param>
    /// <param name="configure">Optional per-entity configuration callback.</param>
    public static ModelBuilder UseTemporalTablesForTemporalEntities(
        this ModelBuilder modelBuilder,
        Action<TemporalTableBuilder>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (typeof(ITemporalEntity).IsAssignableFrom(entityType.ClrType))
            {
                modelBuilder
                    .Entity(entityType.ClrType)
                    .ToTable(t => t.IsTemporal(configure));
            }
        }

        return modelBuilder;
    }

    /// <summary>
    /// Marks every entity type in the model as a SQL Server system-versioned temporal table.
    /// Use with caution — prefer <see cref="UseTemporalTablesForTemporalEntities"/> or
    /// per-entity configuration via <see cref="TemporalEntityTypeBuilderExtensions.HasTemporalTable{TEntity}"/>.
    /// </summary>
    /// <param name="modelBuilder">The model builder.</param>
    /// <param name="configure">Optional per-entity configuration callback.</param>
    public static ModelBuilder UseTemporalTablesForAllEntities(
        this ModelBuilder modelBuilder,
        Action<TemporalTableBuilder>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            modelBuilder
                .Entity(entityType.ClrType)
                .ToTable(t => t.IsTemporal(configure));
        }

        return modelBuilder;
    }
}

/// <summary>
/// Extension methods on <see cref="EntityTypeBuilder{TEntity}"/> for configuring temporal tables.
/// </summary>
public static class TemporalEntityTypeBuilderExtensions
{
    /// <summary>
    /// Configures <typeparamref name="TEntity"/> as a SQL Server system-versioned temporal table
    /// using the default history table name (<c>{TableName}History</c>).
    /// </summary>
    /// <typeparam name="TEntity">The entity type.</typeparam>
    /// <param name="builder">The entity type builder.</param>
    public static EntityTypeBuilder<TEntity> HasTemporalTable<TEntity>(
        this EntityTypeBuilder<TEntity> builder)
        where TEntity : class
    {
        ArgumentNullException.ThrowIfNull(builder);
        return builder.ToTable(t => t.IsTemporal());
    }

    /// <summary>
    /// Configures <typeparamref name="TEntity"/> as a SQL Server system-versioned temporal table
    /// with a custom history table name.
    /// </summary>
    /// <typeparam name="TEntity">The entity type.</typeparam>
    /// <param name="builder">The entity type builder.</param>
    /// <param name="historyTableName">The name for the history table.</param>
    /// <param name="historySchema">Optional schema for the history table.</param>
    public static EntityTypeBuilder<TEntity> HasTemporalTable<TEntity>(
        this EntityTypeBuilder<TEntity> builder,
        string historyTableName,
        string? historySchema = null)
        where TEntity : class
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrWhiteSpace(historyTableName);

        return builder.ToTable(t => t.IsTemporal(ttb =>
        {
            ttb.UseHistoryTable(historyTableName, historySchema);
        }));
    }

    /// <summary>
    /// Configures <typeparamref name="TEntity"/> as a SQL Server system-versioned temporal table
    /// with full <see cref="TemporalTableBuilder"/> callback control.
    /// </summary>
    /// <typeparam name="TEntity">The entity type.</typeparam>
    /// <param name="builder">The entity type builder.</param>
    /// <param name="configure">Callback to configure the temporal table (history table name, period columns, etc.).</param>
    public static EntityTypeBuilder<TEntity> HasTemporalTable<TEntity>(
        this EntityTypeBuilder<TEntity> builder,
        Action<TemporalTableBuilder> configure)
        where TEntity : class
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(configure);

        return builder.ToTable(t => t.IsTemporal(configure));
    }

    /// <summary>
    /// Configures <typeparamref name="TEntity"/> as a temporal table and applies custom period
    /// column names instead of the SQL Server defaults (<c>SysStartTime</c> / <c>SysEndTime</c>).
    /// </summary>
    /// <typeparam name="TEntity">The entity type.</typeparam>
    /// <param name="builder">The entity type builder.</param>
    /// <param name="periodStartColumnName">Column name for the period-start (e.g., <c>ValidFrom</c>).</param>
    /// <param name="periodEndColumnName">Column name for the period-end (e.g., <c>ValidTo</c>).</param>
    /// <param name="historyTableName">Optional history table name.</param>
    public static EntityTypeBuilder<TEntity> HasTemporalTableWithPeriodColumns<TEntity>(
        this EntityTypeBuilder<TEntity> builder,
        string periodStartColumnName,
        string periodEndColumnName,
        string? historyTableName = null)
        where TEntity : class
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrWhiteSpace(periodStartColumnName);
        ArgumentException.ThrowIfNullOrWhiteSpace(periodEndColumnName);

        return builder.ToTable(t => t.IsTemporal(ttb =>
        {
            ttb.HasPeriodStart(periodStartColumnName);
            ttb.HasPeriodEnd(periodEndColumnName);

            if (historyTableName is not null)
                ttb.UseHistoryTable(historyTableName);
        }));
    }
}
