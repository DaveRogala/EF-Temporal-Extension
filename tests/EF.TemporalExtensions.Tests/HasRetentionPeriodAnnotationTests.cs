using EF.TemporalExtensions;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace EF.TemporalExtensions.Tests;

/// <summary>
/// Verifies that calling <c>HasRetentionPeriod</c> sets the expected annotation
/// on the entity type in the EF Core model.
/// </summary>
public sealed class HasRetentionPeriodAnnotationTests
{
    // Minimal entity used only in this test.
    private sealed class Order
    {
        public int    Id     { get; set; }
        public string Status { get; set; } = string.Empty;
    }

    private sealed class TestDbContext : DbContext
    {
        private readonly Action<ModelBuilder> _modelConfig;

        public TestDbContext(Action<ModelBuilder> modelConfig)
            : base(new DbContextOptionsBuilder()
                       .UseSqlServer("Server=.;Database=Test")
                       .Options)
        {
            _modelConfig = modelConfig;
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
            => _modelConfig(modelBuilder);
    }

    [Fact]
    public void HasRetentionPeriod_SetsAnnotationOnEntityType()
    {
        var context = new TestDbContext(mb =>
            mb.Entity<Order>().ToTable("Orders", "sales",
                o => o.IsTemporal()
                       .HasRetentionPeriod(6, TemporalPeriodUnit.Month)));

        var entityType  = context.Model.FindEntityType(typeof(Order))!;
        var annotation  = entityType.FindAnnotation(TemporalRetentionAnnotation.Name);

        annotation.Should().NotBeNull();
        annotation!.Value.Should().Be("6|Month");
    }

    [Fact]
    public void HasRetentionPeriod_SerializesToCorrectSqlFragment()
    {
        var context = new TestDbContext(mb =>
            mb.Entity<Order>().ToTable("Orders",
                o => o.IsTemporal()
                       .HasRetentionPeriod(1, TemporalPeriodUnit.Year)));

        var entityType = context.Model.FindEntityType(typeof(Order))!;
        var rawValue   = entityType.FindAnnotation(TemporalRetentionAnnotation.Name)?.Value as string;

        rawValue.Should().NotBeNullOrEmpty();

        var period = TemporalRetentionPeriod.Deserialize(rawValue!);
        period.ToSqlFragment().Should().Be("1 YEARS");
    }

    [Fact]
    public void HasInfiniteRetention_SetsInfiniteAnnotation()
    {
        var context = new TestDbContext(mb =>
            mb.Entity<Order>().ToTable("Orders",
                o => o.IsTemporal()
                       .HasInfiniteRetention()));

        var entityType = context.Model.FindEntityType(typeof(Order))!;
        var rawValue   = entityType.FindAnnotation(TemporalRetentionAnnotation.Name)?.Value as string;

        rawValue.Should().Be("INFINITE");
    }

    [Fact]
    public void HasRetentionPeriod_ZeroValue_ThrowsArgumentOutOfRangeException()
    {
        var act = () => new TestDbContext(mb =>
            mb.Entity<Order>().ToTable("Orders",
                o => o.IsTemporal()
                       .HasRetentionPeriod(0, TemporalPeriodUnit.Month)));

        // The exception surfaces during model building.
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void HasRetentionPeriod_AllowsChaining()
    {
        // Verifies that HasRetentionPeriod returns TemporalTableBuilder,
        // allowing further calls like UseHistoryTable to be chained after it.
        var act = () => new TestDbContext(mb =>
            mb.Entity<Order>().ToTable("Orders",
                o => o.IsTemporal()
                       .HasRetentionPeriod(3, TemporalPeriodUnit.Month)
                       .UseHistoryTable("OrdersHistory")));

        act.Should().NotThrow();
    }
}
