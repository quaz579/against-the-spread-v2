using AgainstTheSpread.Data.Entities;
using AgainstTheSpread.Data.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AgainstTheSpread.Data.Services;

/// <summary>
/// Service for managing user bowl picks in the Against The Spread application.
/// </summary>
public class BowlPickService : IBowlPickService
{
    private readonly AtsDbContext _context;
    private readonly ILogger<BowlPickService> _logger;

    /// <summary>
    /// Initializes a new instance of BowlPickService.
    /// </summary>
    /// <param name="context">The database context.</param>
    /// <param name="logger">Logger for diagnostic information.</param>
    public BowlPickService(AtsDbContext context, ILogger<BowlPickService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<BowlPickSubmissionResult> SubmitBowlPicksAsync(
        Guid userId,
        int year,
        IEnumerable<BowlPickSubmission> picks,
        CancellationToken cancellationToken = default)
    {
        var picksList = picks.ToList();
        if (!picksList.Any())
        {
            _logger.LogInformation("No bowl picks to submit for user {UserId} year {Year}", userId, year);
            return BowlPickSubmissionResult.CreateSuccess(0);
        }

        // Validate confidence points are unique
        var confidencePoints = picksList.Select(p => p.ConfidencePoints).ToList();
        if (confidencePoints.Distinct().Count() != confidencePoints.Count)
        {
            return BowlPickSubmissionResult.CreateFailure("Confidence points must be unique for each pick");
        }

        var submittedCount = 0;
        var rejectedPicks = new List<RejectedBowlPick>();

        // Get all relevant game IDs in one query
        var gameIds = picksList.Select(p => p.BowlGameId).ToList();
        var games = await _context.BowlGames
            .Where(g => gameIds.Contains(g.Id))
            .ToDictionaryAsync(g => g.Id, cancellationToken);

        // Get existing picks for this user and these games
        var existingPicks = await _context.BowlPicks
            .Where(p => p.UserId == userId && gameIds.Contains(p.BowlGameId))
            .ToDictionaryAsync(p => p.BowlGameId, cancellationToken);

        foreach (var pickSubmission in picksList)
        {
            // Validate game exists
            if (!games.TryGetValue(pickSubmission.BowlGameId, out var game))
            {
                rejectedPicks.Add(new RejectedBowlPick(pickSubmission.BowlGameId, "Bowl game not found"));
                _logger.LogWarning("Bowl pick rejected: Game {GameId} not found", pickSubmission.BowlGameId);
                continue;
            }

            // Validate game is not locked
            if (game.IsLocked)
            {
                rejectedPicks.Add(new RejectedBowlPick(pickSubmission.BowlGameId,
                    $"Game is locked (kickoff: {game.GameDate:g})"));
                _logger.LogWarning("Bowl pick rejected: Game {GameId} is locked", pickSubmission.BowlGameId);
                continue;
            }

            // Validate spread pick is valid for this game
            if (pickSubmission.SpreadPick != game.Favorite && pickSubmission.SpreadPick != game.Underdog)
            {
                rejectedPicks.Add(new RejectedBowlPick(pickSubmission.BowlGameId,
                    $"Invalid spread pick. Must be '{game.Favorite}' or '{game.Underdog}'"));
                _logger.LogWarning("Bowl pick rejected: Invalid spread pick {Team} for game {GameId}",
                    pickSubmission.SpreadPick, pickSubmission.BowlGameId);
                continue;
            }

            // Validate outright winner pick is valid for this game
            if (pickSubmission.OutrightWinnerPick != game.Favorite && pickSubmission.OutrightWinnerPick != game.Underdog)
            {
                rejectedPicks.Add(new RejectedBowlPick(pickSubmission.BowlGameId,
                    $"Invalid outright winner pick. Must be '{game.Favorite}' or '{game.Underdog}'"));
                _logger.LogWarning("Bowl pick rejected: Invalid outright winner pick {Team} for game {GameId}",
                    pickSubmission.OutrightWinnerPick, pickSubmission.BowlGameId);
                continue;
            }

            // Check for existing pick
            if (existingPicks.TryGetValue(pickSubmission.BowlGameId, out var existingPick))
            {
                // Update existing pick
                existingPick.SpreadPick = pickSubmission.SpreadPick;
                existingPick.ConfidencePoints = pickSubmission.ConfidencePoints;
                existingPick.OutrightWinnerPick = pickSubmission.OutrightWinnerPick;
                existingPick.UpdatedAt = DateTime.UtcNow;
                _logger.LogDebug("Updated bowl pick for user {UserId} game {GameId}", userId, pickSubmission.BowlGameId);
            }
            else
            {
                // Create new pick
                var newPick = new BowlPickEntity
                {
                    UserId = userId,
                    BowlGameId = pickSubmission.BowlGameId,
                    SpreadPick = pickSubmission.SpreadPick,
                    ConfidencePoints = pickSubmission.ConfidencePoints,
                    OutrightWinnerPick = pickSubmission.OutrightWinnerPick,
                    SubmittedAt = DateTime.UtcNow,
                    Year = year
                };
                _context.BowlPicks.Add(newPick);
                _logger.LogDebug("Created bowl pick for user {UserId} game {GameId}", userId, pickSubmission.BowlGameId);
            }

            submittedCount++;
        }

        if (submittedCount > 0)
        {
            await _context.SaveChangesAsync(cancellationToken);
        }

        _logger.LogInformation("Bowl pick submission for user {UserId}: {Submitted} submitted, {Rejected} rejected",
            userId, submittedCount, rejectedPicks.Count);

        return BowlPickSubmissionResult.CreateSuccess(submittedCount, rejectedPicks.Count > 0 ? rejectedPicks : null);
    }

    /// <inheritdoc/>
    public async Task<List<BowlPickEntity>> GetUserBowlPicksAsync(
        Guid userId,
        int year,
        CancellationToken cancellationToken = default)
    {
        return await _context.BowlPicks
            .Include(p => p.BowlGame)
            .Where(p => p.UserId == userId && p.Year == year)
            .OrderBy(p => p.BowlGame!.GameNumber)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<List<Guid>> GetUsersWithBowlPicksAsync(
        int year,
        CancellationToken cancellationToken = default)
    {
        return await _context.BowlPicks
            .Where(p => p.Year == year)
            .Select(p => p.UserId)
            .Distinct()
            .ToListAsync(cancellationToken);
    }
}
