namespace EF.TemporalExtensions;

/// <summary>
/// Pairs an entity instance with the temporal period during which that version was valid.
/// </summary>
/// <typeparam name="TEntity">The entity type.</typeparam>
public sealed class TemporalSnapshot<TEntity> where TEntity : class
{
    /// <summary>The entity as it existed during <see cref="Period"/>.</summary>
    public TEntity Entity { get; }

    /// <summary>The validity period of this version of the entity.</summary>
    public TemporalPeriod Period { get; }

    /// <summary>Convenience accessor for <see cref="TemporalPeriod.Start"/>.</summary>
    public DateTime ValidFrom => Period.Start;

    /// <summary>Convenience accessor for <see cref="TemporalPeriod.End"/>.</summary>
    public DateTime ValidTo => Period.End;

    /// <param name="entity">The entity instance.</param>
    /// <param name="validFrom">Start of the validity period (UTC).</param>
    /// <param name="validTo">End of the validity period (UTC).</param>
    public TemporalSnapshot(TEntity entity, DateTime validFrom, DateTime validTo)
    {
        Entity = entity ?? throw new ArgumentNullException(nameof(entity));
        Period = new TemporalPeriod(validFrom, validTo);
    }

    /// <param name="entity">The entity instance.</param>
    /// <param name="period">The validity period.</param>
    public TemporalSnapshot(TEntity entity, TemporalPeriod period)
    {
        Entity = entity ?? throw new ArgumentNullException(nameof(entity));
        Period = period ?? throw new ArgumentNullException(nameof(period));
    }

    /// <summary>Deconstructs the snapshot into its entity and period.</summary>
    public void Deconstruct(out TEntity entity, out TemporalPeriod period) =>
        (entity, period) = (Entity, Period);

    /// <inheritdoc/>
    public override string ToString() => $"{typeof(TEntity).Name} {Period}";
}
