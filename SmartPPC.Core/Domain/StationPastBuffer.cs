namespace SmartPPC.Core.Domain;

/// <summary>
/// Represents a past buffer value for a station at a specific time instant
/// </summary>
public class StationPastBuffer
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
    /// Time instant (negative for past periods)
    /// </summary>
    public int Instant { get; set; }

    /// <summary>
    /// Buffer value at this instant
    /// </summary>
    public int Value { get; set; }

    /// <summary>
    /// Navigation property to the station declaration
    /// </summary>
    public StationDeclaration StationDeclaration { get; set; } = null!;
}
