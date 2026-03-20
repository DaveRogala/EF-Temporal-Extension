namespace EF.TemporalExtensions;

/// <summary>
/// Represents an immutable time interval used to scope temporal queries.
/// Both <see cref="Start"/> and <see cref="End"/> are stored in UTC.
/// </summary>
public sealed class TemporalPeriod : IEquatable<TemporalPeriod>
{
    /// <summary>Start of the period (inclusive).</summary>
    public DateTime Start { get; }

    /// <summary>End of the period (exclusive for FROM-TO queries, inclusive for BETWEEN queries).</summary>
    public DateTime End { get; }

    /// <summary>Duration of the period.</summary>
    public TimeSpan Duration => End - Start;

    /// <summary>Returns <c>true</c> when the period represents a single instant (Start == End).</summary>
    public bool IsInstant => Start == End;

    /// <param name="start">Start of period (UTC).</param>
    /// <param name="end">End of period (UTC). Must be &gt;= <paramref name="start"/>.</param>
    public TemporalPeriod(DateTime start, DateTime end)
    {
        if (start.Kind == DateTimeKind.Local || end.Kind == DateTimeKind.Local)
            throw new ArgumentException("Period boundaries must be UTC or Unspecified DateTimeKind.");

        if (end < start)
            throw new ArgumentOutOfRangeException(nameof(end), "End must be greater than or equal to Start.");

        Start = start;
        End = end;
    }

    /// <summary>Creates a period spanning a single point in time.</summary>
    public static TemporalPeriod At(DateTime point) => new(point, point);

    /// <summary>Creates a period from <paramref name="start"/> to <paramref name="end"/>.</summary>
    public static TemporalPeriod Between(DateTime start, DateTime end) => new(start, end);

    /// <summary>Creates a period from <paramref name="start"/> lasting <paramref name="duration"/>.</summary>
    public static TemporalPeriod Starting(DateTime start, TimeSpan duration) => new(start, start + duration);

    /// <summary>Returns <c>true</c> when <paramref name="point"/> falls within [Start, End].</summary>
    public bool Contains(DateTime point) => point >= Start && point <= End;

    /// <summary>Returns <c>true</c> when this period fully contains <paramref name="other"/>.</summary>
    public bool Contains(TemporalPeriod other) => other.Start >= Start && other.End <= End;

    /// <summary>Returns <c>true</c> when this period overlaps <paramref name="other"/>.</summary>
    public bool Overlaps(TemporalPeriod other) => Start < other.End && End > other.Start;

    /// <summary>Returns the intersection of this period and <paramref name="other"/>, or <c>null</c> when they do not overlap.</summary>
    public TemporalPeriod? Intersect(TemporalPeriod other)
    {
        var start = Start > other.Start ? Start : other.Start;
        var end   = End   < other.End   ? End   : other.End;
        return start <= end ? new TemporalPeriod(start, end) : null;
    }

    /// <inheritdoc/>
    public bool Equals(TemporalPeriod? other) =>
        other is not null && Start == other.Start && End == other.End;

    /// <inheritdoc/>
    public override bool Equals(object? obj) => Equals(obj as TemporalPeriod);

    /// <inheritdoc/>
    public override int GetHashCode() => HashCode.Combine(Start, End);

    /// <inheritdoc/>
    public override string ToString() => $"[{Start:O} → {End:O}]";

    /// <summary>Deconstructs the period into its <paramref name="start"/> and <paramref name="end"/> components.</summary>
    public void Deconstruct(out DateTime start, out DateTime end) => (start, end) = (Start, End);
}
