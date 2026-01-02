using AgainstTheSpread.Web.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.JSInterop;
using System.Text.Json;

namespace AgainstTheSpread.Web.Pages;

public partial class Admin : ComponentBase
{
    [Inject]
    private ApiService ApiService { get; set; } = default!;

    [Inject]
    private IJSRuntime JS { get; set; } = default!;

    [Inject]
    private HttpClient Http { get; set; } = default!;

    [Inject]
    private NavigationManager Navigation { get; set; } = default!;

    [Inject]
    private ILogger<Admin> Logger { get; set; } = default!;

    private bool isAuthenticated = false;
    private bool isAuthorized = false;
    private string? userEmail;
    private int weekNumber = 1;
    private int year = DateTime.Now.Year;
    private IBrowserFile? selectedFile;
    private bool isUploading = false;
    private string? errorMessage;
    private string? successMessage;

    // Bowl upload fields
    private int bowlYear = DateTime.Now.Year;
    private IBrowserFile? selectedBowlFile;
    private bool isBowlUploading = false;
    private string? bowlErrorMessage;
    private string? bowlSuccessMessage;

    // Results entry fields
    private int resultsWeek = 1;
    private int resultsYear = DateTime.Now.Year;
    private bool isLoadingGames = false;
    private bool isSubmittingResults = false;
    private string? resultsErrorMessage;
    private string? resultsSuccessMessage;
    private List<ApiService.GameResultDto>? gamesForResults;
    private Dictionary<int, ScoreEntry> gameScores = new();

    // CFBD Sync fields
    private int syncWeek = 1;
    private int syncYear = DateTime.Now.Year;
    private int syncBowlYear = DateTime.Now.Year;
    private int syncResultsWeek = 1;
    private int syncResultsYear = DateTime.Now.Year;
    private bool isCheckingStatus = false;
    private bool isSyncingWeekly = false;
    private bool isSyncingBowl = false;
    private bool isSyncingResults = false;
    private string? syncErrorMessage;
    private string? syncSuccessMessage;
    private ApiService.SyncStatusResponse? syncStatus;

    protected override async Task OnInitializedAsync()
    {
        await CheckAuthStatus();
    }

    private async Task CheckAuthStatus()
    {
        try
        {
            // Create a new HttpClient with the host base address for auth endpoints
            using var authClient = new HttpClient { BaseAddress = new Uri(Navigation.BaseUri) };

            var response = await authClient.GetAsync(".auth/me");
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                Logger.LogDebug("Auth response: {AuthResponse}", json);

                var authInfo = JsonSerializer.Deserialize<AuthMeResponse>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (authInfo?.ClientPrincipal != null)
                {
                    isAuthenticated = true;
                    userEmail = authInfo.ClientPrincipal.UserDetails;

                    // For now, assume authenticated users are authorized
                    // The backend will do the actual email validation
                    isAuthorized = true;
                }
                else
                {
                    Logger.LogWarning("No client principal in auth response");
                }
            }
            else
            {
                Logger.LogWarning("Auth check failed with status code: {StatusCode}", response.StatusCode);
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error checking authentication status");
        }
    }

    private void Login()
    {
        // Use absolute URL for redirect to ensure it works in staging environments
        var redirectUri = new Uri(new Uri(Navigation.BaseUri), "/admin").ToString();
        Navigation.NavigateTo($"/.auth/login/google?post_login_redirect_uri={Uri.EscapeDataString(redirectUri)}", true);
    }

    private void Logout()
    {
        // Use absolute URL for redirect to ensure it works in staging environments
        var redirectUri = new Uri(new Uri(Navigation.BaseUri), "/").ToString();
        Navigation.NavigateTo($"/.auth/logout?post_logout_redirect_uri={Uri.EscapeDataString(redirectUri)}", true);
    }

    private void OnFileSelected(InputFileChangeEventArgs e)
    {
        selectedFile = e.File;
        ClearMessages();
    }

    private async Task UploadFile()
    {
        if (selectedFile == null || weekNumber < 1)
            return;

        isUploading = true;
        ClearMessages();

        try
        {
            // Read file stream
            using var stream = selectedFile.OpenReadStream(maxAllowedSize: 10 * 1024 * 1024); // 10 MB limit
            using var memoryStream = new MemoryStream();
            await stream.CopyToAsync(memoryStream);
            memoryStream.Position = 0;

            // Upload to API
            var response = await ApiService.UploadLinesAsync(weekNumber, year, memoryStream, selectedFile.Name);

            if (response?.Success == true)
            {
                successMessage = response.Message;
                selectedFile = null;
            }
            else
            {
                errorMessage = "Failed to upload file. Please check the format and try again.";
            }
        }
        catch (Exception ex)
        {
            errorMessage = $"Error uploading file: {ex.Message}";
        }
        finally
        {
            isUploading = false;
        }
    }

