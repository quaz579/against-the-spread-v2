using AgainstTheSpread.Core.Models;
using Microsoft.Extensions.Logging;
using System.Net.Http.Json;

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
                return await response.Content.ReadFromJsonAsync<SubmitResultsResponse>();
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
    }

    public class SingleResultResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}
