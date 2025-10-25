namespace SmartPPC.Core.Domain;

/// <summary>
/// Represents an input relationship between stations (which station feeds into another)
/// </summary>
public class StationInput
{
    /// <summary>
    /// Unique identifier
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Foreign key to the source station declaration
    /// </summary>
    public Guid SourceStationDeclarationId { get; set; }

    /// <summary>
    /// Index of the target station that receives input from the source station
    /// </summary>
    public int TargetStationIndex { get; set; }

    /// <summary>
    /// Percentage of output that goes to the target station
    /// </summary>
    public float Percentage { get; set; }

    /// <summary>
    /// Navigation property to the source station declaration
    /// </summary>
    public StationDeclaration SourceStationDeclaration { get; set; } = null!;
}
