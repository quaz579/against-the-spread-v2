using AgainstTheSpread.Core.Interfaces;
using Microsoft.Extensions.Logging;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AgainstTheSpread.Core.Services;

/// <summary>
/// Sports data provider implementation using the CollegeFootballData (CFBD) API.
/// Free tier: 1,000 requests/month.
/// API Documentation: https://api.collegefootballdata.com/api/docs
/// </summary>
public class CollegeFootballDataProvider : ISportsDataProvider
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<CollegeFootballDataProvider> _logger;
    private const string BaseUrl = "https://api.collegefootballdata.com";
    private const string PreferredLineProvider = "consensus"; // Other options: DraftKings, Bovada, etc.

    public string ProviderName => "CollegeFootballData";

    public CollegeFootballDataProvider(HttpClient httpClient, ILogger<CollegeFootballDataProvider> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<List<ExternalGame>> GetWeeklyGamesAsync(int year, int week, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Fetching games for year {Year} week {Week} from CFBD", year, week);

        try
        {
            // Get games with betting lines
            var linesUrl = $"{BaseUrl}/lines?year={year}&week={week}&seasonType=regular";
            var linesResponse = await _httpClient.GetFromJsonAsync<List<CfbdGameLine>>(linesUrl, GetJsonOptions(), cancellationToken);

            if (linesResponse == null || !linesResponse.Any())
            {
                _logger.LogWarning("No games with lines found for year {Year} week {Week}", year, week);
                return new List<ExternalGame>();
            }

            var games = new List<ExternalGame>();

            foreach (var gameLine in linesResponse)
            {
                // Find preferred line provider, fallback to first available
                var line = gameLine.Lines?
                    .FirstOrDefault(l => l.Provider?.Equals(PreferredLineProvider, StringComparison.OrdinalIgnoreCase) == true)
                    ?? gameLine.Lines?.FirstOrDefault();

                if (line == null || line.Spread == null)
                {
                    continue;
                }

                var spread = line.Spread.Value;
                var game = new ExternalGame
                {
                    GameId = gameLine.Id.ToString(),
                    HomeTeam = gameLine.HomeTeam ?? string.Empty,
                    AwayTeam = gameLine.AwayTeam ?? string.Empty,
                    GameDate = gameLine.StartDate ?? DateTime.MinValue,
                    Year = year,
                    Week = week,
                    LineProvider = line.Provider ?? "unknown"
                };

                // CFBD spread is from home team perspective (negative means home favored)
                // We convert to favorite/underdog format
                if (spread < 0)
                {
                    // Home team is favored
                    game.Favorite = gameLine.HomeTeam ?? string.Empty;
                    game.Underdog = gameLine.AwayTeam ?? string.Empty;
                    game.Line = spread;
                }
                else if (spread > 0)
                {
                    // Away team is favored
                    game.Favorite = gameLine.AwayTeam ?? string.Empty;
                    game.Underdog = gameLine.HomeTeam ?? string.Empty;
                    game.Line = -spread;
                }
                else
                {
                    // Pick'em - home team listed as favorite by convention
                    game.Favorite = gameLine.HomeTeam ?? string.Empty;
                    game.Underdog = gameLine.AwayTeam ?? string.Empty;
                    game.Line = 0;
                }

                games.Add(game);
            }

            _logger.LogInformation("Retrieved {Count} games with lines for year {Year} week {Week}", games.Count, year, week);
            return games;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error fetching games from CFBD for year {Year} week {Week}", year, week);
            throw;
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "JSON parsing error for CFBD response year {Year} week {Week}", year, week);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<List<ExternalBowlGame>> GetBowlGamesAsync(int year, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Fetching bowl games for year {Year} from CFBD", year);

        try
        {
            // Get postseason games with lines
            var linesUrl = $"{BaseUrl}/lines?year={year}&seasonType=postseason";
            var linesResponse = await _httpClient.GetFromJsonAsync<List<CfbdGameLine>>(linesUrl, GetJsonOptions(), cancellationToken);

            if (linesResponse == null || !linesResponse.Any())
            {
                _logger.LogWarning("No bowl games with lines found for year {Year}", year);
                return new List<ExternalBowlGame>();
            }

            var games = new List<ExternalBowlGame>();
            var gameNumber = 1;

            foreach (var gameLine in linesResponse.OrderBy(g => g.StartDate))
            {
                var line = gameLine.Lines?
                    .FirstOrDefault(l => l.Provider?.Equals(PreferredLineProvider, StringComparison.OrdinalIgnoreCase) == true)
                    ?? gameLine.Lines?.FirstOrDefault();

                if (line == null || line.Spread == null)
                {
                    continue;
                }

                var spread = line.Spread.Value;
                var game = new ExternalBowlGame
                {
                    GameId = gameLine.Id.ToString(),
                    BowlName = ExtractBowlName(gameLine),
                    HomeTeam = gameLine.HomeTeam ?? string.Empty,
                    AwayTeam = gameLine.AwayTeam ?? string.Empty,
                    GameDate = gameLine.StartDate ?? DateTime.MinValue,
                    Year = year,
                    LineProvider = line.Provider ?? "unknown"
                };

                if (spread < 0)
                {
                    game.Favorite = gameLine.HomeTeam ?? string.Empty;
                    game.Underdog = gameLine.AwayTeam ?? string.Empty;
                    game.Line = spread;
                }
                else if (spread > 0)
                {
                    game.Favorite = gameLine.AwayTeam ?? string.Empty;
                    game.Underdog = gameLine.HomeTeam ?? string.Empty;
                    game.Line = -spread;
                }
                else
                {
                    game.Favorite = gameLine.HomeTeam ?? string.Empty;
                    game.Underdog = gameLine.AwayTeam ?? string.Empty;
                    game.Line = 0;
                }

                games.Add(game);
                gameNumber++;
            }

            _logger.LogInformation("Retrieved {Count} bowl games with lines for year {Year}", games.Count, year);
            return games;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error fetching bowl games from CFBD for year {Year}", year);
            throw;
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "JSON parsing error for CFBD bowl games response year {Year}", year);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<ExternalGameResult?> GetGameResultAsync(string gameId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Fetching game result for gameId {GameId} from CFBD", gameId);

        try
        {
            var url = $"{BaseUrl}/games?id={gameId}";
            var response = await _httpClient.GetFromJsonAsync<List<CfbdGame>>(url, GetJsonOptions(), cancellationToken);

            var game = response?.FirstOrDefault();
            if (game == null)
            {
                _logger.LogWarning("Game {GameId} not found in CFBD", gameId);
                return null;
            }

            return new ExternalGameResult
            {
                GameId = game.Id.ToString(),
                HomeTeam = game.HomeTeam ?? string.Empty,
                AwayTeam = game.AwayTeam ?? string.Empty,
                HomeScore = game.HomePoints ?? 0,
                AwayScore = game.AwayPoints ?? 0,
                IsCompleted = game.Completed == true,
                Year = game.Season,
                Week = game.Week
            };
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error fetching game result from CFBD for gameId {GameId}", gameId);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<List<ExternalGameResult>> GetWeeklyResultsAsync(int year, int week, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Fetching results for year {Year} week {Week} from CFBD", year, week);

        try
        {
            var url = $"{BaseUrl}/games?year={year}&week={week}&seasonType=regular";
            var response = await _httpClient.GetFromJsonAsync<List<CfbdGame>>(url, GetJsonOptions(), cancellationToken);

            if (response == null || !response.Any())
            {
                _logger.LogWarning("No games found for year {Year} week {Week}", year, week);
                return new List<ExternalGameResult>();
            }

            var results = response
                .Where(g => g.Completed == true)
                .Select(g => new ExternalGameResult
                {
                    GameId = g.Id.ToString(),
                    HomeTeam = g.HomeTeam ?? string.Empty,
                    AwayTeam = g.AwayTeam ?? string.Empty,
                    HomeScore = g.HomePoints ?? 0,
                    AwayScore = g.AwayPoints ?? 0,
                    IsCompleted = true,
                    Year = g.Season,
                    Week = g.Week
                })
                .ToList();

            _logger.LogInformation("Retrieved {Count} completed game results for year {Year} week {Week}", results.Count, year, week);
            return results;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error fetching results from CFBD for year {Year} week {Week}", year, week);
            throw;
        }
    }

    private static string ExtractBowlName(CfbdGameLine gameLine)
    {
        // Try to extract bowl name from notes or construct from game data
        if (!string.IsNullOrEmpty(gameLine.Notes))
        {
            return gameLine.Notes;
        }

        // Fallback: generate name from teams
        return $"{gameLine.AwayTeam} vs {gameLine.HomeTeam}";
    }

    private static JsonSerializerOptions GetJsonOptions()
    {
        return new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };
    }
}

#region CFBD API Models

/// <summary>
/// CFBD API game line response model.
/// </summary>
internal class CfbdGameLine
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("homeTeam")]
    public string? HomeTeam { get; set; }

    [JsonPropertyName("awayTeam")]
    public string? AwayTeam { get; set; }

    [JsonPropertyName("startDate")]
    public DateTime? StartDate { get; set; }

    [JsonPropertyName("notes")]
    public string? Notes { get; set; }

    [JsonPropertyName("lines")]
    public List<CfbdLine>? Lines { get; set; }
}

/// <summary>
/// CFBD API betting line model.
/// </summary>
internal class CfbdLine
{
    [JsonPropertyName("provider")]
    public string? Provider { get; set; }

    [JsonPropertyName("spread")]
    public decimal? Spread { get; set; }

    [JsonPropertyName("overUnder")]
    public decimal? OverUnder { get; set; }
}

/// <summary>
/// CFBD API game model for results.
/// </summary>
internal class CfbdGame
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("season")]
    public int Season { get; set; }

    [JsonPropertyName("week")]
    public int? Week { get; set; }

    [JsonPropertyName("homeTeam")]
    public string? HomeTeam { get; set; }

    [JsonPropertyName("awayTeam")]
    public string? AwayTeam { get; set; }

    [JsonPropertyName("homePoints")]
    public int? HomePoints { get; set; }

    [JsonPropertyName("awayPoints")]
    public int? AwayPoints { get; set; }

    [JsonPropertyName("completed")]
    public bool? Completed { get; set; }

    [JsonPropertyName("startDate")]
    public DateTime? StartDate { get; set; }
}

#endregion
