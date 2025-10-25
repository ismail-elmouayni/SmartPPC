namespace SmartPPC.Core.Domain;

/// <summary>
/// Represents a station declaration in the production configuration
/// </summary>
public class StationDeclaration
{
    /// <summary>
    /// Unique identifier
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Foreign key to the configuration
    /// </summary>
    public Guid ConfigurationId { get; set; }

    /// <summary>
    /// Index/position of the station in the production line
    /// </summary>
    public int? StationIndex { get; set; }

    /// <summary>
    /// Processing time for this station
    /// </summary>
    public float? ProcessingTime { get; set; }

    /// <summary>
    /// Lead time for this station (applicable for input stations)
    /// </summary>
    public float? LeadTime { get; set; }

    /// <summary>
    /// Initial buffer level
    /// </summary>
    public int? InitialBuffer { get; set; }

    /// <summary>
    /// Demand variability coefficient (applicable for output stations)
    /// </summary>
    public float? DemandVariability { get; set; }

    /// <summary>
    /// Navigation property to the configuration
    /// </summary>
    public Configuration Configuration { get; set; } = null!;

    /// <summary>
    /// Past buffer values for this station
    /// </summary>
    public ICollection<StationPastBuffer> PastBuffers { get; set; } = new List<StationPastBuffer>();

    /// <summary>
    /// Past order amounts for this station
    /// </summary>
    public ICollection<StationPastOrderAmount> PastOrderAmounts { get; set; } = new List<StationPastOrderAmount>();

    /// <summary>
    /// Demand forecast values for this station
    /// </summary>
    public ICollection<StationDemandForecast> DemandForecasts { get; set; } = new List<StationDemandForecast>();

    /// <summary>
    /// Input relationships (stations that feed into this station)
    /// </summary>
    public ICollection<StationInput> NextStationInputs { get; set; } = new List<StationInput>();
}
