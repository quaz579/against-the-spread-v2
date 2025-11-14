namespace AgainstTheSpread.Web.Models;

/// <summary>
/// Represents the primary and secondary colors for a team
/// </summary>
public class TeamColors
{
    /// <summary>
    /// The primary color in hex format (e.g., "#9E1B32")
    /// </summary>
    public string Primary { get; set; } = string.Empty;

    /// <summary>
    /// The secondary color in hex format (e.g., "#828A8F")
    /// </summary>
    public string Secondary { get; set; } = string.Empty;
}
