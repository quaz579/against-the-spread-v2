namespace AgainstTheSpread.Data.Entities;

/// <summary>
/// Represents a user's pick for a specific game.
/// </summary>
public class Pick
{
    /// <summary>
    /// Primary key identifier for the pick.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Foreign key to the User who made this pick.
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Foreign key to the Game this pick is for.
    /// </summary>
    public int GameId { get; set; }

    /// <summary>
    /// The team the user picked (either Favorite or Underdog).
    /// </summary>
    public string SelectedTeam { get; set; } = string.Empty;

    /// <summary>
    /// Timestamp when the pick was originally submitted.
    /// </summary>
    public DateTime SubmittedAt { get; set; }

    /// <summary>
    /// Timestamp when the pick was last updated. Null if never updated.
    /// </summary>
    public DateTime? UpdatedAt { get; set; }

    #region Denormalized Fields for Efficient Queries

    /// <summary>
    /// The season year (denormalized from Game for efficient queries).
    /// </summary>
    public int Year { get; set; }

    /// <summary>
    /// The week number (denormalized from Game for efficient queries).
    /// </summary>
    public int Week { get; set; }

    #endregion

    #region Navigation Properties

    /// <summary>
    /// Navigation property to the User who made this pick.
    /// </summary>
    public User? User { get; set; }

    /// <summary>
    /// Navigation property to the Game this pick is for.
    /// </summary>
    public GameEntity? Game { get; set; }

    #endregion
}
