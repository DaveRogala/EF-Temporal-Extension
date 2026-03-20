namespace EF.TemporalExtensions;

/// <summary>
/// Holds the annotation name used to store the retention period on an entity type.
/// Following EF Core's SqlServer annotation naming convention.
/// </summary>
internal static class TemporalRetentionAnnotation
{
    /// <summary>
    /// The annotation name stored on the EF Core entity type and propagated to
    /// <see cref="Microsoft.EntityFrameworkCore.Migrations.Operations.MigrationOperation"/> annotations.
    /// Value is the serialized <see cref="TemporalRetentionPeriod"/>.
    /// </summary>
    internal const string Name = "SqlServer:TemporalHistoryRetentionPeriod";
}
