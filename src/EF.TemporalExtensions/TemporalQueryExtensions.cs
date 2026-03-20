using Microsoft.EntityFrameworkCore;

namespace EF.TemporalExtensions;

/// <summary>
/// Fluent extension methods for querying EF Core temporal (system-versioned) tables.
/// All methods delegate to the corresponding built-in EF Core temporal operators.
/// </summary>
public static class TemporalQueryExtensions
{
    // -------------------------------------------------------------------------
    // Core temporal operators
    // -------------------------------------------------------------------------

    /// <summary>
    /// Returns rows that were active at <paramref name="pointInTime"/>.
    /// Maps to SQL: <c>FOR SYSTEM_TIME AS OF @point</c>.
    /// </summary>
    /// <param name="source">The entity set.</param>
    /// <param name="pointInTime">The UTC instant to query as-of.</param>
    public static IQueryable<TEntity> AsOf<TEntity>(
        this IQueryable<TEntity> source,
        DateTime pointInTime)
        where TEntity : class
        => source.TemporalAsOf(pointInTime.ToUniversalTime());

    /// <summary>
    /// Returns rows that were active at the instant described by <paramref name="period"/>.
    /// Requires <see cref="TemporalPeriod.IsInstant"/> to be <c>true</c>.
    /// </summary>
    public static IQueryable<TEntity> AsOf<TEntity>(
        this IQueryable<TEntity> source,
        TemporalPeriod period)
        where TEntity : class
    {
        if (!period.IsInstant)
            throw new ArgumentException("Period must represent a single instant (Start == End) for AsOf queries.", nameof(period));

        return source.TemporalAsOf(period.Start);
    }

    /// <summary>
    /// Returns all rows whose validity period starts at or after <paramref name="from"/>
    /// and before <paramref name="to"/> (exclusive).
    /// Maps to SQL: <c>FOR SYSTEM_TIME FROM @from TO @to</c>.
    /// </summary>
    public static IQueryable<TEntity> FromTo<TEntity>(
        this IQueryable<TEntity> source,
        DateTime from,
        DateTime to)
        where TEntity : class
        => source.TemporalFromTo(from.ToUniversalTime(), to.ToUniversalTime());

    /// <summary>
    /// Returns all rows whose validity period starts at or after <c>period.Start</c>
    /// and before <c>period.End</c> (exclusive).
    /// </summary>
    public static IQueryable<TEntity> FromTo<TEntity>(
        this IQueryable<TEntity> source,
        TemporalPeriod period)
        where TEntity : class
        => source.TemporalFromTo(period.Start, period.End);

    /// <summary>
    /// Returns all rows that were active at any point between <paramref name="from"/>
    /// and <paramref name="to"/> (inclusive on both ends).
    /// Maps to SQL: <c>FOR SYSTEM_TIME BETWEEN @from AND @to</c>.
    /// </summary>
    public static IQueryable<TEntity> Between<TEntity>(
        this IQueryable<TEntity> source,
        DateTime from,
        DateTime to)
        where TEntity : class
        => source.TemporalBetween(from.ToUniversalTime(), to.ToUniversalTime());

    /// <summary>
    /// Returns all rows that were active at any point within <paramref name="period"/> (inclusive).
    /// </summary>
    public static IQueryable<TEntity> Between<TEntity>(
        this IQueryable<TEntity> source,
        TemporalPeriod period)
        where TEntity : class
        => source.TemporalBetween(period.Start, period.End);

    /// <summary>
    /// Returns rows whose entire validity period is contained within [<paramref name="from"/>, <paramref name="to"/>].
    /// Maps to SQL: <c>FOR SYSTEM_TIME CONTAINED IN (@from, @to)</c>.
    /// </summary>
    public static IQueryable<TEntity> ContainedIn<TEntity>(
        this IQueryable<TEntity> source,
        DateTime from,
        DateTime to)
        where TEntity : class
        => source.TemporalContainedIn(from.ToUniversalTime(), to.ToUniversalTime());

    /// <summary>
    /// Returns rows whose entire validity period is contained within <paramref name="period"/>.
    /// </summary>
    public static IQueryable<TEntity> ContainedIn<TEntity>(
        this IQueryable<TEntity> source,
        TemporalPeriod period)
        where TEntity : class
        => source.TemporalContainedIn(period.Start, period.End);

    /// <summary>
    /// Returns all rows from both the current and history tables.
    /// Maps to SQL: <c>FOR SYSTEM_TIME ALL</c>.
    /// </summary>
    public static IQueryable<TEntity> All<TEntity>(
        this IQueryable<TEntity> source)
        where TEntity : class
        => source.TemporalAll();

    // -------------------------------------------------------------------------
    // Snapshot helpers
    // -------------------------------------------------------------------------

    /// <summary>
    /// Projects each row into a <see cref="TemporalSnapshot{TEntity}"/> that includes
    /// the EF-tracked period columns (<c>PeriodStart</c> / <c>PeriodEnd</c>).
    /// </summary>
    /// <param name="source">A temporal query (e.g., the result of <see cref="All{TEntity}"/>).</param>
    /// <param name="periodStartSelector">Expression selecting the period-start column.</param>
    /// <param name="periodEndSelector">Expression selecting the period-end column.</param>
    public static IQueryable<TemporalSnapshot<TEntity>> WithSnapshot<TEntity>(
        this IQueryable<TEntity> source,
        System.Linq.Expressions.Expression<Func<TEntity, DateTime>> periodStartSelector,
        System.Linq.Expressions.Expression<Func<TEntity, DateTime>> periodEndSelector)
        where TEntity : class
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(periodStartSelector);
        ArgumentNullException.ThrowIfNull(periodEndSelector);

