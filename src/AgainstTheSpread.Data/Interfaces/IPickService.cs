using AgainstTheSpread.Data.Entities;

namespace AgainstTheSpread.Data.Interfaces;

/// <summary>
/// Service for managing user picks in the Against The Spread application.
/// </summary>
public interface IPickService
{
    /// <summary>
    /// Submits or updates picks for a user for a specific week.
    /// Validates that games are not locked before allowing submission.
    /// </summary>
    /// <param name="userId">The user's ID.</param>
    /// <param name="year">The season year.</param>
    /// <param name="week">The week number.</param>
    /// <param name="picks">The picks to submit.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result containing success status and any error details.</returns>
    Task<PickSubmissionResult> SubmitPicksAsync(
        Guid userId,
        int year,
        int week,
        IEnumerable<PickSubmission> picks,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a user's picks for a specific week.
    /// </summary>
    /// <param name="userId">The user's ID.</param>
    /// <param name="year">The season year.</param>
    /// <param name="week">The week number.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of user's picks for the specified week.</returns>
    Task<List<Pick>> GetUserPicksAsync(
        Guid userId,
        int year,
        int week,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all of a user's picks for an entire season.
    /// </summary>
    /// <param name="userId">The user's ID.</param>
    /// <param name="year">The season year.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of user's picks for the entire season, grouped by week.</returns>
    Task<List<Pick>> GetUserSeasonPicksAsync(
        Guid userId,
        int year,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Input model for submitting a pick.
/// </summary>
public record PickSubmission(int GameId, string SelectedTeam);

/// <summary>
/// Result of a pick submission attempt.
/// </summary>
public class PickSubmissionResult
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
    /// Number of picks that were rejected (e.g., game locked).
    /// </summary>
    public int PicksRejected { get; set; }

    /// <summary>
    /// Details about rejected picks.
    /// </summary>
    public List<RejectedPick> RejectedPicks { get; set; } = new();

    /// <summary>
    /// General error message if the entire submission failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    public static PickSubmissionResult CreateSuccess(int submitted, List<RejectedPick>? rejected = null)
    {
        return new PickSubmissionResult
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
    public static PickSubmissionResult CreateFailure(string error)
    {
        return new PickSubmissionResult
        {
            Success = false,
            ErrorMessage = error
        };
    }
}

/// <summary>
/// Details about a rejected pick.
/// </summary>
public record RejectedPick(int GameId, string Reason);
