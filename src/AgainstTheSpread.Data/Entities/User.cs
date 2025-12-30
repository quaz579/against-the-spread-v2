namespace AgainstTheSpread.Data.Entities;

/// <summary>
/// Represents a user in the Against The Spread application.
/// Users can be authenticated via Google OAuth and submit picks.
/// </summary>
public class User
{
    /// <summary>
    /// Primary key identifier for the user.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Google OAuth subject identifier (sub claim).
    /// Used to uniquely identify the user from Google authentication.
    /// </summary>
    public string GoogleSubjectId { get; set; } = string.Empty;

    /// <summary>
    /// User's email address from Google authentication.
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// User's display name from Google authentication.
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Timestamp when the user account was created.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Timestamp of the user's most recent login.
    /// </summary>
    public DateTime LastLoginAt { get; set; }

    /// <summary>
    /// Indicates whether the user account is active.
    /// Inactive users cannot submit picks.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Collection of picks made by this user.
    /// </summary>
    public ICollection<Pick> Picks { get; set; } = new List<Pick>();
}
