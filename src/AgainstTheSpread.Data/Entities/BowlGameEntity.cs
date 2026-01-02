using System.ComponentModel.DataAnnotations.Schema;

namespace AgainstTheSpread.Data.Entities;

/// <summary>
/// Represents a bowl game with betting line information.
/// Bowl games have a name and game number rather than week.
/// </summary>
public class BowlGameEntity
{
    /// <summary>
    /// Cached value of DISABLE_GAME_LOCKING environment variable.
    /// Loaded once at startup for performance.
    /// </summary>
    private static readonly bool _gameLockingDisabled =
        Environment.GetEnvironmentVariable("DISABLE_GAME_LOCKING") == "true";

    /// <summary>
    /// Primary key identifier for the bowl game.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// The bowl season year (e.g., 2024 for the 2024-2025 bowl season).
    /// </summary>
    public int Year { get; set; }

    /// <summary>
    /// Game sequence number (1-N, determines order).
    /// </summary>
    public int GameNumber { get; set; }

    /// <summary>
    /// Name of the bowl game (e.g., "Rose Bowl", "Sugar Bowl").
    /// </summary>
    public string BowlName { get; set; } = string.Empty;

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
    /// </summary>
    public string? SpreadWinner { get; set; }

    /// <summary>
    /// Indicates if the game resulted in a push (exact spread match).
    /// </summary>
    public bool? IsPush { get; set; }

    /// <summary>
    /// The team that won the game outright. Null until result is entered.
    /// </summary>
    public string? OutrightWinner { get; set; }

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
    /// Collection of picks made for this bowl game.
    /// </summary>
    public ICollection<BowlPickEntity> Picks { get; set; } = new List<BowlPickEntity>();

    #region Computed Properties (Not Mapped to Database)

    /// <summary>
    /// Indicates whether the game is locked for picks (kickoff time has passed).
    /// Can be disabled via DISABLE_GAME_LOCKING environment variable for testing.
    /// </summary>
    [NotMapped]
    public bool IsLocked => !_gameLockingDisabled && DateTime.UtcNow >= GameDate;

    /// <summary>
    /// Indicates whether the game has a result entered.
    /// </summary>
    [NotMapped]
    public bool HasResult => SpreadWinner != null || IsPush == true;

    #endregion
}
