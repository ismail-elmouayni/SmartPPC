namespace SmartPPC.Core.Domain;

/// <summary>
/// Represents a production planning configuration for a user
/// </summary>
public class Configuration
{
    /// <summary>
    /// Unique identifier for the configuration
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Foreign key to the user who owns this configuration
    /// </summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// Name of the configuration (user-defined)
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Date when the configuration was created
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Date when the configuration was last updated
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Indicates if this is the currently active/loaded configuration for the user
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// Navigation property to the user
    /// </summary>
    public User User { get; set; } = null!;

    /// <summary>
    /// Navigation property to general settings (1-to-1)
    /// </summary>
    public GeneralSettings? GeneralSettings { get; set; }

    /// <summary>
    /// Navigation property to station declarations (1-to-many)
    /// </summary>
    public ICollection<StationDeclaration> StationDeclarations { get; set; } = new List<StationDeclaration>();
}
