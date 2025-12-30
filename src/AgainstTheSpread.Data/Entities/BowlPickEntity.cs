namespace AgainstTheSpread.Data.Entities;

/// <summary>
/// Represents a user's pick for a bowl game.
/// Bowl picks include spread pick, confidence points, and outright winner prediction.
/// </summary>
public class BowlPickEntity
{
    /// <summary>
    /// Primary key identifier.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Foreign key to the user who made this pick.
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Foreign key to the bowl game.
    /// </summary>
    public int BowlGameId { get; set; }

    /// <summary>
    /// The team picked against the spread.
    /// </summary>
    public string SpreadPick { get; set; } = string.Empty;

    /// <summary>
    /// Confidence points assigned to this pick (1 through total game count).
    /// Higher values indicate more confidence. Each value must be unique per user per season.
    /// </summary>
    public int ConfidencePoints { get; set; }

    /// <summary>
    /// The team picked to win outright (regardless of spread).
    /// </summary>
    public string OutrightWinnerPick { get; set; } = string.Empty;

    /// <summary>
    /// Timestamp when the pick was submitted.
    /// </summary>
    public DateTime SubmittedAt { get; set; }

    /// <summary>
    /// Timestamp when the pick was last updated.
    /// </summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// Denormalized year for efficient queries.
    /// </summary>
    public int Year { get; set; }

    #region Navigation Properties

    /// <summary>
    /// Navigation property to the user.
    /// </summary>
    public User? User { get; set; }

    /// <summary>
    /// Navigation property to the bowl game.
    /// </summary>
    public BowlGameEntity? BowlGame { get; set; }

    #endregion
}
