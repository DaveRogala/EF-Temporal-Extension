using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.SqlServer.Migrations.Internal;

namespace EF.TemporalExtensions;

/// <summary>
/// Extends the SQL Server migrations annotation provider so that the
/// <see cref="TemporalRetentionAnnotation.Name"/> annotation is included
/// in the set of annotations written to <c>CreateTableOperation</c> and
/// <c>AlterTableOperation</c> during migration generation.
///
/// Without this, EF Core's model-differ would not carry the custom annotation
/// from the entity type through to the migration operation, and the SQL
/// generator would never see it.
/// </summary>
public sealed class TemporalRetentionAnnotationProvider : SqlServerMigrationsAnnotationProvider
{
    /// <inheritdoc/>
    public TemporalRetentionAnnotationProvider(MigrationsAnnotationProviderDependencies dependencies)
        : base(dependencies) { }

    /// <inheritdoc/>
    public override IEnumerable<IAnnotation> For(ITable table, bool designTime)
    {
        // Yield all annotations the base SQL Server provider already produces.
        foreach (var annotation in base.For(table, designTime))
            yield return annotation;

        // Walk the entity type mappings for this table and surface our retention
        // annotation if any entity type has it set.
        foreach (var mapping in table.EntityTypeMappings)
        {
            var annotation = mapping.TypeBase.FindAnnotation(TemporalRetentionAnnotation.Name);
            if (annotation is not null)
            {
                yield return annotation;
                yield break; // only one retention period per table
            }
        }
    }
}
