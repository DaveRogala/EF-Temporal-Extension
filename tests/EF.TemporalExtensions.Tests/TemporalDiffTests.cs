using EF.TemporalExtensions;
using FluentAssertions;

namespace EF.TemporalExtensions.Tests;

public sealed class TemporalDiffTests
{
    private static readonly DateTime T0 = new(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime T1 = new(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    private sealed class FakeEntity { public int Id { get; set; } }

    [Fact]
    public void ChangeType_NullBefore_IsCreated()
    {
        var diff = new TemporalDiff<FakeEntity>(null, new FakeEntity(), T0, T1);

        diff.ChangeType.Should().Be(TemporalChangeType.Created);
        diff.IsCreated.Should().BeTrue();
        diff.IsDeleted.Should().BeFalse();
        diff.IsModified.Should().BeFalse();
    }

    [Fact]
    public void ChangeType_NullAfter_IsDeleted()
    {
        var diff = new TemporalDiff<FakeEntity>(new FakeEntity(), null, T0, T1);

        diff.ChangeType.Should().Be(TemporalChangeType.Deleted);
        diff.IsDeleted.Should().BeTrue();
    }

    [Fact]
    public void ChangeType_BothPresent_IsModified()
    {
        var diff = new TemporalDiff<FakeEntity>(new FakeEntity(), new FakeEntity(), T0, T1);

        diff.ChangeType.Should().Be(TemporalChangeType.Modified);
        diff.IsModified.Should().BeTrue();
    }

    [Fact]
    public void Constructor_AfterBeforeFrom_ThrowsArgumentOutOfRangeException()
    {
        var act = () => new TemporalDiff<FakeEntity>(null, null, T1, T0);

        act.Should().Throw<ArgumentOutOfRangeException>()
           .WithParameterName("afterAt");
    }

    [Fact]
    public void Constructor_SameInstant_Succeeds()
    {
        var act = () => new TemporalDiff<FakeEntity>(null, null, T0, T0);

        act.Should().NotThrow();
    }

    [Fact]
    public void ToString_ContainsChangeType()
    {
        var diff = new TemporalDiff<FakeEntity>(null, new FakeEntity(), T0, T1);

        diff.ToString().Should().Contain("Created");
    }
}
