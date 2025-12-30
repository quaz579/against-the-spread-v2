using AgainstTheSpread.Data.Interfaces;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Text.Json;

namespace AgainstTheSpread.Functions;

/// <summary>
/// Azure Functions for leaderboard endpoints
/// </summary>
public class LeaderboardFunction
{
    private readonly ILeaderboardService _leaderboardService;
    private readonly ILogger<LeaderboardFunction> _logger;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public LeaderboardFunction(ILeaderboardService leaderboardService, ILogger<LeaderboardFunction> logger)
    {
        _leaderboardService = leaderboardService;
        _logger = logger;
    }

    /// <summary>
    /// Get weekly leaderboard for a specific week
    /// </summary>
    [Function("GetWeeklyLeaderboard")]
    public async Task<HttpResponseData> GetWeeklyLeaderboard(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "leaderboard/week/{week}")] HttpRequestData req,
        int week,
        CancellationToken cancellationToken)
    {
        // Parse year from query string
        var query = System.Web.HttpUtility.ParseQueryString(req.Url.Query);
        var yearStr = query["year"];
        var year = string.IsNullOrEmpty(yearStr) ? DateTime.Now.Year : int.Parse(yearStr);

        _logger.LogInformation("Getting weekly leaderboard for year {Year}, week {Week}", year, week);

        var entries = await _leaderboardService.GetWeeklyLeaderboardAsync(year, week, cancellationToken);

        var response = req.CreateResponse(HttpStatusCode.OK);
        response.Headers.Add("Content-Type", "application/json");

        var result = new WeeklyLeaderboardResponse
        {
            Year = year,
            Week = week,
            Entries = entries.Select(e => new WeeklyLeaderboardEntryDto
            {
                UserId = e.UserId,
                DisplayName = e.DisplayName,
                Week = e.Week,
                Wins = e.Wins,
                Losses = e.Losses,
                Pushes = e.Pushes,
                WinPercentage = e.WinPercentage
            }).ToList()
        };

        await response.WriteStringAsync(JsonSerializer.Serialize(result, JsonOptions));
        return response;
    }

    /// <summary>
    /// Get season leaderboard
    /// </summary>
    [Function("GetSeasonLeaderboard")]
    public async Task<HttpResponseData> GetSeasonLeaderboard(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "leaderboard/season")] HttpRequestData req,
        CancellationToken cancellationToken)
    {
        // Parse year from query string
        var query = System.Web.HttpUtility.ParseQueryString(req.Url.Query);
        var yearStr = query["year"];
        var year = string.IsNullOrEmpty(yearStr) ? DateTime.Now.Year : int.Parse(yearStr);

        _logger.LogInformation("Getting season leaderboard for year {Year}", year);

        var entries = await _leaderboardService.GetSeasonLeaderboardAsync(year, cancellationToken);

        var response = req.CreateResponse(HttpStatusCode.OK);
        response.Headers.Add("Content-Type", "application/json");

        var result = new SeasonLeaderboardResponse
        {
            Year = year,
            Entries = entries.Select(e => new SeasonLeaderboardEntryDto
            {
                UserId = e.UserId,
                DisplayName = e.DisplayName,
                TotalWins = e.TotalWins,
                TotalLosses = e.TotalLosses,
                TotalPushes = e.TotalPushes,
                WinPercentage = e.WinPercentage,
                WeeksPlayed = e.WeeksPlayed,
                PerfectWeeks = e.PerfectWeeks
            }).ToList()
        };

        await response.WriteStringAsync(JsonSerializer.Serialize(result, JsonOptions));
        return response;
    }

    /// <summary>
    /// Get user's season pick history
    /// </summary>
    [Function("GetUserSeasonHistory")]
    public async Task<HttpResponseData> GetUserSeasonHistory(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "leaderboard/user/{userId}")] HttpRequestData req,
        string userId,
        CancellationToken cancellationToken)
    {
        // Parse year from query string
        var query = System.Web.HttpUtility.ParseQueryString(req.Url.Query);
        var yearStr = query["year"];
        var year = string.IsNullOrEmpty(yearStr) ? DateTime.Now.Year : int.Parse(yearStr);

        if (!Guid.TryParse(userId, out var userGuid))
        {
            var badRequest = req.CreateResponse(HttpStatusCode.BadRequest);
            await badRequest.WriteStringAsync("Invalid user ID format");
            return badRequest;
        }

        _logger.LogInformation("Getting season history for user {UserId}, year {Year}", userId, year);

        var history = await _leaderboardService.GetUserSeasonHistoryAsync(userGuid, year, cancellationToken);

        if (history == null)
        {
            var notFound = req.CreateResponse(HttpStatusCode.NotFound);
            await notFound.WriteStringAsync("User not found");
            return notFound;
        }

        var response = req.CreateResponse(HttpStatusCode.OK);
        response.Headers.Add("Content-Type", "application/json");

        var result = new UserSeasonHistoryDto
        {
            UserId = history.UserId,
            DisplayName = history.DisplayName,
            Year = history.Year,
            TotalWins = history.TotalWins,
            TotalLosses = history.TotalLosses,
            TotalPushes = history.TotalPushes,
            WinPercentage = history.WinPercentage,
            Weeks = history.Weeks.Select(w => new UserWeekHistoryDto
            {
                Week = w.Week,
                Wins = w.Wins,
                Losses = w.Losses,
                Pushes = w.Pushes,
                IsPerfect = w.IsPerfect,
                Picks = w.Picks.Select(p => new UserPickResultDto
                {
                    GameId = p.GameId,
                    Favorite = p.Favorite,
                    Underdog = p.Underdog,
                    Line = p.Line,
                    SelectedTeam = p.SelectedTeam,
                    SpreadWinner = p.SpreadWinner,
                    IsPush = p.IsPush,
                    IsWin = p.IsWin,
                    HasResult = p.HasResult
                }).ToList()
            }).ToList()
        };

        await response.WriteStringAsync(JsonSerializer.Serialize(result, JsonOptions));
        return response;
    }

    /// <summary>
    /// Get my (authenticated user's) season history
    /// </summary>
    [Function("GetMySeasonHistory")]
    public async Task<HttpResponseData> GetMySeasonHistory(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "leaderboard/me")] HttpRequestData req,
        CancellationToken cancellationToken)
    {
        // Get user from SWA authentication header
        var clientPrincipalHeader = req.Headers.TryGetValues("X-MS-CLIENT-PRINCIPAL-ID", out var principalValues)
            ? principalValues.FirstOrDefault()
            : null;

        if (string.IsNullOrEmpty(clientPrincipalHeader) || !Guid.TryParse(clientPrincipalHeader, out var userId))
        {
            var unauthorized = req.CreateResponse(HttpStatusCode.Unauthorized);
            await unauthorized.WriteStringAsync("Authentication required");
            return unauthorized;
        }

        // Parse year from query string
        var query = System.Web.HttpUtility.ParseQueryString(req.Url.Query);
        var yearStr = query["year"];
        var year = string.IsNullOrEmpty(yearStr) ? DateTime.Now.Year : int.Parse(yearStr);

        _logger.LogInformation("Getting season history for authenticated user {UserId}, year {Year}", userId, year);

        var history = await _leaderboardService.GetUserSeasonHistoryAsync(userId, year, cancellationToken);

        if (history == null)
        {
            // Return empty history for new user
            var emptyResponse = req.CreateResponse(HttpStatusCode.OK);
            emptyResponse.Headers.Add("Content-Type", "application/json");
            await emptyResponse.WriteStringAsync(JsonSerializer.Serialize(new UserSeasonHistoryDto
            {
                UserId = userId,
                DisplayName = "Unknown",
                Year = year
            }, JsonOptions));
            return emptyResponse;
        }

        var response = req.CreateResponse(HttpStatusCode.OK);
        response.Headers.Add("Content-Type", "application/json");

        var result = new UserSeasonHistoryDto
        {
            UserId = history.UserId,
            DisplayName = history.DisplayName,
            Year = history.Year,
            TotalWins = history.TotalWins,
            TotalLosses = history.TotalLosses,
            TotalPushes = history.TotalPushes,
            WinPercentage = history.WinPercentage,
            Weeks = history.Weeks.Select(w => new UserWeekHistoryDto
            {
                Week = w.Week,
                Wins = w.Wins,
                Losses = w.Losses,
                Pushes = w.Pushes,
                IsPerfect = w.IsPerfect,
                Picks = w.Picks.Select(p => new UserPickResultDto
                {
                    GameId = p.GameId,
                    Favorite = p.Favorite,
                    Underdog = p.Underdog,
                    Line = p.Line,
                    SelectedTeam = p.SelectedTeam,
                    SpreadWinner = p.SpreadWinner,
                    IsPush = p.IsPush,
                    IsWin = p.IsWin,
                    HasResult = p.HasResult
                }).ToList()
            }).ToList()
        };

        await response.WriteStringAsync(JsonSerializer.Serialize(result, JsonOptions));
        return response;
    }

    #region DTOs

    private class WeeklyLeaderboardResponse
    {
        public int Year { get; set; }
        public int Week { get; set; }
        public List<WeeklyLeaderboardEntryDto> Entries { get; set; } = new();
    }

    private class WeeklyLeaderboardEntryDto
    {
        public Guid UserId { get; set; }
        public string DisplayName { get; set; } = string.Empty;
        public int Week { get; set; }
        public decimal Wins { get; set; }
        public decimal Losses { get; set; }
        public int Pushes { get; set; }
        public decimal WinPercentage { get; set; }
    }

    private class SeasonLeaderboardResponse
    {
        public int Year { get; set; }
        public List<SeasonLeaderboardEntryDto> Entries { get; set; } = new();
    }

    private class SeasonLeaderboardEntryDto
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

    private class UserSeasonHistoryDto
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

    private class UserWeekHistoryDto
    {
        public int Week { get; set; }
        public decimal Wins { get; set; }
        public decimal Losses { get; set; }
        public int Pushes { get; set; }
        public bool IsPerfect { get; set; }
        public List<UserPickResultDto> Picks { get; set; } = new();
    }

    private class UserPickResultDto
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

    #endregion
}
