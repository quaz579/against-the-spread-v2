using AgainstTheSpread.Data.Entities;

namespace AgainstTheSpread.Data.Interfaces;

/// <summary>
/// Service for managing user bowl picks in the Against The Spread application.
/// </summary>
public interface IBowlPickService
{
    /// <summary>
    /// Submits or updates all bowl picks for a user for a specific year.
    /// Validates confidence point uniqueness and that games are not locked.
    /// </summary>
    /// <param name="userId">The user's ID.</param>
    /// <param name="year">The bowl season year.</param>
    /// <param name="picks">The bowl picks to submit.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result containing success status and any error details.</returns>
    Task<BowlPickSubmissionResult> SubmitBowlPicksAsync(
        Guid userId,
        int year,
        IEnumerable<BowlPickSubmission> picks,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a user's bowl picks for a specific year.
    /// </summary>
    /// <param name="userId">The user's ID.</param>
    /// <param name="year">The bowl season year.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of user's bowl picks for the specified year.</returns>
    Task<List<BowlPickEntity>> GetUserBowlPicksAsync(
        Guid userId,
        int year,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all users who have submitted bowl picks for a year.
    /// </summary>
    /// <param name="year">The bowl season year.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of user IDs who have submitted picks.</returns>
    Task<List<Guid>> GetUsersWithBowlPicksAsync(
        int year,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Input model for submitting a bowl pick.
/// </summary>
public record BowlPickSubmission(
    int BowlGameId,
    string SpreadPick,
    int ConfidencePoints,
    string OutrightWinnerPick);

/// <summary>
/// Result of a bowl pick submission attempt.
/// </summary>
public class BowlPickSubmissionResult
{
    /// <summary>
    /// Whether the submission was successful.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Number of picks successfully submitted.
    /// </summary>
    public int PicksSubmitted { get; set; }

    /// <summary>
    /// Number of picks that were rejected.
    /// </summary>
    public int PicksRejected { get; set; }

    /// <summary>
    /// Details about rejected picks.
    /// </summary>
    public List<RejectedBowlPick> RejectedPicks { get; set; } = new();

    /// <summary>
    /// General error message if the entire submission failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    public static BowlPickSubmissionResult CreateSuccess(int submitted, List<RejectedBowlPick>? rejected = null)
    {
        return new BowlPickSubmissionResult
        {
            Success = true,
            PicksSubmitted = submitted,
            PicksRejected = rejected?.Count ?? 0,
            RejectedPicks = rejected ?? new()
        };
    }

    /// <summary>
    /// Creates a failed result.
    /// </summary>
    public static BowlPickSubmissionResult CreateFailure(string error)
    {
        return new BowlPickSubmissionResult
        {
            Success = false,
            ErrorMessage = error
        };
    }
}

/// <summary>
/// Details about a rejected bowl pick.
/// </summary>
public record RejectedBowlPick(int BowlGameId, string Reason);
