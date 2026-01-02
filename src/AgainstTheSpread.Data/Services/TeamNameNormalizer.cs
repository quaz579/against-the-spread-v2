using AgainstTheSpread.Data.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AgainstTheSpread.Data.Services;

/// <summary>
/// Service for normalizing team names to their canonical form.
/// Uses an in-memory cache for efficient lookups.
/// </summary>
public class TeamNameNormalizer : ITeamNameNormalizer
{
    private readonly AtsDbContext _context;
    private readonly ILogger<TeamNameNormalizer> _logger;

    // In-memory cache: alias (lowercase) -> canonical name
    private Dictionary<string, string> _aliasCache = new(StringComparer.OrdinalIgnoreCase);
    private readonly SemaphoreSlim _cacheLock = new(1, 1);
    private bool _cacheLoaded = false;

    /// <summary>
    /// Initializes a new instance of TeamNameNormalizer.
    /// </summary>
    /// <param name="context">The database context.</param>
    /// <param name="logger">Logger for diagnostic information.</param>
    public TeamNameNormalizer(AtsDbContext context, ILogger<TeamNameNormalizer> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<string> NormalizeAsync(string teamName, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(teamName))
        {
            return string.Empty;
        }

        var trimmedName = teamName.Trim();

        await EnsureCacheLoadedAsync(cancellationToken);

        if (_aliasCache.TryGetValue(trimmedName, out var canonicalName))
        {
            if (!string.Equals(trimmedName, canonicalName, StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogDebug("Normalized team name '{Original}' to '{Canonical}'", trimmedName, canonicalName);
            }
            return canonicalName;
        }

        // Unknown team - pass through with warning
        _logger.LogWarning("Unknown team '{TeamName}' - no alias mapping found. Passing through unchanged.", trimmedName);
        return trimmedName;
    }

    /// <inheritdoc/>
    public async Task<Dictionary<string, string>> NormalizeBatchAsync(
        IEnumerable<string> teamNames,
        CancellationToken cancellationToken = default)
    {
        await EnsureCacheLoadedAsync(cancellationToken);

        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var teamName in teamNames)
        {
            if (string.IsNullOrWhiteSpace(teamName))
            {
                continue;
            }

            var trimmedName = teamName.Trim();
            if (result.ContainsKey(trimmedName))
            {
                continue; // Already processed
            }

            if (_aliasCache.TryGetValue(trimmedName, out var canonicalName))
            {
                result[trimmedName] = canonicalName;
            }
            else
            {
                _logger.LogWarning("Unknown team '{TeamName}' - no alias mapping found. Passing through unchanged.", trimmedName);
                result[trimmedName] = trimmedName;
            }
        }

        return result;
    }

    /// <inheritdoc/>
    public async Task<bool> AreTeamsEqualAsync(
        string teamName1,
        string teamName2,
        CancellationToken cancellationToken = default)
    {
        var normalized1 = await NormalizeAsync(teamName1, cancellationToken);
        var normalized2 = await NormalizeAsync(teamName2, cancellationToken);

        return string.Equals(normalized1, normalized2, StringComparison.OrdinalIgnoreCase);
    }

    /// <inheritdoc/>
    public async Task RefreshCacheAsync(CancellationToken cancellationToken = default)
    {
        await _cacheLock.WaitAsync(cancellationToken);
        try
        {
            _logger.LogInformation("Refreshing team alias cache from database");

            var aliases = await _context.TeamAliases
                .AsNoTracking()
                .ToListAsync(cancellationToken);

            var newCache = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            foreach (var alias in aliases)
            {
                newCache[alias.Alias] = alias.CanonicalName;
            }

            _aliasCache = newCache;
            _cacheLoaded = true;

            _logger.LogInformation("Loaded {Count} team aliases into cache", aliases.Count);
        }
        finally
        {
            _cacheLock.Release();
        }
    }

    /// <inheritdoc/>
    public async Task<List<string>> GetAllCanonicalNamesAsync(CancellationToken cancellationToken = default)
    {
        await EnsureCacheLoadedAsync(cancellationToken);

        return _aliasCache.Values
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(n => n)
            .ToList();
    }

    /// <summary>
    /// Ensures the cache is loaded from the database.
    /// </summary>
    private async Task EnsureCacheLoadedAsync(CancellationToken cancellationToken)
    {
        if (_cacheLoaded)
        {
            return;
        }

        await RefreshCacheAsync(cancellationToken);
    }
}
