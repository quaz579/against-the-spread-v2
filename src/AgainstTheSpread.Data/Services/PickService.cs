using AgainstTheSpread.Data.Entities;
using AgainstTheSpread.Data.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AgainstTheSpread.Data.Services;

/// <summary>
/// Service for managing user picks in the Against The Spread application.
/// </summary>
public class PickService : IPickService
{
    private readonly AtsDbContext _context;
    private readonly ILogger<PickService> _logger;

    /// <summary>
    /// Initializes a new instance of PickService.
    /// </summary>
    /// <param name="context">The database context.</param>
    /// <param name="logger">Logger for diagnostic information.</param>
    public PickService(AtsDbContext context, ILogger<PickService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<PickSubmissionResult> SubmitPicksAsync(
        Guid userId,
        int year,
        int week,
        IEnumerable<PickSubmission> picks,
        CancellationToken cancellationToken = default)
    {
        var picksList = picks.ToList();
        if (!picksList.Any())
        {
            _logger.LogInformation("No picks to submit for user {UserId} year {Year} week {Week}", userId, year, week);
            return PickSubmissionResult.CreateSuccess(0);
        }

        var submittedCount = 0;
        var deletedCount = 0;
        var rejectedPicks = new List<RejectedPick>();

        // Get all relevant game IDs in one query
        var gameIds = picksList.Select(p => p.GameId).ToList();
        var games = await _context.Games
            .Where(g => gameIds.Contains(g.Id))
            .ToDictionaryAsync(g => g.Id, cancellationToken);

        // Get ALL existing picks for this user/year/week (not just the submitted game IDs)
        var allExistingPicks = await _context.Picks
            .Include(p => p.Game)
            .Where(p => p.UserId == userId && p.Year == year && p.Week == week)
            .ToListAsync(cancellationToken);

        var existingPicksByGameId = allExistingPicks.ToDictionary(p => p.GameId);

        // Delete picks for unlocked games that are NOT in the new submission
        // This ensures the user's picks for this week match exactly what they submitted
        var picksToDelete = allExistingPicks
            .Where(p => !gameIds.Contains(p.GameId) && p.Game != null && !p.Game.IsLocked)
            .ToList();

        foreach (var pickToDelete in picksToDelete)
        {
            _context.Picks.Remove(pickToDelete);
            deletedCount++;
            _logger.LogDebug("Deleted pick for user {UserId} game {GameId} (not in new submission)",
                userId, pickToDelete.GameId);
        }

        foreach (var pickSubmission in picksList)
        {
            // Validate game exists
            if (!games.TryGetValue(pickSubmission.GameId, out var game))
            {
                rejectedPicks.Add(new RejectedPick(pickSubmission.GameId, "Game not found"));
                _logger.LogWarning("Pick rejected: Game {GameId} not found", pickSubmission.GameId);
                continue;
            }

            // Validate game is not locked
            if (game.IsLocked)
            {
                rejectedPicks.Add(new RejectedPick(pickSubmission.GameId, $"Game is locked (kickoff: {game.GameDate:g})"));
                _logger.LogWarning("Pick rejected: Game {GameId} is locked", pickSubmission.GameId);
                continue;
            }

            // Validate selected team is valid for this game
            if (pickSubmission.SelectedTeam != game.Favorite && pickSubmission.SelectedTeam != game.Underdog)
            {
                rejectedPicks.Add(new RejectedPick(pickSubmission.GameId,
                    $"Invalid team selection. Must be '{game.Favorite}' or '{game.Underdog}'"));
                _logger.LogWarning("Pick rejected: Invalid team {Team} for game {GameId}",
                    pickSubmission.SelectedTeam, pickSubmission.GameId);
                continue;
            }

            // Check for existing pick
            if (existingPicksByGameId.TryGetValue(pickSubmission.GameId, out var existingPick))
            {
                // Update existing pick
                existingPick.SelectedTeam = pickSubmission.SelectedTeam;
                existingPick.UpdatedAt = DateTime.UtcNow;
                _logger.LogDebug("Updated pick for user {UserId} game {GameId}", userId, pickSubmission.GameId);
            }
            else
            {
                // Create new pick
                var newPick = new Pick
                {
                    UserId = userId,
                    GameId = pickSubmission.GameId,
                    SelectedTeam = pickSubmission.SelectedTeam,
                    SubmittedAt = DateTime.UtcNow,
                    Year = year,
                    Week = week
                };
                _context.Picks.Add(newPick);
                _logger.LogDebug("Created pick for user {UserId} game {GameId}", userId, pickSubmission.GameId);
            }

            submittedCount++;
        }

        if (submittedCount > 0 || deletedCount > 0)
        {
            await _context.SaveChangesAsync(cancellationToken);
        }

        _logger.LogInformation("Pick submission for user {UserId}: {Submitted} submitted, {Deleted} deleted, {Rejected} rejected",
            userId, submittedCount, deletedCount, rejectedPicks.Count);

        return PickSubmissionResult.CreateSuccess(submittedCount, rejectedPicks.Count > 0 ? rejectedPicks : null);
    }

    /// <inheritdoc/>
    public async Task<List<Pick>> GetUserPicksAsync(
        Guid userId,
        int year,
        int week,
        CancellationToken cancellationToken = default)
    {
        return await _context.Picks
            .Include(p => p.Game)
            .Where(p => p.UserId == userId && p.Year == year && p.Week == week)
            .OrderBy(p => p.Game!.GameDate)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<List<Pick>> GetUserSeasonPicksAsync(
        Guid userId,
        int year,
        CancellationToken cancellationToken = default)
    {
        return await _context.Picks
            .Include(p => p.Game)
            .Where(p => p.UserId == userId && p.Year == year)
            .OrderBy(p => p.Week)
            .ThenBy(p => p.Game!.GameDate)
            .ToListAsync(cancellationToken);
    }
}
