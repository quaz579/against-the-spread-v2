using AgainstTheSpread.Core.Models;
using Microsoft.Extensions.Logging;
using System.Net.Http.Json;
using System.Text.Json;

namespace AgainstTheSpread.Web.Services;

/// <summary>
/// Service for calling the Azure Functions API
/// </summary>
public class ApiService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ApiService> _logger;

    public ApiService(HttpClient httpClient, ILogger<ApiService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    // ============ Configuration Methods ============

    /// <summary>
    /// Get application configuration flags
    /// </summary>
    public async Task<ConfigResponse?> GetConfigAsync()
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<ConfigResponse>("api/config");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get config");
            return null;
        }
    }

    /// <summary>
    /// Get list of available weeks for a given year
    /// </summary>
    public async Task<List<int>> GetAvailableWeeksAsync(int year)
    {
        try
        {
            _logger.LogInformation("Calling API: api/weeks?year={Year}", year);
            _logger.LogDebug("HttpClient BaseAddress: {BaseAddress}", _httpClient.BaseAddress);
            
            var response = await _httpClient.GetFromJsonAsync<WeeksResponse>($"api/weeks?year={year}");
            
            _logger.LogDebug("Response received: {ResponseStatus}", response != null ? "not null" : "null");
            if (response != null)
            {
                _logger.LogDebug("Response.Year: {Year}, Response.Weeks.Count: {Count}", 
                    response.Year, response.Weeks?.Count ?? 0);
            }
            
            var result = response?.Weeks ?? new List<int>();
            _logger.LogInformation("Returning {Count} weeks for year {Year}", result.Count, year);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception calling API for year {Year}", year);
            return new List<int>();
        }
    }

    /// <summary>
    /// Get lines for a specific week
    /// </summary>
    public async Task<WeeklyLines?> GetLinesAsync(int week, int year)
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<WeeklyLines>($"api/lines/{week}?year={year}");
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Submit user picks and download Excel file
    /// </summary>
    public async Task<byte[]?> SubmitPicksAsync(UserPicks userPicks)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("api/picks", userPicks);

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadAsByteArrayAsync();
            }

            return null;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Upload weekly lines file
    /// </summary>
    public async Task<UploadResponse?> UploadLinesAsync(int week, int year, Stream fileStream, string fileName)
    {
        try
        {
            using var content = new MultipartFormDataContent();
            var streamContent = new StreamContent(fileStream);
            streamContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
            content.Add(streamContent, "file", fileName);

            var response = await _httpClient.PostAsync($"api/upload-lines?week={week}&year={year}", content);

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<UploadResponse>();
            }

            return null;
        }
        catch
        {
            return null;
        }
    }

    // ============ Bowl Game Methods ============

    /// <summary>
    /// Get bowl lines for a specific year
    /// </summary>
    public async Task<BowlLines?> GetBowlLinesAsync(int year)
    {
        try
        {
            _logger.LogInformation("Calling API: api/bowl-lines?year={Year}", year);
            return await _httpClient.GetFromJsonAsync<BowlLines>($"api/bowl-lines?year={year}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception calling bowl lines API for year {Year}", year);
            return null;
        }
    }

    /// <summary>
    /// Check if bowl lines exist for a specific year
    /// </summary>
    public async Task<bool> BowlLinesExistAsync(int year)
    {
        try
        {
            var response = await _httpClient.GetFromJsonAsync<BowlLinesExistsResponse>($"api/bowl-lines/exists?year={year}");
            return response?.Exists ?? false;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to check bowl lines existence for year {Year}", year);
            return false;
        }
    }

    /// <summary>
    /// Submit bowl picks and download Excel file
    /// </summary>
    public async Task<byte[]?> SubmitBowlPicksAsync(BowlUserPicks bowlPicks)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("api/bowl-picks", bowlPicks);

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadAsByteArrayAsync();
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to submit bowl picks for {Name}", bowlPicks.Name);
            return null;
        }
    }

    /// <summary>
    /// Upload bowl lines file
    /// </summary>
    public async Task<BowlUploadResponse?> UploadBowlLinesAsync(int year, Stream fileStream, string fileName)
    {
        try
        {
            using var content = new MultipartFormDataContent();
            var streamContent = new StreamContent(fileStream);
            streamContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
            content.Add(streamContent, "file", fileName);

            var response = await _httpClient.PostAsync($"api/upload-bowl-lines?year={year}", content);

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<BowlUploadResponse>();
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to upload bowl lines for year {Year}", year);
            return null;
        }
    }

    private class WeeksResponse
    {
        public int Year { get; set; }
        public List<int> Weeks { get; set; } = new();
    }

    public class ConfigResponse
    {
        public bool GameLockingDisabled { get; set; }
    }

    private class BowlLinesExistsResponse
    {
        public int Year { get; set; }
        public bool Exists { get; set; }
    }

    public class UploadResponse
    {
        public bool Success { get; set; }
        public int Week { get; set; }
        public int Year { get; set; }
        public int GamesCount { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    public class BowlUploadResponse
    {
        public bool Success { get; set; }
        public int Year { get; set; }
        public int GamesCount { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    // ============ Games from Database ============

    /// <summary>
    /// Get games from the database for a specific week with IDs and lock status.
    /// Used for authenticated pick submission.
    /// </summary>
    public async Task<GamesResponse?> GetGamesAsync(int week, int year)
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<GamesResponse>($"api/games/{week}?year={year}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get games for week {Week}, year {Year}", week, year);
            return null;
        }
    }

    public class GamesResponse
    {
        public int Year { get; set; }
        public int Week { get; set; }
        public int TotalGames { get; set; }
        public List<GameDto> Games { get; set; } = new();
    }

    public class GameDto
    {
        public int Id { get; set; }
        public string Favorite { get; set; } = string.Empty;
        public string Underdog { get; set; } = string.Empty;
        public decimal Line { get; set; }
        public DateTime GameDate { get; set; }
        public bool IsLocked { get; set; }
        public bool HasResult { get; set; }
        public string? SpreadWinner { get; set; }
        public bool? IsPush { get; set; }
    }

    // ============ Authenticated User Picks Methods ============

    /// <summary>
    /// Submit picks to the database for authenticated users
    /// </summary>
    public async Task<UserPicksResponse?> SubmitUserPicksAsync(int year, int week, List<PickSubmission> picks)
    {
        try
        {
            var request = new UserPicksRequest
            {
                Year = year,
                Week = week,
                Picks = picks
            };

            var response = await _httpClient.PostAsJsonAsync("api/user-picks", request);

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<UserPicksResponse>();
            }

            var errorContent = await response.Content.ReadAsStringAsync();
            _logger.LogWarning("Failed to submit user picks: {StatusCode} - {Content}",
                response.StatusCode, errorContent);

            return new UserPicksResponse
            {
                Success = false,
                Message = $"Server error: {response.StatusCode}"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception submitting user picks for week {Week}, year {Year}", week, year);
            return new UserPicksResponse
            {
                Success = false,
                Message = $"Error: {ex.Message}"
            };
        }
    }

    /// <summary>
    /// Get the authenticated user's picks for a specific week
    /// </summary>
    public async Task<List<UserPickDto>?> GetUserPicksAsync(int year, int week)
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<List<UserPickDto>>($"api/user-picks/{week}?year={year}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get user picks for week {Week}, year {Year}", week, year);
            return null;
        }
    }

    /// <summary>
    /// Get the authenticated user's picks for an entire season
    /// </summary>
    public async Task<List<UserPickDto>?> GetUserSeasonPicksAsync(int year)
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<List<UserPickDto>>($"api/user-picks?year={year}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get user season picks for year {Year}", year);
            return null;
        }
    }

    // DTOs for user picks
    public class PickSubmission
    {
        public int GameId { get; set; }
        public string SelectedTeam { get; set; } = string.Empty;
    }

    public class UserPicksRequest
    {
        public int Year { get; set; }
        public int Week { get; set; }
        public List<PickSubmission> Picks { get; set; } = new();
    }

    public class UserPicksResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public int PicksSubmitted { get; set; }
        public int PicksUpdated { get; set; }
        public List<string> LockedGames { get; set; } = new();
    }

    public class UserPickDto
    {
        public int GameId { get; set; }
        public string SelectedTeam { get; set; } = string.Empty;
        public DateTime SubmittedAt { get; set; }
        public string Favorite { get; set; } = string.Empty;
        public string Underdog { get; set; } = string.Empty;
        public decimal Line { get; set; }
        public DateTime GameDate { get; set; }
        public bool IsLocked { get; set; }
    }

    // ============ Results Methods ============

    /// <summary>
    /// Get results for a specific week
    /// </summary>
    public async Task<WeekResultsResponse?> GetResultsAsync(int week, int year)
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<WeekResultsResponse>($"api/results/{week}?year={year}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get results for week {Week}, year {Year}", week, year);
            return null;
        }
    }

    /// <summary>
    /// Submit results for a week (admin only)
    /// </summary>
    public async Task<SubmitResultsResponse?> SubmitResultsAsync(int week, int year, List<ResultInput> results)
    {
        try
        {
            var request = new SubmitResultsRequest { Results = results };
            var response = await _httpClient.PostAsJsonAsync($"api/results/{week}?year={year}", request);

            if (response.IsSuccessStatusCode)
            {
                var jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                return await response.Content.ReadFromJsonAsync<SubmitResultsResponse>(jsonOptions);
            }

            var errorContent = await response.Content.ReadAsStringAsync();
            _logger.LogWarning("Failed to submit results: {StatusCode} - {Content}",
                response.StatusCode, errorContent);

            return new SubmitResultsResponse
            {
                Success = false,
                Message = $"Server error: {response.StatusCode}"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception submitting results for week {Week}, year {Year}", week, year);
            return new SubmitResultsResponse
            {
                Success = false,
                Message = $"Error: {ex.Message}"
            };
        }
    }

    /// <summary>
    /// Submit result for a single game (admin only)
    /// </summary>
    public async Task<SingleResultResponse?> SubmitSingleResultAsync(int gameId, int favoriteScore, int underdogScore)
    {
        try
        {
            var request = new { FavoriteScore = favoriteScore, UnderdogScore = underdogScore };
            var response = await _httpClient.PostAsJsonAsync($"api/results/game/{gameId}", request);

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<SingleResultResponse>();
            }

            return new SingleResultResponse { Success = false };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception submitting result for game {GameId}", gameId);
            return new SingleResultResponse { Success = false };
        }
    }

    // DTOs for results
    public class WeekResultsResponse
    {
        public int Year { get; set; }
        public int Week { get; set; }
        public int TotalGames { get; set; }
        public int GamesWithResults { get; set; }
        public List<GameResultDto> Games { get; set; } = new();
    }

    public class GameResultDto
    {
        public int Id { get; set; }
        public string Favorite { get; set; } = string.Empty;
        public string Underdog { get; set; } = string.Empty;
        public decimal Line { get; set; }
        public DateTime GameDate { get; set; }
        public bool IsLocked { get; set; }
        public bool HasResult { get; set; }
        public int? FavoriteScore { get; set; }
        public int? UnderdogScore { get; set; }
        public string? SpreadWinner { get; set; }
        public bool? IsPush { get; set; }
        public DateTime? ResultEnteredAt { get; set; }
    }

    public class SubmitResultsRequest
    {
        public List<ResultInput> Results { get; set; } = new();
    }

    public class ResultInput
    {
        public int GameId { get; set; }
        public int FavoriteScore { get; set; }
        public int UnderdogScore { get; set; }
    }

    public class SubmitResultsResponse
    {
        public bool Success { get; set; }
        public int ResultsEntered { get; set; }
        public int ResultsFailed { get; set; }
        public string Message { get; set; } = string.Empty;
        public List<FailedResultEntry>? FailedResults { get; set; }
    }

    public class FailedResultEntry
    {
        public int GameId { get; set; }
        public string Reason { get; set; } = string.Empty;
    }

    public class SingleResultResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    // ============ Leaderboard Methods ============

    /// <summary>
    /// Get weekly leaderboard for a specific week
    /// </summary>
    public async Task<WeeklyLeaderboardResponse?> GetWeeklyLeaderboardAsync(int week, int year)
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<WeeklyLeaderboardResponse>($"api/leaderboard/week/{week}?year={year}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get weekly leaderboard for week {Week}, year {Year}", week, year);
            return null;
        }
    }

    /// <summary>
    /// Get season leaderboard
    /// </summary>
    public async Task<SeasonLeaderboardResponse?> GetSeasonLeaderboardAsync(int year)
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<SeasonLeaderboardResponse>($"api/leaderboard/season?year={year}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get season leaderboard for year {Year}", year);
            return null;
        }
    }

    /// <summary>
    /// Get a specific user's season pick history
    /// </summary>
    public async Task<UserSeasonHistoryDto?> GetUserSeasonHistoryAsync(Guid userId, int year)
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<UserSeasonHistoryDto>($"api/leaderboard/user/{userId}?year={year}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get user season history for user {UserId}, year {Year}", userId, year);
            return null;
        }
    }

    /// <summary>
    /// Get authenticated user's own season pick history
    /// </summary>
    public async Task<UserSeasonHistoryDto?> GetMySeasonHistoryAsync(int year)
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<UserSeasonHistoryDto>($"api/leaderboard/me?year={year}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get my season history for year {Year}", year);
            return null;
        }
    }

    // DTOs for leaderboard
    public class WeeklyLeaderboardResponse
    {
        public int Year { get; set; }
        public int Week { get; set; }
        public List<WeeklyLeaderboardEntryDto> Entries { get; set; } = new();
    }

    public class WeeklyLeaderboardEntryDto
    {
        public Guid UserId { get; set; }
        public string DisplayName { get; set; } = string.Empty;
        public int Week { get; set; }
        public decimal Wins { get; set; }
        public decimal Losses { get; set; }
        public int Pushes { get; set; }
        public decimal WinPercentage { get; set; }
    }

    public class SeasonLeaderboardResponse
    {
        public int Year { get; set; }
        public List<SeasonLeaderboardEntryDto> Entries { get; set; } = new();
    }

    public class SeasonLeaderboardEntryDto
    {
        public Guid UserId { get; set; }
        public string DisplayName { get; set; } = string.Empty;
        public decimal TotalWins { get; set; }
        public decimal TotalLosses { get; set; }
        public int TotalPushes { get; set; }
        public decimal WinPercentage { get; set; }
        public int WeeksPlayed { get; set; }
        public int PerfectWeeks { get; set; }
    }

    public class UserSeasonHistoryDto
    {
        public Guid UserId { get; set; }
        public string DisplayName { get; set; } = string.Empty;
        public int Year { get; set; }
        public decimal TotalWins { get; set; }
        public decimal TotalLosses { get; set; }
        public int TotalPushes { get; set; }
        public decimal WinPercentage { get; set; }
        public List<UserWeekHistoryDto> Weeks { get; set; } = new();
    }

    public class UserWeekHistoryDto
    {
        public int Week { get; set; }
        public decimal Wins { get; set; }
        public decimal Losses { get; set; }
        public int Pushes { get; set; }
        public bool IsPerfect { get; set; }
        public List<UserPickHistoryDto> Picks { get; set; } = new();
    }

    public class UserPickHistoryDto
    {
        public int GameId { get; set; }
        public string Favorite { get; set; } = string.Empty;
        public string Underdog { get; set; } = string.Empty;
        public decimal Line { get; set; }
        public string SelectedTeam { get; set; } = string.Empty;
        public string? SpreadWinner { get; set; }
        public bool? IsPush { get; set; }
        public bool? IsWin { get; set; }
        public bool HasResult { get; set; }
    }

    // ============ Sports Data Sync Methods ============

    /// <summary>
    /// Get the status of the sports data provider configuration
    /// </summary>
    public async Task<SyncStatusResponse?> GetSyncStatusAsync()
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<SyncStatusResponse>("api/sync/status");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get sync status");
            return null;
        }
    }

    /// <summary>
    /// Sync weekly games from CFBD API (admin only)
    /// </summary>
    public async Task<SyncResponse?> SyncWeeklyGamesAsync(int week, int year)
    {
        try
        {
            var response = await _httpClient.PostAsync($"api/sync/games/{week}?year={year}", null);
            var jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<SyncResponse>(jsonOptions);
            }

            // Try to parse error response to get actual message
            var errorContent = await response.Content.ReadAsStringAsync();
            _logger.LogWarning("Failed to sync weekly games: {StatusCode} - {Content}",
                response.StatusCode, errorContent);

            var errorMessage = ExtractErrorMessage(errorContent)
                ?? (response.StatusCode == System.Net.HttpStatusCode.ServiceUnavailable
                    ? "Sports data provider not configured. Please set CFBD_API_KEY."
                    : $"Server error: {response.StatusCode}");

            return new SyncResponse
            {
                Success = false,
                Message = errorMessage
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception syncing weekly games for week {Week}, year {Year}", week, year);
            return new SyncResponse
            {
                Success = false,
                Message = $"Error: {ex.Message}"
            };
        }
    }

    /// <summary>
    /// Sync bowl games from CFBD API (admin only)
    /// </summary>
    public async Task<SyncResponse?> SyncBowlGamesAsync(int year)
    {
        try
        {
            var response = await _httpClient.PostAsync($"api/sync/bowl-games?year={year}", null);
            var jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<SyncResponse>(jsonOptions);
            }

            // Try to parse error response to get actual message
            var errorContent = await response.Content.ReadAsStringAsync();
            _logger.LogWarning("Failed to sync bowl games: {StatusCode} - {Content}",
                response.StatusCode, errorContent);

            var errorMessage = ExtractErrorMessage(errorContent)
                ?? (response.StatusCode == System.Net.HttpStatusCode.ServiceUnavailable
                    ? "Sports data provider not configured. Please set CFBD_API_KEY."
                    : $"Server error: {response.StatusCode}");

            return new SyncResponse
            {
                Success = false,
                Message = errorMessage
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception syncing bowl games for year {Year}", year);
            return new SyncResponse
            {
                Success = false,
                Message = $"Error: {ex.Message}"
            };
        }
    }

    /// <summary>
    /// Sync weekly game results from CFBD API (admin only)
    /// </summary>
    public async Task<SyncResultsResponse?> SyncWeeklyResultsAsync(int week, int year)
    {
        try
        {
            var response = await _httpClient.PostAsync($"api/sync/results/{week}?year={year}", null);
            var jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<SyncResultsResponse>(jsonOptions);
            }

            // Try to parse error response to get actual message
            var errorContent = await response.Content.ReadAsStringAsync();
            _logger.LogWarning("Failed to sync weekly results: {StatusCode} - {Content}",
                response.StatusCode, errorContent);

            var errorMessage = ExtractErrorMessage(errorContent)
                ?? (response.StatusCode == System.Net.HttpStatusCode.ServiceUnavailable
                    ? "Sports data provider not configured. Please set CFBD_API_KEY."
                    : $"Server error: {response.StatusCode}");

            return new SyncResultsResponse
            {
                Success = false,
                Message = errorMessage
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception syncing weekly results for week {Week}, year {Year}", week, year);
            return new SyncResultsResponse
            {
                Success = false,
                Message = $"Error: {ex.Message}"
            };
        }
    }

    /// <summary>
    /// Extract error message from JSON response body
    /// </summary>
    private static string? ExtractErrorMessage(string responseContent)
    {
        if (string.IsNullOrWhiteSpace(responseContent))
            return null;

        try
        {
            using var doc = JsonDocument.Parse(responseContent);
            var root = doc.RootElement;

            // Try "message" property first (new format)
            if (root.TryGetProperty("message", out var messageElement))
            {
                return messageElement.GetString();
            }

            // Fall back to "error" property (legacy format)
            if (root.TryGetProperty("error", out var errorElement))
            {
                return errorElement.GetString();
            }
        }
        catch (JsonException)
        {
            // Not valid JSON, return null
        }

        return null;
    }

    // DTOs for sync
    public class SyncStatusResponse
    {
        public string Provider { get; set; } = string.Empty;
        public bool IsConfigured { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    public class SyncResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string? Provider { get; set; }
        public int? Year { get; set; }
        public int? Week { get; set; }
        public int GamesSynced { get; set; }
        public int GamesFound { get; set; }
    }

    public class SyncResultsResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string? Provider { get; set; }
        public int? Year { get; set; }
        public int? Week { get; set; }
        public int ResultsSynced { get; set; }
        public int GamesNotFound { get; set; }
        public int GamesSkipped { get; set; }
        public List<string>? UnmatchedGames { get; set; }
    }
}
