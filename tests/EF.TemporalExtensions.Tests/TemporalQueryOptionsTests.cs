using EF.TemporalExtensions;
using FluentAssertions;

namespace EF.TemporalExtensions.Tests;

public sealed class TemporalQueryOptionsTests
{
    [Fact]
    public void Default_HasExpectedValues()
    {
        var options = TemporalQueryOptions.Default;

        options.MaxVersions.Should().Be(0);
        options.DescendingOrder.Should().BeFalse();
        options.Period.Should().BeNull();
        options.NormalizeToUtc.Should().BeTrue();
    }

    [Fact]
    public void InitProperties_CanBeCustomized()
    {
        var period  = new TemporalPeriod(
            new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc));

        var options = new TemporalQueryOptions
        {
            MaxVersions    = 10,
            DescendingOrder = true,
            Period          = period,
            NormalizeToUtc  = false
        };

        options.MaxVersions.Should().Be(10);
        options.DescendingOrder.Should().BeTrue();
        options.Period.Should().Be(period);
        options.NormalizeToUtc.Should().BeFalse();
    }
}
