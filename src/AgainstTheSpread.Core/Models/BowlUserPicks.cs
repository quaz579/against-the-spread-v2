namespace AgainstTheSpread.Core.Models;

/// <summary>
/// Represents a user's complete bowl picks for all games.
/// Bowl picks require unique confidence points 1-N and sum to N(N+1)/2.
/// </summary>
public class BowlUserPicks
{
    /// <summary>
    /// User's name
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Year of the bowl season
    /// </summary>
    public int Year { get; set; }

    /// <summary>
    /// Total number of bowl games for this season (determined from lines file)
    /// </summary>
    public int TotalGames { get; set; }

    /// <summary>
    /// List of all bowl picks (must match TotalGames count)
    /// </summary>
    public List<BowlPick> Picks { get; set; } = new();

    /// <summary>
    /// When the picks were submitted
    /// </summary>
    public DateTime SubmittedAt { get; set; }

    /// <summary>
    /// Calculates expected confidence sum for validation: N(N+1)/2
    /// </summary>
    public int ExpectedConfidenceSum => TotalGames * (TotalGames + 1) / 2;

    /// <summary>
    /// Validates that all picks are complete and confidence points are valid
    /// </summary>
    public bool IsValid()
    {
        if (string.IsNullOrWhiteSpace(Name))
            return false;

        if (Year < 2020)
            return false;

        if (TotalGames < 1)
            return false;

        if (Picks.Count != TotalGames)
            return false;

        if (!Picks.All(p => p.IsValid(TotalGames)))
            return false;

        // Check unique confidence points
        var confidencePoints = Picks.Select(p => p.ConfidencePoints).ToList();
        if (confidencePoints.Distinct().Count() != TotalGames)
            return false;

        // Check sum equals N(N+1)/2
        if (confidencePoints.Sum() != ExpectedConfidenceSum)
            return false;

        return true;
    }

    /// <summary>
    /// Gets validation error message if invalid
    /// </summary>
    public string? GetValidationError()
    {
        if (string.IsNullOrWhiteSpace(Name))
            return "Name is required";

        if (Year < 2020)
            return "Invalid year";

        if (TotalGames < 1)
            return "Total games must be set";

        if (Picks.Count != TotalGames)
            return $"Exactly {TotalGames} picks are required (you have {Picks.Count})";

        var invalidPicks = Picks.Where(p => !p.IsValid(TotalGames)).ToList();
        if (invalidPicks.Any())
            return $"Invalid picks found for games: {string.Join(", ", invalidPicks.Select(p => p.GameNumber))}";

        var confidencePoints = Picks.Select(p => p.ConfidencePoints).ToList();
        var duplicates = confidencePoints.GroupBy(x => x).Where(g => g.Count() > 1).Select(g => g.Key);
        if (duplicates.Any())
            return $"Duplicate confidence points found: {string.Join(", ", duplicates)}";

        var sum = confidencePoints.Sum();
        if (sum != ExpectedConfidenceSum)
            return $"Confidence points must sum to {ExpectedConfidenceSum} (yours: {sum})";

        return null;
    }

    /// <summary>
    /// Calculates if confidence points sum to the expected value
    /// </summary>
    public bool HasValidConfidenceSum()
    {
        return Picks.Sum(p => p.ConfidencePoints) == ExpectedConfidenceSum;
    }
}