        var startFn = periodStartSelector.Compile();
        var endFn   = periodEndSelector.Compile();

        // NOTE: AsEnumerable() is required because TemporalSnapshot is not an EF-mapped type.
        return source
            .AsEnumerable()
            .Select(e => new TemporalSnapshot<TEntity>(e, startFn(e), endFn(e)))
            .AsQueryable();
    }

    // -------------------------------------------------------------------------
    // Audit / history helpers
    // -------------------------------------------------------------------------

    /// <summary>
    /// Returns all historical versions of a single entity identified by <paramref name="keySelector"/>
    /// within <paramref name="period"/>, ordered chronologically.
    /// </summary>
    /// <typeparam name="TEntity">The entity type.</typeparam>
    /// <typeparam name="TKey">The primary key type.</typeparam>
    /// <param name="source">The entity set.</param>
    /// <param name="keySelector">Selects the primary key property.</param>
    /// <param name="key">The key value to filter on.</param>
    /// <param name="period">The time window to search. Pass <c>null</c> to retrieve the entire history.</param>
    public static IQueryable<TEntity> HistoryFor<TEntity, TKey>(
        this IQueryable<TEntity> source,
        System.Linq.Expressions.Expression<Func<TEntity, TKey>> keySelector,
        TKey key,
        TemporalPeriod? period = null)
        where TEntity : class
    {
        var query = period is null
            ? source.TemporalAll()
            : source.TemporalBetween(period.Start, period.End);

        // Build a predicate: entity => entity.Key == key
        var param     = keySelector.Parameters[0];
        var body      = System.Linq.Expressions.Expression.Equal(
                            keySelector.Body,
                            System.Linq.Expressions.Expression.Constant(key, typeof(TKey)));
        var predicate = System.Linq.Expressions.Expression.Lambda<Func<TEntity, bool>>(body, param);

        return query.Where(predicate);
    }

    // -------------------------------------------------------------------------
    // Diff helpers
    // -------------------------------------------------------------------------

    /// <summary>
    /// Computes a <see cref="TemporalDiff{TEntity}"/> for an entity between two instants.
    /// Both queries are executed asynchronously in parallel.
    /// </summary>
    /// <typeparam name="TEntity">The entity type.</typeparam>
    /// <typeparam name="TKey">The primary key type.</typeparam>
    /// <param name="source">The entity set.</param>
    /// <param name="keySelector">Selects the primary key property.</param>
    /// <param name="key">The key value to locate.</param>
    /// <param name="from">The earlier instant (UTC).</param>
    /// <param name="to">The later instant (UTC).</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    public static async Task<TemporalDiff<TEntity>> GetDiffAsync<TEntity, TKey>(
        this IQueryable<TEntity> source,
        System.Linq.Expressions.Expression<Func<TEntity, TKey>> keySelector,
        TKey key,
        DateTime from,
        DateTime to,
        CancellationToken cancellationToken = default)
        where TEntity : class
    {
        if (to < from)
            throw new ArgumentOutOfRangeException(nameof(to), "to must be >= from.");

        var param     = keySelector.Parameters[0];
        var body      = System.Linq.Expressions.Expression.Equal(
                            keySelector.Body,
                            System.Linq.Expressions.Expression.Constant(key, typeof(TKey)));
        var predicate = System.Linq.Expressions.Expression.Lambda<Func<TEntity, bool>>(body, param);

        var fromUtc = from.ToUniversalTime();
        var toUtc   = to.ToUniversalTime();

        var beforeTask = source.TemporalAsOf(fromUtc).Where(predicate).FirstOrDefaultAsync(cancellationToken);
        var afterTask  = source.TemporalAsOf(toUtc).Where(predicate).FirstOrDefaultAsync(cancellationToken);

        await Task.WhenAll(beforeTask, afterTask).ConfigureAwait(false);

        return new TemporalDiff<TEntity>(
            before:   beforeTask.Result,
            after:    afterTask.Result,
            beforeAt: fromUtc,
            afterAt:  toUtc);
    }

    // -------------------------------------------------------------------------
    // Paging helpers
    // -------------------------------------------------------------------------

    /// <summary>
    /// Applies offset-based pagination to a temporal query.
    /// </summary>
    /// <param name="source">Any <see cref="IQueryable{T}"/>.</param>
    /// <param name="page">Zero-based page index.</param>
    /// <param name="pageSize">Number of records per page.</param>
    public static IQueryable<TEntity> PagedTemporal<TEntity>(
        this IQueryable<TEntity> source,
        int page,
        int pageSize)
        where TEntity : class
    {
        if (page < 0)
            throw new ArgumentOutOfRangeException(nameof(page), "Page must be >= 0.");
        if (pageSize <= 0)
            throw new ArgumentOutOfRangeException(nameof(pageSize), "Page size must be > 0.");

        return source.Skip(page * pageSize).Take(pageSize);
    }

    /// <summary>
    /// Returns <c>true</c> when any version of the entity identified by <paramref name="key"/>
    /// existed within <paramref name="period"/>.
    /// </summary>
    public static Task<bool> ExistedDuringAsync<TEntity, TKey>(
        this IQueryable<TEntity> source,
        System.Linq.Expressions.Expression<Func<TEntity, TKey>> keySelector,
        TKey key,
        TemporalPeriod period,
        CancellationToken cancellationToken = default)
        where TEntity : class
        => source
            .HistoryFor(keySelector, key, period)
            .AnyAsync(cancellationToken);
}
