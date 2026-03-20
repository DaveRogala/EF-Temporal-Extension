namespace EF.TemporalExtensions;

/// <summary>
/// Represents a SQL Server temporal history retention period.
/// Stored as an EF Core annotation value; serialized as <c>"VALUE|UNIT"</c>
/// (e.g. <c>"6|Month"</c>) or <c>"INFINITE"</c>.
/// </summary>
internal sealed record TemporalRetentionPeriod(int Value, TemporalPeriodUnit Unit)
{
    private const string InfiniteToken = "INFINITE";
    private const char   Separator     = '|';

    /// <summary>Returns the SQL Server DDL fragment, e.g. <c>6 MONTHS</c> or <c>INFINITE</c>.</summary>
    internal string ToSqlFragment() => Unit == TemporalPeriodUnit.Infinite
        ? InfiniteToken
        : $"{Value} {Unit.ToSqlKeyword()}";

    /// <summary>Serialises the retention period to an annotation-safe string.</summary>
    internal string Serialize() => Unit == TemporalPeriodUnit.Infinite
        ? InfiniteToken
        : $"{Value}{Separator}{Unit}";

    /// <summary>Deserialises a retention period from its annotation string.</summary>
    /// <exception cref="FormatException">Thrown when the string is not a valid serialized period.</exception>
    internal static TemporalRetentionPeriod Deserialize(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new FormatException("Retention period string must not be null or empty.");

        if (value.Equals(InfiniteToken, StringComparison.OrdinalIgnoreCase))
            return new TemporalRetentionPeriod(0, TemporalPeriodUnit.Infinite);

        var parts = value.Split(Separator);
        if (parts.Length != 2 || !int.TryParse(parts[0], out var numericValue) || numericValue <= 0)
            throw new FormatException($"Invalid temporal retention period format: '{value}'. Expected 'VALUE|UNIT' or 'INFINITE'.");

        if (!Enum.TryParse<TemporalPeriodUnit>(parts[1], ignoreCase: true, out var unit))
            throw new FormatException($"Unknown temporal period unit '{parts[1]}' in '{value}'.");

        return new TemporalRetentionPeriod(numericValue, unit);
    }
}
