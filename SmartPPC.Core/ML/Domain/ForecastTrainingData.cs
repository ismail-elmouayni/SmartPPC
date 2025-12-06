using System;

namespace SmartPPC.Core.ML.Domain;

/// <summary>
/// Represents a single historical demand observation used for training forecasting models.
/// Captures actual demand data along with contextual information at a specific point in time.
/// </summary>
public class ForecastTrainingData
{
    /// <summary>
    /// Unique identifier for this training data point.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Reference to the configuration this data belongs to.
    /// </summary>
    public Guid ConfigurationId { get; set; }

    /// <summary>
    /// Reference to the specific station this demand data is for.
    /// </summary>
    public int StationDeclarationId { get; set; }

    /// <summary>
    /// The date and time when this demand was observed.
    /// </summary>
    public DateTime ObservationDate { get; set; }

    /// <summary>
    /// The actual demand value observed at this station on this date.
    /// </summary>
    public int DemandValue { get; set; }

    /// <summary>
    /// Buffer level at the time of observation (if station has a buffer).
    /// </summary>
    public int? BufferLevel { get; set; }

    /// <summary>
    /// Order amount placed at this station on this date.
    /// </summary>
    public int? OrderAmount { get; set; }

    /// <summary>
    /// Day of week (1-7) for temporal feature extraction.
    /// </summary>
    public int DayOfWeek { get; set; }

    /// <summary>
    /// Month (1-12) for seasonal pattern recognition.
    /// </summary>
    public int Month { get; set; }

    /// <summary>
    /// Quarter (1-4) for quarterly seasonality.
    /// </summary>
    public int Quarter { get; set; }

    /// <summary>
    /// Optional JSON field for additional exogenous factors
    /// (e.g., holidays, promotions, special events).
    /// </summary>
    public string? ExogenousFactors { get; set; }

    /// <summary>
    /// Timestamp when this record was created in the database.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    // Note: StationDeclarationId stores the station index (int), not the StationDeclaration.Id (Guid)
    // No navigation property due to type mismatch

    /// <summary>
    /// Navigation property to the configuration.
    /// </summary>
    public SmartPPC.Core.Domain.Configuration? Configuration { get; set; }
}
