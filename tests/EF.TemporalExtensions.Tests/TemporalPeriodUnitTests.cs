using EF.TemporalExtensions;
using FluentAssertions;

namespace EF.TemporalExtensions.Tests;

/// <summary>
/// Tests for <see cref="TemporalPeriodUnit"/> SQL keyword mapping.
/// </summary>
public sealed class TemporalPeriodUnitTests
{
    [Theory]
    [InlineData(TemporalPeriodUnit.Day,      "DAYS")]
    [InlineData(TemporalPeriodUnit.Week,     "WEEKS")]
    [InlineData(TemporalPeriodUnit.Month,    "MONTHS")]
    [InlineData(TemporalPeriodUnit.Year,     "YEARS")]
    [InlineData(TemporalPeriodUnit.Infinite, "INFINITE")]
    public void ToSqlKeyword_ReturnsCorrectKeyword(TemporalPeriodUnit unit, string expected)
    {
        unit.ToSqlKeyword().Should().Be(expected);
    }

    [Theory]
    [InlineData("DAY",      TemporalPeriodUnit.Day)]
    [InlineData("DAYS",     TemporalPeriodUnit.Day)]
    [InlineData("day",      TemporalPeriodUnit.Day)]
    [InlineData("WEEK",     TemporalPeriodUnit.Week)]
    [InlineData("WEEKS",    TemporalPeriodUnit.Week)]
    [InlineData("MONTH",    TemporalPeriodUnit.Month)]
    [InlineData("MONTHS",   TemporalPeriodUnit.Month)]
    [InlineData("YEAR",     TemporalPeriodUnit.Year)]
    [InlineData("YEARS",    TemporalPeriodUnit.Year)]
    [InlineData("INFINITE", TemporalPeriodUnit.Infinite)]
    [InlineData("infinite", TemporalPeriodUnit.Infinite)]
    public void FromSqlKeyword_ReturnsCorrectUnit(string keyword, TemporalPeriodUnit expected)
    {
        TemporalPeriodUnitExtensions.FromSqlKeyword(keyword).Should().Be(expected);
    }

    [Fact]
    public void FromSqlKeyword_UnknownKeyword_ThrowsArgumentException()
    {
        var act = () => TemporalPeriodUnitExtensions.FromSqlKeyword("DECADE");
        act.Should().Throw<ArgumentException>();
    }
}
