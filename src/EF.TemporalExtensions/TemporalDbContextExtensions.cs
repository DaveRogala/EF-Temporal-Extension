using Microsoft.EntityFrameworkCore;

namespace EF.TemporalExtensions;

/// <summary>
/// Extension methods on <see cref="DbContext"/> for executing common temporal operations.
/// </summary>
public static class TemporalDbContextExtensions
{
    /// <summary>
    /// Retrieves the full audit trail for a single entity identified by <paramref name="key"/>,
    /// returned as an ordered sequence of <see cref="TemporalSnapshot{TEntity}"/>.
    /// </summary>
    /// <typeparam name="TEntity">The entity type (must be registered in the context).</typeparam>
    /// <typeparam name="TKey">The primary key type.</typeparam>
    /// <param name="context">The database context.</param>
    /// <param name="keySelector">Selects the primary key property.</param>
    /// <param name="key">The primary key value.</param>
    /// <param name="periodStartSelector">Selects the period-start column from the entity.</param>
    /// <param name="periodEndSelector">Selects the period-end column from the entity.</param>
    /// <param name="period">Optional time window to constrain the audit trail.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>Chronologically ordered snapshots.</returns>
    public static async Task<IReadOnlyList<TemporalSnapshot<TEntity>>> GetAuditTrailAsync<TEntity, TKey>(
        this DbContext context,
        System.Linq.Expressions.Expression<Func<TEntity, TKey>> keySelector,
        TKey key,
        System.Linq.Expressions.Expression<Func<TEntity, DateTime>> periodStartSelector,
        System.Linq.Expressions.Expression<Func<TEntity, DateTime>> periodEndSelector,
        TemporalPeriod? period = null,
        CancellationToken cancellationToken = default)
        where TEntity : class
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(keySelector);
        ArgumentNullException.ThrowIfNull(periodStartSelector);
        ArgumentNullException.ThrowIfNull(periodEndSelector);

        var startFn = periodStartSelector.Compile();
        var endFn   = periodEndSelector.Compile();

        var rows = await context.Set<TEntity>()
            .HistoryFor(keySelector, key, period)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return rows
            .Select(e => new TemporalSnapshot<TEntity>(e, startFn(e), endFn(e)))
            .OrderBy(s => s.ValidFrom)
            .ToList()
            .AsReadOnly();
    }

    /// <summary>
    /// Returns the entity as it existed at <paramref name="pointInTime"/>, or <c>null</c>
    /// when no version was active at that instant.
    /// </summary>
    /// <typeparam name="TEntity">The entity type.</typeparam>
    /// <typeparam name="TKey">The primary key type.</typeparam>
    /// <param name="context">The database context.</param>
    /// <param name="keySelector">Selects the primary key property.</param>
    /// <param name="key">The primary key value.</param>
    /// <param name="pointInTime">The UTC instant to query as-of.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    public static Task<TEntity?> FindAsOfAsync<TEntity, TKey>(
        this DbContext context,
        System.Linq.Expressions.Expression<Func<TEntity, TKey>> keySelector,
        TKey key,
        DateTime pointInTime,
        CancellationToken cancellationToken = default)
        where TEntity : class
    {
        ArgumentNullException.ThrowIfNull(context);

        var param     = keySelector.Parameters[0];
        var body      = System.Linq.Expressions.Expression.Equal(
                            keySelector.Body,
                            System.Linq.Expressions.Expression.Constant(key, typeof(TKey)));
        var predicate = System.Linq.Expressions.Expression.Lambda<Func<TEntity, bool>>(body, param);

        return context.Set<TEntity>()
            .AsOf(pointInTime)
            .Where(predicate)
            .FirstOrDefaultAsync(cancellationToken);
    }

    /// <summary>
    /// Computes a <see cref="TemporalDiff{TEntity}"/> for a single entity between two points in time.
    /// </summary>
    /// <typeparam name="TEntity">The entity type.</typeparam>
    /// <typeparam name="TKey">The primary key type.</typeparam>
    /// <param name="context">The database context.</param>
    /// <param name="keySelector">Selects the primary key property.</param>
    /// <param name="key">The primary key value.</param>
    /// <param name="from">The earlier UTC instant.</param>
    /// <param name="to">The later UTC instant.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    public static Task<TemporalDiff<TEntity>> GetDiffAsync<TEntity, TKey>(
        this DbContext context,
        System.Linq.Expressions.Expression<Func<TEntity, TKey>> keySelector,
        TKey key,
        DateTime from,
        DateTime to,
        CancellationToken cancellationToken = default)
        where TEntity : class
    {
        ArgumentNullException.ThrowIfNull(context);

        return context.Set<TEntity>()
            .GetDiffAsync(keySelector, key, from, to, cancellationToken);
    }

    /// <summary>
    /// Returns the number of distinct versions that existed for the entity identified by
    /// <paramref name="key"/> within the optional <paramref name="period"/>.
    /// </summary>
    /// <typeparam name="TEntity">The entity type.</typeparam>
    /// <typeparam name="TKey">The primary key type.</typeparam>
    /// <param name="context">The database context.</param>
    /// <param name="keySelector">Selects the primary key property.</param>
    /// <param name="key">The primary key value.</param>
    /// <param name="period">Optional period constraint. <c>null</c> counts all versions.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    public static Task<int> CountVersionsAsync<TEntity, TKey>(
        this DbContext context,
        System.Linq.Expressions.Expression<Func<TEntity, TKey>> keySelector,
        TKey key,
        TemporalPeriod? period = null,
        CancellationToken cancellationToken = default)
        where TEntity : class
    {
        ArgumentNullException.ThrowIfNull(context);

        return context.Set<TEntity>()
            .HistoryFor(keySelector, key, period)
            .CountAsync(cancellationToken);
    }
}
