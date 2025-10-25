using Microsoft.AspNetCore.Identity;

namespace SmartPPC.Core.Domain;

/// <summary>
/// Represents a user in the SmartPPC system.
/// Extends IdentityUser to include additional profile information.
/// </summary>
public class User : IdentityUser
{
    /// <summary>
    /// User's first name (optional)
    /// </summary>
    public string? FirstName { get; set; }

    /// <summary>
    /// User's last name (optional)
    /// </summary>
    public string? LastName { get; set; }

    /// <summary>
    /// User's phone number (additional field, not to be confused with IdentityUser.PhoneNumber)
    /// </summary>
    public string? Phone { get; set; }

    /// <summary>
    /// User's address (optional)
    /// </summary>
    public string? Address { get; set; }

    /// <summary>
    /// Date when the user account was created
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Date when the user account was last updated
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Navigation property to user's configurations
    /// </summary>
    public ICollection<Configuration> Configurations { get; set; } = new List<Configuration>();
}
