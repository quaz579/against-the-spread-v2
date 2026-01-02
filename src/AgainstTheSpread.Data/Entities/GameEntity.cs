using System.ComponentModel.DataAnnotations.Schema;

namespace AgainstTheSpread.Data.Entities;

/// <summary>
/// Represents a football game with betting line information.
/// Named GameEntity to avoid confusion with Core.Models.Game.
/// </summary>
public class GameEntity
{
    /// <summary>
    /// Primary key identifier for the game.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// The season year (e.g., 2024).
    /// </summary>
    public int Year { get; set; }

    /// <summary>
    /// The week number within the season.
    /// </summary>
    public int Week { get; set; }

    /// <summary>
    /// The favored team in the matchup.
    /// </summary>
    public string Favorite { get; set; } = string.Empty;

    /// <summary>
    /// The underdog team in the matchup.
    /// </summary>
    public string Underdog { get; set; } = string.Empty;

    /// <summary>
    /// The point spread (negative number representing favorite's handicap).
    /// Example: -7.5 means favorite must win by more than 7.5 points.
    /// </summary>
    public decimal Line { get; set; }

    /// <summary>
    /// The scheduled kickoff time for the game.
    /// </summary>
    public DateTime GameDate { get; set; }

    #region Result Fields

    /// <summary>
    /// Final score for the favorite team. Null until result is entered.
    /// </summary>
    public int? FavoriteScore { get; set; }

    /// <summary>
    /// Final score for the underdog team. Null until result is entered.
    /// </summary>
    public int? UnderdogScore { get; set; }

    /// <summary>
    /// The team that won against the spread. Null until result is entered.
    /// Will be null if the game resulted in a push.
    /// </summary>
    public string? SpreadWinner { get; set; }

    /// <summary>
    /// Indicates if the game resulted in a push (exact spread match).
    /// Null until result is entered.
    /// </summary>
    public bool? IsPush { get; set; }

    /// <summary>
    /// Timestamp when the result was entered.
    /// </summary>
    public DateTime? ResultEnteredAt { get; set; }

    /// <summary>
    /// User ID of the admin who entered the result.
    /// </summary>
    public Guid? ResultEnteredBy { get; set; }

    #endregion

    /// <summary>
    /// Collection of picks made for this game.
    /// </summary>
    public ICollection<Pick> Picks { get; set; } = new List<Pick>();

    #region Computed Properties (Not Mapped to Database)

    /// <summary>
    /// Indicates whether the game is locked for picks (kickoff time has passed).
    /// Can be disabled via DISABLE_GAME_LOCKING environment variable for testing.
    /// This property is not stored in the database.
    /// </summary>
    [NotMapped]
    public bool IsLocked =>
        Environment.GetEnvironmentVariable("DISABLE_GAME_LOCKING") != "true"
        && DateTime.UtcNow >= GameDate;

    /// <summary>
    /// Indicates whether the game has a result entered.
    /// True if either SpreadWinner is set or IsPush is true.
    /// This property is not stored in the database.
    /// </summary>
    [NotMapped]
    public bool HasResult => SpreadWinner != null || IsPush == true;

    #endregion
}
