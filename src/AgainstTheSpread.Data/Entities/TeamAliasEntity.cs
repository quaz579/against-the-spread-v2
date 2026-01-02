namespace AgainstTheSpread.Data.Entities;

/// <summary>
/// Represents a team name alias mapping.
/// Maps alternative team names (aliases) to their canonical form.
/// </summary>
public class TeamAliasEntity
{
    /// <summary>
    /// Primary key identifier.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// The alias or alternative name for the team (e.g., "USF", "South Florida Bulls").
    /// This is the lookup key - must be unique (case-insensitive).
    /// </summary>
    public string Alias { get; set; } = string.Empty;

    /// <summary>
    /// The canonical (official) team name that this alias maps to (e.g., "South Florida").
    /// All aliases for the same team should map to the same canonical name.
    /// </summary>
    public string CanonicalName { get; set; } = string.Empty;

    /// <summary>
    /// When this alias was created.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// When this alias was last updated. Null if never updated.
    /// </summary>
    public DateTime? UpdatedAt { get; set; }
}
