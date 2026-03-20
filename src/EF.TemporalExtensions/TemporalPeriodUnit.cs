namespace EF.TemporalExtensions;

/// <summary>
/// The time unit used when specifying a temporal history retention period.
/// Maps to the SQL Server <c>HISTORY_RETENTION_PERIOD</c> unit keywords.
/// </summary>
public enum TemporalPeriodUnit
{
    /// <summary>Retain history indefinitely. SQL: <c>INFINITE</c>.</summary>
    Infinite,

    /// <summary>Retain history for N days. SQL: <c>N DAYS</c>.</summary>
    Day,

    /// <summary>Retain history for N weeks. SQL: <c>N WEEKS</c>.</summary>
    Week,

    /// <summary>Retain history for N months. SQL: <c>N MONTHS</c>.</summary>
    Month,

    /// <summary>Retain history for N years. SQL: <c>N YEARS</c>.</summary>
    Year
}

internal static class TemporalPeriodUnitExtensions
{
    /// <summary>Returns the SQL Server keyword for the unit (e.g. <c>MONTHS</c>).</summary>
    internal static string ToSqlKeyword(this TemporalPeriodUnit unit) => unit switch
    {
        TemporalPeriodUnit.Day      => "DAYS",
        TemporalPeriodUnit.Week     => "WEEKS",
        TemporalPeriodUnit.Month    => "MONTHS",
        TemporalPeriodUnit.Year     => "YEARS",
        TemporalPeriodUnit.Infinite => "INFINITE",
        _ => throw new ArgumentOutOfRangeException(nameof(unit), unit, null)
    };

    internal static TemporalPeriodUnit FromSqlKeyword(string keyword) => keyword.ToUpperInvariant() switch
    {
        "DAY"      or "DAYS"      => TemporalPeriodUnit.Day,
        "WEEK"     or "WEEKS"     => TemporalPeriodUnit.Week,
        "MONTH"    or "MONTHS"    => TemporalPeriodUnit.Month,
        "YEAR"     or "YEARS"     => TemporalPeriodUnit.Year,
        "INFINITE"                => TemporalPeriodUnit.Infinite,
        _ => throw new ArgumentException($"Unknown temporal period unit keyword: '{keyword}'", nameof(keyword))
    };
}
