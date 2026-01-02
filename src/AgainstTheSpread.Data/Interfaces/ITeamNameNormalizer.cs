namespace AgainstTheSpread.Data.Interfaces;

/// <summary>
/// Service for normalizing team names to their canonical form.
/// Maps aliases (e.g., "USF", "South Florida Bulls") to canonical names (e.g., "South Florida").
/// </summary>
public interface ITeamNameNormalizer
{
    /// <summary>
    /// Normalizes a team name to its canonical form.
    /// Returns the original name (trimmed) if no alias mapping exists.
    /// Logs a warning for unknown teams.
    /// </summary>
    /// <param name="teamName">The team name to normalize.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The canonical team name, or the original name if no mapping exists.</returns>
    Task<string> NormalizeAsync(string teamName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Normalizes multiple team names efficiently (batch operation).
    /// Uses cached mappings to avoid repeated database calls.
    /// </summary>
    /// <param name="teamNames">The team names to normalize.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Dictionary mapping original names to their canonical forms.</returns>
    Task<Dictionary<string, string>> NormalizeBatchAsync(
        IEnumerable<string> teamNames,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if two team names refer to the same team after normalization.
    /// </summary>
    /// <param name="teamName1">First team name.</param>
    /// <param name="teamName2">Second team name.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if both names normalize to the same canonical name.</returns>
    Task<bool> AreTeamsEqualAsync(
        string teamName1,
        string teamName2,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Refreshes the in-memory alias cache from the database.
    /// Call this after adding new aliases via admin UI.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task RefreshCacheAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all known canonical team names.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of all canonical team names.</returns>
    Task<List<string>> GetAllCanonicalNamesAsync(CancellationToken cancellationToken = default);
}
