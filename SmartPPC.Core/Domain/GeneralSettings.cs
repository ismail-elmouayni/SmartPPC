namespace SmartPPC.Core.Domain;

/// <summary>
/// Represents general settings for a configuration (1-to-1 with Configuration)
/// </summary>
public class GeneralSettings
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
    /// Planning horizon (number of periods to plan ahead)
    /// </summary>
    public int PlanningHorizon { get; set; }

    /// <summary>
    /// Peak horizon (number of periods to consider for peak demand)
    /// </summary>
    public int PeakHorizon { get; set; }

    /// <summary>
    /// Past horizon (number of past periods to consider)
    /// </summary>
    public int PastHorizon { get; set; }

    /// <summary>
    /// Peak threshold value
    /// </summary>
    public float PeakThreshold { get; set; }

    /// <summary>
    /// Number of stations in the production line
    /// </summary>
    public int NumberOfStations { get; set; }

    /// <summary>
    /// Navigation property to the configuration
    /// </summary>
    public Configuration Configuration { get; set; } = null!;
}
