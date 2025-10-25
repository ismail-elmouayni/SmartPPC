namespace SmartPPC.Core.Domain;

/// <summary>
/// Represents a demand forecast value for a station at a specific time instant
/// </summary>
public class StationDemandForecast
{
    /// <summary>
    /// Unique identifier
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Foreign key to the station declaration
    /// </summary>
    public Guid StationDeclarationId { get; set; }

    /// <summary>
    /// Time instant (future period)
    /// </summary>
    public int Instant { get; set; }

    /// <summary>
    /// Forecasted demand value at this instant
    /// </summary>
    public int Value { get; set; }

    /// <summary>
    /// Navigation property to the station declaration
    /// </summary>
    public StationDeclaration StationDeclaration { get; set; } = null!;
}
