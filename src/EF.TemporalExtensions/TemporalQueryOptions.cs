namespace EF.TemporalExtensions;

/// <summary>
/// Configuration options that control how temporal queries are executed.
/// Passed to higher-level helpers that build composite queries.
/// </summary>
public sealed class TemporalQueryOptions
{
    /// <summary>
    /// Default instance with sensible defaults.
    /// </summary>
    public static TemporalQueryOptions Default { get; } = new();

    /// <summary>
    /// Maximum number of history rows to return per entity.
    /// <c>0</c> or negative means unlimited (default: <c>0</c>).
    /// </summary>
    public int MaxVersions { get; init; } = 0;

    /// <summary>
    /// When <c>true</c>, results are ordered from newest to oldest.
    /// Default: <c>false</c> (oldest first).
    /// </summary>
    public bool DescendingOrder { get; init; } = false;

    /// <summary>
    /// When set, restricts temporal queries to this period.
    /// </summary>
    public TemporalPeriod? Period { get; init; }

    /// <summary>
    /// When <c>true</c>, every <see cref="DateTime"/> passed to the library is automatically
    /// converted to UTC before being forwarded to EF Core.
    /// Default: <c>true</c>.
    /// </summary>
    public bool NormalizeToUtc { get; init; } = true;

    /// <summary>
    /// Applies <see cref="MaxVersions"/> and <see cref="DescendingOrder"/> to an ordered
    /// <see cref="IQueryable{TEntity}"/>.
    /// </summary>
    internal IQueryable<TEntity> Apply<TEntity>(IQueryable<TEntity> query)
        where TEntity : class
    {
        if (MaxVersions > 0)
            query = query.Take(MaxVersions);

        return query;
    }
}
