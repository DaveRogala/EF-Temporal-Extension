using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace EF.TemporalExtensions;

/// <summary>
/// Registration extensions for <see cref="DbContextOptionsBuilder"/>.
/// </summary>
public static class TemporalExtensionsDbContextOptionsExtensions
{
    /// <summary>
    /// Registers the EF.TemporalExtensions services so that
    /// <see cref="TemporalTableBuilderExtensions.HasRetentionPeriod"/> configurations
    /// are automatically translated to SQL in generated migrations.
    ///
    /// Call this inside <c>UseSqlServer(…)</c> or directly on
    /// <see cref="DbContextOptionsBuilder"/>:
    /// <code>
    /// services.AddDbContext&lt;MyDbContext&gt;(options =>
    ///     options.UseSqlServer(connectionString)
    ///            .UseTemporalRetentionPeriods());
    /// </code>
    /// </summary>
    /// <param name="optionsBuilder">The options builder being configured.</param>
    /// <returns>The same options builder for further chaining.</returns>
    public static DbContextOptionsBuilder UseTemporalRetentionPeriods(
        this DbContextOptionsBuilder optionsBuilder)
    {
        ArgumentNullException.ThrowIfNull(optionsBuilder);

        optionsBuilder
            .ReplaceService<IMigrationsSqlGenerator,  TemporalRetentionMigrationsSqlGenerator>()
            .ReplaceService<IMigrationsAnnotationProvider, TemporalRetentionAnnotationProvider>();

        return optionsBuilder;
    }

    /// <inheritdoc cref="UseTemporalRetentionPeriods(DbContextOptionsBuilder)"/>
    public static DbContextOptionsBuilder<TContext> UseTemporalRetentionPeriods<TContext>(
        this DbContextOptionsBuilder<TContext> optionsBuilder)
        where TContext : DbContext
    {
        ((DbContextOptionsBuilder)optionsBuilder).UseTemporalRetentionPeriods();
        return optionsBuilder;
    }
}
