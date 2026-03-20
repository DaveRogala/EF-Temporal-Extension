using System.Reflection;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EF.TemporalExtensions;

/// <summary>
/// Extends <see cref="TemporalTableBuilder"/> with a <c>HasRetentionPeriod</c> method,
/// allowing the SQL Server temporal history retention period to be configured via the
/// EF Core fluent API.
/// </summary>
/// <example>
/// <code>
/// entity.ToTable("Orders", schema: "sales",
///     o => o.IsTemporal()
///            .HasRetentionPeriod(6, TemporalPeriodUnit.Month));
/// </code>
/// </example>
public static class TemporalTableBuilderExtensions
{
    // EF Core stores the underlying EntityTypeBuilder in a private field.
    // This is the only practical way to set a custom annotation from an extension
    // method on TemporalTableBuilder, which does not expose the builder publicly.
    // Tested against EF Core 8, 9, and 10. If the field name ever changes, the
    // InvalidOperationException below will surface it at startup, not silently.
    private static readonly FieldInfo EntityTypeBuilderField =
        typeof(TemporalTableBuilder)
            .GetField("_entityTypeBuilder", BindingFlags.NonPublic | BindingFlags.Instance)
        ?? throw new InvalidOperationException(
            "EF.TemporalExtensions: Could not locate '_entityTypeBuilder' on " +
            $"{nameof(TemporalTableBuilder)}. This may mean an incompatible EF Core version " +
            "is installed. Please open an issue at https://github.com/DaveRogala/EF-Temporal-Extension.");

    /// <summary>
    /// Configures the SQL Server temporal history retention period for this table.
    /// Generates <c>ALTER TABLE … SET (SYSTEM_VERSIONING = ON (… HISTORY_RETENTION_PERIOD = N UNIT))</c>
    /// in the migration.
    /// </summary>
    /// <param name="builder">The temporal table builder returned by <c>IsTemporal()</c>.</param>
    /// <param name="value">
    ///   The numeric retention value. Ignored when <paramref name="unit"/> is
    ///   <see cref="TemporalPeriodUnit.Infinite"/>.
    /// </param>
    /// <param name="unit">The time unit for the retention period.</param>
    /// <returns>The same <see cref="TemporalTableBuilder"/> to allow further chaining.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    ///   Thrown when <paramref name="value"/> is less than 1 and <paramref name="unit"/>
    ///   is not <see cref="TemporalPeriodUnit.Infinite"/>.
    /// </exception>
    public static TemporalTableBuilder HasRetentionPeriod(
        this TemporalTableBuilder builder,
        int value,
        TemporalPeriodUnit unit)
    {
        ArgumentNullException.ThrowIfNull(builder);

        if (unit != TemporalPeriodUnit.Infinite && value < 1)
            throw new ArgumentOutOfRangeException(nameof(value),
                $"Retention value must be >= 1 when unit is {unit}.");

        var retention         = new TemporalRetentionPeriod(value, unit);
        var entityTypeBuilder = (EntityTypeBuilder)EntityTypeBuilderField.GetValue(builder)!;
        entityTypeBuilder.HasAnnotation(TemporalRetentionAnnotation.Name, retention.Serialize());

        return builder;
    }

    /// <summary>
    /// Sets the retention period to <see cref="TemporalPeriodUnit.Infinite"/>,
    /// explicitly removing any previously configured limit.
    /// </summary>
    /// <param name="builder">The temporal table builder returned by <c>IsTemporal()</c>.</param>
    /// <returns>The same <see cref="TemporalTableBuilder"/> to allow further chaining.</returns>
    public static TemporalTableBuilder HasInfiniteRetention(this TemporalTableBuilder builder)
        => builder.HasRetentionPeriod(0, TemporalPeriodUnit.Infinite);
}
