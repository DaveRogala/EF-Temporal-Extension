namespace EF.TemporalExtensions;

/// <summary>
/// Represents the difference between two temporal states of an entity.
/// </summary>
/// <typeparam name="TEntity">The entity type.</typeparam>
public sealed class TemporalDiff<TEntity> where TEntity : class
{
    /// <summary>The entity state at the earlier point in time, or <c>null</c> if it didn't exist yet.</summary>
    public TEntity? Before { get; }

    /// <summary>The entity state at the later point in time, or <c>null</c> if it was deleted.</summary>
    public TEntity? After { get; }

    /// <summary>The point in time for the <see cref="Before"/> state.</summary>
    public DateTime BeforeAt { get; }

    /// <summary>The point in time for the <see cref="After"/> state.</summary>
    public DateTime AfterAt { get; }

    /// <summary>Returns <c>true</c> when the entity was created between the two points.</summary>
    public bool IsCreated => Before is null && After is not null;

    /// <summary>Returns <c>true</c> when the entity was deleted between the two points.</summary>
    public bool IsDeleted => Before is not null && After is null;

    /// <summary>Returns <c>true</c> when the entity existed at both points but may have changed.</summary>
    public bool IsModified => Before is not null && After is not null;

    /// <summary>The type of change represented by this diff.</summary>
    public TemporalChangeType ChangeType => (Before, After) switch
    {
        (null, not null) => TemporalChangeType.Created,
        (not null, null) => TemporalChangeType.Deleted,
        _                => TemporalChangeType.Modified
    };

    /// <param name="before">Entity state at <paramref name="beforeAt"/>, or <c>null</c>.</param>
    /// <param name="after">Entity state at <paramref name="afterAt"/>, or <c>null</c>.</param>
    /// <param name="beforeAt">Point in time of the before state (UTC).</param>
    /// <param name="afterAt">Point in time of the after state (UTC). Must be &gt;= <paramref name="beforeAt"/>.</param>
    public TemporalDiff(TEntity? before, TEntity? after, DateTime beforeAt, DateTime afterAt)
    {
        if (afterAt < beforeAt)
            throw new ArgumentOutOfRangeException(nameof(afterAt), "AfterAt must be >= BeforeAt.");

        Before   = before;
        After    = after;
        BeforeAt = beforeAt;
        AfterAt  = afterAt;
    }

    /// <inheritdoc/>
    public override string ToString() =>
        $"{typeof(TEntity).Name} {ChangeType} ({BeforeAt:O} → {AfterAt:O})";
}

/// <summary>Categorises the type of change in a <see cref="TemporalDiff{TEntity}"/>.</summary>
public enum TemporalChangeType
{
    /// <summary>The entity was created.</summary>
    Created,

    /// <summary>The entity was modified.</summary>
    Modified,

    /// <summary>The entity was deleted.</summary>
    Deleted
}
