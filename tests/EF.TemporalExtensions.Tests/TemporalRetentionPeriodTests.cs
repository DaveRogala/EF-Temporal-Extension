using EF.TemporalExtensions;
using FluentAssertions;

namespace EF.TemporalExtensions.Tests;

/// <summary>
/// Tests for <see cref="TemporalRetentionPeriod"/> serialization, deserialization,
/// and SQL fragment generation.
/// </summary>
public sealed class TemporalRetentionPeriodTests
{
    // -------------------------------------------------------------------------
    // ToSqlFragment
    // -------------------------------------------------------------------------

    [Theory]
    [InlineData(6,  TemporalPeriodUnit.Month,  "6 MONTHS")]
    [InlineData(1,  TemporalPeriodUnit.Month,  "1 MONTHS")]
    [InlineData(30, TemporalPeriodUnit.Day,    "30 DAYS")]
    [InlineData(2,  TemporalPeriodUnit.Week,   "2 WEEKS")]
    [InlineData(1,  TemporalPeriodUnit.Year,   "1 YEARS")]
    [InlineData(0,  TemporalPeriodUnit.Infinite, "INFINITE")]
    public void ToSqlFragment_ReturnsCorrectSql(int value, TemporalPeriodUnit unit, string expected)
    {
        var period = new TemporalRetentionPeriod(value, unit);
        period.ToSqlFragment().Should().Be(expected);
    }

    // -------------------------------------------------------------------------
    // Serialize / Deserialize round-trip
    // -------------------------------------------------------------------------

    [Theory]
    [InlineData(6,  TemporalPeriodUnit.Month)]
    [InlineData(30, TemporalPeriodUnit.Day)]
    [InlineData(2,  TemporalPeriodUnit.Week)]
    [InlineData(5,  TemporalPeriodUnit.Year)]
    [InlineData(0,  TemporalPeriodUnit.Infinite)]
    public void SerializeDeserialize_RoundTrip_IsEqual(int value, TemporalPeriodUnit unit)
    {
        var original   = new TemporalRetentionPeriod(value, unit);
        var serialized = original.Serialize();
        var restored   = TemporalRetentionPeriod.Deserialize(serialized);

        restored.Should().Be(original);
    }

    [Fact]
    public void Deserialize_Infinite_CaseInsensitive_Succeeds()
    {
        var result = TemporalRetentionPeriod.Deserialize("infinite");
        result.Unit.Should().Be(TemporalPeriodUnit.Infinite);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("abc|Month")]
    [InlineData("0|Month")]
    [InlineData("-1|Month")]
    [InlineData("6|Decade")]
    [InlineData("6")]
    public void Deserialize_InvalidInput_ThrowsFormatException(string input)
    {
        var act = () => TemporalRetentionPeriod.Deserialize(input);
        act.Should().Throw<FormatException>();
    }
}