    private void ClearMessages()
    {
        errorMessage = null;
        successMessage = null;
    }

    private void ClearError()
    {
        errorMessage = null;
    }

    private void ClearSuccess()
    {
        successMessage = null;
    }

    // Bowl upload methods
    private void OnBowlFileSelected(InputFileChangeEventArgs e)
    {
        selectedBowlFile = e.File;
        ClearBowlMessages();
    }

    private async Task UploadBowlFile()
    {
        if (selectedBowlFile == null)
            return;

        isBowlUploading = true;
        ClearBowlMessages();

        try
        {
            // Read file stream
            using var stream = selectedBowlFile.OpenReadStream(maxAllowedSize: 10 * 1024 * 1024); // 10 MB limit
            using var memoryStream = new MemoryStream();
            await stream.CopyToAsync(memoryStream);
            memoryStream.Position = 0;

            // Upload to API
            var response = await ApiService.UploadBowlLinesAsync(bowlYear, memoryStream, selectedBowlFile.Name);

            if (response?.Success == true)
            {
                bowlSuccessMessage = response.Message;
                selectedBowlFile = null;
            }
            else
            {
                bowlErrorMessage = "Failed to upload bowl file. Please check the format and try again.";
            }
        }
        catch (Exception ex)
        {
            bowlErrorMessage = $"Error uploading bowl file: {ex.Message}";
        }
        finally
        {
            isBowlUploading = false;
        }
    }

    private void ClearBowlMessages()
    {
        bowlErrorMessage = null;
        bowlSuccessMessage = null;
    }

    private void ClearBowlError()
    {
        bowlErrorMessage = null;
    }

    private void ClearBowlSuccess()
    {
        bowlSuccessMessage = null;
    }

    // Helper classes for SWA auth
    private class AuthMeResponse
    {
        public ClientPrincipal? ClientPrincipal { get; set; }
    }

    private class ClientPrincipal
    {
        public string? IdentityProvider { get; set; }
        public string? UserId { get; set; }
        public string? UserDetails { get; set; }
        public List<string>? UserRoles { get; set; }
    }

    // Results entry methods
    private async Task LoadGamesForResults()
    {
        isLoadingGames = true;
        ClearResultsMessages();

        try
        {
            var response = await ApiService.GetResultsAsync(resultsWeek, resultsYear);
            if (response != null)
            {
                gamesForResults = response.Games;

                // Initialize score entries for games that already have results
                gameScores.Clear();
                foreach (var game in response.Games)
                {
                    gameScores[game.Id] = new ScoreEntry
                    {
                        FavoriteScore = game.FavoriteScore,
                        UnderdogScore = game.UnderdogScore
                    };
                }
            }
            else
            {
                gamesForResults = new List<ApiService.GameResultDto>();
            }
        }
        catch (Exception ex)
        {
            resultsErrorMessage = $"Error loading games: {ex.Message}";
        }
        finally
        {
            isLoadingGames = false;
        }
    }

    private void UpdateFavoriteScore(int gameId, string? value)
    {
        if (!gameScores.ContainsKey(gameId))
        {
            gameScores[gameId] = new ScoreEntry();
        }

        if (int.TryParse(value, out int score))
        {
            gameScores[gameId].FavoriteScore = score;
        }
        else
        {
            gameScores[gameId].FavoriteScore = null;
        }
    }

    private void UpdateUnderdogScore(int gameId, string? value)
    {
        if (!gameScores.ContainsKey(gameId))
        {
            gameScores[gameId] = new ScoreEntry();
        }

        if (int.TryParse(value, out int score))
        {
            gameScores[gameId].UnderdogScore = score;
        }
        else
        {
            gameScores[gameId].UnderdogScore = null;
        }
    }

