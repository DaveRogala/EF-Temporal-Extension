using EF.TemporalExtensions;
using FluentAssertions;

namespace EF.TemporalExtensions.Tests;

public sealed class TemporalSnapshotTests
{
    private static readonly DateTime T0 = new(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime T1 = new(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    private sealed class FakeEntity { public int Id { get; set; } = 1; }

    [Fact]
    public void Constructor_SetsEntityAndPeriod()
    {
        var entity   = new FakeEntity();
        var snapshot = new TemporalSnapshot<FakeEntity>(entity, T0, T1);

        snapshot.Entity.Should().BeSameAs(entity);
        snapshot.ValidFrom.Should().Be(T0);
        snapshot.ValidTo.Should().Be(T1);
    }

    [Fact]
    public void Constructor_NullEntity_ThrowsArgumentNullException()
    {
        var act = () => new TemporalSnapshot<FakeEntity>(null!, T0, T1);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Constructor_WithPeriod_SetsPeriod()
    {
        var entity   = new FakeEntity();
        var period   = new TemporalPeriod(T0, T1);
        var snapshot = new TemporalSnapshot<FakeEntity>(entity, period);

        snapshot.Period.Should().Be(period);
    }

    [Fact]
    public void Deconstruct_YieldsEntityAndPeriod()
    {
        var entity   = new FakeEntity();
        var period   = new TemporalPeriod(T0, T1);
        var snapshot = new TemporalSnapshot<FakeEntity>(entity, period);

        var (e, p) = snapshot;

        e.Should().BeSameAs(entity);
        p.Should().Be(period);
    }

    [Fact]
    public void ToString_ContainsTypeName()
    {
        var snapshot = new TemporalSnapshot<FakeEntity>(new FakeEntity(), T0, T1);

        snapshot.ToString().Should().Contain("FakeEntity");
    }
}
