namespace EF.TemporalExtensions;

/// <summary>
/// Marker interface that opts an entity class into automatic temporal table configuration
/// when <see cref="TemporalModelBuilderExtensions.UseTemporalTablesForTemporalEntities"/> is called.
/// </summary>
/// <remarks>
/// Implement this interface on any entity whose EF table should be system-versioned:
/// <code>
/// public class Order : ITemporalEntity
/// {
///     public int Id { get; set; }
///     public string Status { get; set; } = string.Empty;
/// }
/// </code>
/// </remarks>
public interface ITemporalEntity;
