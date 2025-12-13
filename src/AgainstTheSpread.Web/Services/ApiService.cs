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
}
