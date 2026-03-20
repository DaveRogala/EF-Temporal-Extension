using EF.TemporalExtensions;
using FluentAssertions;

namespace EF.TemporalExtensions.Tests;

public sealed class TemporalPeriodTests
{
    private static readonly DateTime T0 = new(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime T1 = new(2024, 6, 1, 0, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime T2 = new(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    // -------------------------------------------------------------------------
    // Construction
    // -------------------------------------------------------------------------

    [Fact]
    public void Constructor_ValidRange_SetsStartAndEnd()
    {
        var period = new TemporalPeriod(T0, T2);

        period.Start.Should().Be(T0);
        period.End.Should().Be(T2);
    }

    [Fact]
    public void Constructor_EndBeforeStart_ThrowsArgumentOutOfRangeException()
    {
        var act = () => new TemporalPeriod(T2, T0);

        act.Should().Throw<ArgumentOutOfRangeException>()
           .WithParameterName("end");
    }

    [Fact]
    public void Constructor_LocalDateTimeKind_ThrowsArgumentException()
    {
        var local = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Local);
        var act   = () => new TemporalPeriod(local, T2);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void At_CreatesInstantPeriod()
    {
        var period = TemporalPeriod.At(T1);

        period.IsInstant.Should().BeTrue();
        period.Start.Should().Be(T1);
        period.End.Should().Be(T1);
        period.Duration.Should().Be(TimeSpan.Zero);
    }

    [Fact]
    public void Starting_CreatesPeriodOfGivenDuration()
    {
        var duration = TimeSpan.FromDays(30);
        var period   = TemporalPeriod.Starting(T0, duration);

        period.Duration.Should().Be(duration);
        period.End.Should().Be(T0 + duration);
    }

    // -------------------------------------------------------------------------
    // Contains
    // -------------------------------------------------------------------------

    [Theory]
    [InlineData(true,  "2024-01-01")] // exactly at start
    [InlineData(true,  "2024-06-01")] // in the middle
    [InlineData(true,  "2025-01-01")] // exactly at end
    [InlineData(false, "2023-12-31")] // before start
    [InlineData(false, "2025-01-02")] // after end
    public void Contains_Point_ReturnsExpected(bool expected, string dateStr)
    {
        var period = new TemporalPeriod(T0, T2);
        var point  = DateTime.Parse(dateStr, null, System.Globalization.DateTimeStyles.AssumeUniversal)
                             .ToUniversalTime();

        period.Contains(point).Should().Be(expected);
    }

    [Fact]
    public void Contains_SubPeriod_ReturnsTrue()
    {
        var outer = new TemporalPeriod(T0, T2);
        var inner = new TemporalPeriod(T0, T1);

        outer.Contains(inner).Should().BeTrue();
    }

    [Fact]
    public void Contains_OverlappingPeriod_ReturnsFalse()
    {
        var a = new TemporalPeriod(T0, T1);
        var b = new TemporalPeriod(T1, T2);

        // b starts where a ends — b is NOT fully contained within a
        a.Contains(b).Should().BeFalse();
    }

    // -------------------------------------------------------------------------
    // Overlaps
    // -------------------------------------------------------------------------

    [Fact]
    public void Overlaps_AdjacentPeriods_ReturnsFalse()
    {
        var a = new TemporalPeriod(T0, T1);
        var b = new TemporalPeriod(T1, T2);

        a.Overlaps(b).Should().BeFalse();
    }

    [Fact]
    public void Overlaps_OverlappingPeriods_ReturnsTrue()
    {
        var mid = new DateTime(2024, 3, 1, 0, 0, 0, DateTimeKind.Utc);
        var a   = new TemporalPeriod(T0, mid);
        var b   = new TemporalPeriod(T1, T2);

        // mid < T1 means a and b share [T1, mid)? No — mid is before T1, so no overlap.
        // Use a period that actually overlaps:
        var c = new TemporalPeriod(T0, T2);
        var d = new TemporalPeriod(T1, T2);

        c.Overlaps(d).Should().BeTrue();
    }

    // -------------------------------------------------------------------------
    // Intersect
    // -------------------------------------------------------------------------

    [Fact]
    public void Intersect_OverlappingPeriods_ReturnsIntersection()
    {
        var a          = new TemporalPeriod(T0, T2);
        var b          = new TemporalPeriod(T1, T2);
        var intersect  = a.Intersect(b);

        intersect.Should().NotBeNull();
        intersect!.Start.Should().Be(T1);
        intersect.End.Should().Be(T2);
    }

    [Fact]
    public void Intersect_NonOverlappingPeriods_ReturnsNull()
    {
        var a = new TemporalPeriod(T0, T1);
        var b = new TemporalPeriod(T2, T2 + TimeSpan.FromDays(1));

        a.Intersect(b).Should().BeNull();
    }

    // -------------------------------------------------------------------------
    // Equality
    // -------------------------------------------------------------------------

    [Fact]
    public void Equals_SameBoundaries_ReturnsTrue()
    {
        var a = new TemporalPeriod(T0, T2);
        var b = new TemporalPeriod(T0, T2);

        a.Should().Be(b);
        a.GetHashCode().Should().Be(b.GetHashCode());
    }

    [Fact]
    public void Equals_DifferentBoundaries_ReturnsFalse()
    {
        var a = new TemporalPeriod(T0, T1);
        var b = new TemporalPeriod(T0, T2);

        a.Should().NotBe(b);
    }

    // -------------------------------------------------------------------------
    // Deconstruct
    // -------------------------------------------------------------------------

    [Fact]
    public void Deconstruct_YieldsStartAndEnd()
    {
        var period = new TemporalPeriod(T0, T2);
        var (start, end) = period;

        start.Should().Be(T0);
        end.Should().Be(T2);
    }
}