    private async Task SubmitResults()
    {
        isSubmittingResults = true;
        ClearResultsMessages();

        try
        {
            // Only submit games that have both scores entered
            var resultsToSubmit = gameScores
                .Where(kvp => kvp.Value.FavoriteScore.HasValue && kvp.Value.UnderdogScore.HasValue)
                .Select(kvp => new ApiService.ResultInput
                {
                    GameId = kvp.Key,
                    FavoriteScore = kvp.Value.FavoriteScore!.Value,
                    UnderdogScore = kvp.Value.UnderdogScore!.Value
                })
                .ToList();

            if (!resultsToSubmit.Any())
            {
                resultsErrorMessage = "No complete scores to submit. Enter both scores for at least one game.";
                return;
            }

            var response = await ApiService.SubmitResultsAsync(resultsWeek, resultsYear, resultsToSubmit);

            if (response?.Success == true)
            {
                resultsSuccessMessage = $"Successfully saved {response.ResultsEntered} result(s)!";
                // Reload games to show updated results
                await LoadGamesForResults();
            }
            else
            {
                resultsErrorMessage = response?.Message ?? "Failed to submit results. Please try again.";
            }
        }
        catch (Exception ex)
        {
            resultsErrorMessage = $"Error submitting results: {ex.Message}";
        }
        finally
        {
            isSubmittingResults = false;
        }
    }

    private void ClearResultsMessages()
    {
        resultsErrorMessage = null;
        resultsSuccessMessage = null;
    }

    private void ClearResultsError()
    {
        resultsErrorMessage = null;
    }

    private void ClearResultsSuccess()
    {
        resultsSuccessMessage = null;
    }

    // Helper class for score entry
    private class ScoreEntry
    {
        public int? FavoriteScore { get; set; }
        public int? UnderdogScore { get; set; }
    }

    // CFBD Sync methods
    private async Task CheckSyncStatus()
    {
        isCheckingStatus = true;
        ClearSyncMessages();

        try
        {
            syncStatus = await ApiService.GetSyncStatusAsync();
            if (syncStatus == null)
            {
                syncErrorMessage = "Failed to check API status. Please try again.";
            }
        }
        catch (Exception ex)
        {
            syncErrorMessage = $"Error checking status: {ex.Message}";
        }
        finally
        {
            isCheckingStatus = false;
        }
    }

    private async Task SyncWeeklyGames()
    {
        isSyncingWeekly = true;
        ClearSyncMessages();

        try
        {
            var response = await ApiService.SyncWeeklyGamesAsync(syncWeek, syncYear);

            if (response?.Success == true)
            {
                syncSuccessMessage = $"Synced {response.GamesSynced} games for Week {syncWeek}, {syncYear} from {response.Provider}";
            }
            else
            {
                syncErrorMessage = response?.Message ?? "Failed to sync weekly games. Please try again.";
            }
        }
        catch (Exception ex)
        {
            syncErrorMessage = $"Error syncing weekly games: {ex.Message}";
        }
        finally
        {
            isSyncingWeekly = false;
        }
    }

    private async Task SyncBowlGames()
    {
        isSyncingBowl = true;
        ClearSyncMessages();

        try
        {
            var response = await ApiService.SyncBowlGamesAsync(syncBowlYear);

            if (response?.Success == true)
            {
                syncSuccessMessage = $"Synced {response.GamesSynced} bowl games for {syncBowlYear} from {response.Provider}";
            }
            else
            {
                syncErrorMessage = response?.Message ?? "Failed to sync bowl games. Please try again.";
            }
        }
        catch (Exception ex)
        {
            syncErrorMessage = $"Error syncing bowl games: {ex.Message}";
        }
        finally
        {
            isSyncingBowl = false;
        }
    }

    private async Task SyncWeeklyResults()
    {
        isSyncingResults = true;
        ClearSyncMessages();

        try
        {
            var response = await ApiService.SyncWeeklyResultsAsync(syncResultsWeek, syncResultsYear);

            if (response?.Success == true)
            {
                var message = $"Synced {response.ResultsSynced} results for Week {syncResultsWeek}, {syncResultsYear}";
                if (response.GamesSkipped > 0)
                {
                    message += $" ({response.GamesSkipped} games already had results)";
                }
                if (response.GamesNotFound > 0)
                {
                    message += $" ({response.GamesNotFound} games not found in database)";
                }
                syncSuccessMessage = message;

                // Reload the games if we're viewing the same week
                if (syncResultsWeek == resultsWeek && syncResultsYear == resultsYear && gamesForResults != null)
                {
                    await LoadGamesForResults();
                }
            }
            else
            {
                syncErrorMessage = response?.Message ?? "Failed to sync results. Please try again.";
            }
        }
        catch (Exception ex)
        {
            syncErrorMessage = $"Error syncing results: {ex.Message}";
        }
        finally
        {
            isSyncingResults = false;
        }
    }

    private void ClearSyncMessages()
    {
        syncErrorMessage = null;
        syncSuccessMessage = null;
    }

    private void ClearSyncError()
    {
        syncErrorMessage = null;
    }

    private void ClearSyncSuccess()
    {
        syncSuccessMessage = null;
    }
}
