using AgainstTheSpread.Core.Models;
using AgainstTheSpread.Web.Helpers;
using AgainstTheSpread.Web.Models;
using AgainstTheSpread.Web.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace AgainstTheSpread.Web.Pages;

public partial class Picks : ComponentBase
{
    [Inject]
    private ApiService ApiService { get; set; } = default!;

    [Inject]
    private ITeamLogoService TeamLogoService { get; set; } = default!;

    [Inject]
    private ITeamColorService TeamColorService { get; set; } = default!;

    [Inject]
    private IAuthStateService AuthStateService { get; set; } = default!;

    [Inject]
    private NavigationManager Navigation { get; set; } = default!;

    [Inject]
    private HttpClient HttpClient { get; set; } = default!;

    [Inject]
    private IJSRuntime JSRuntime { get; set; } = default!;

    [Inject]
    private ILogger<Picks> Logger { get; set; } = default!;

    private string userName = "";
    private int selectedYear = DateTime.Now.Year;
    private int selectedWeek = 0;
    private List<int> availableWeeks = new();
    private ApiService.GamesResponse? gamesFromDb = null;
    private List<string> selectedPicks = new();
    private Dictionary<string, int> teamToGameId = new(); // Maps team name to GameId for database submission
    private bool isLoading = true;
    private bool isDownloading = false;
    private bool isPrinting = false;
    private bool isSubmitting = false;
    private string? errorMessage = null;
    private string? successMessage = null;

    protected override async Task OnInitializedAsync()
    {
        // Initialize auth state
        await AuthStateService.InitializeAsync();

        // Initialize the team logo service and team color service
        await TeamLogoService.InitializeAsync(HttpClient);
        await TeamColorService.InitializeAsync(HttpClient);

        await LoadAvailableWeeks();
    }

    private void Login()
    {
        AuthStateService.Login(Navigation.Uri);
    }

    private void Logout()
    {
        AuthStateService.Logout("/picks");
    }

    private async Task LoadAvailableWeeks()
    {
        isLoading = true;
        errorMessage = null;

        try
        {
            availableWeeks = await ApiService.GetAvailableWeeksAsync(selectedYear);

            if (availableWeeks.Count == 0)
            {
                errorMessage = $"No weeks available for {selectedYear}. Please check back later or contact your administrator.";
            }
        }
        catch (Exception ex)
        {
            errorMessage = $"Failed to load available weeks: {ex.Message}";
        }
        finally
        {
            isLoading = false;
        }
    }

    private async Task LoadGames()
    {
        if ((!AuthStateService.IsAuthenticated && string.IsNullOrWhiteSpace(userName)) || selectedWeek == 0)
            return;

        isLoading = true;
        errorMessage = null;

        try
        {
            // Load games from database (used for all users)
            gamesFromDb = await ApiService.GetGamesAsync(selectedWeek, selectedYear);

            if (gamesFromDb != null && gamesFromDb.Games.Any())
            {
                // Build mapping of team names to game IDs
                teamToGameId.Clear();
                foreach (var game in gamesFromDb.Games)
                {
                    teamToGameId[game.Favorite] = game.Id;
                    teamToGameId[game.Underdog] = game.Id;
                }

                // For authenticated users, load existing picks
                if (AuthStateService.IsAuthenticated)
                {
                    await LoadExistingPicks();
                    selectedPicks.Clear();
                    foreach (var pick in existingPicks)
                    {
                        selectedPicks.Add(pick.SelectedTeam);
                    }
                }
                else
                {
                    selectedPicks.Clear();
                }
            }
            else
            {
                errorMessage = $"No games available for Week {selectedWeek} of {selectedYear}. Please check back later or contact your administrator.";
            }
        }
        catch (Exception ex)
        {
            errorMessage = $"Failed to load games: {ex.Message}";
            Logger.LogError(ex, "Error loading games");
        }
        finally
        {
            isLoading = false;
        }
    }

    private List<ApiService.UserPickDto> existingPicks = new();

    private async Task LoadExistingPicks()
    {
        try
        {
            var picks = await ApiService.GetUserPicksAsync(selectedYear, selectedWeek);
            existingPicks = picks ?? new List<ApiService.UserPickDto>();
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Failed to load existing picks");
            existingPicks = new List<ApiService.UserPickDto>();
        }
    }

    private void GoBack()
    {
        gamesFromDb = null;
        selectedPicks.Clear();
        teamToGameId.Clear();
        existingPicks.Clear();
        successMessage = null;
    }

    private void TogglePick(GameDisplayModel game, string team)
    {
        // Don't allow changes to locked games
        if (game.IsLocked)
            return;

        // Find the opposing team in this game
        var opposingTeam = team == game.Favorite ? game.Underdog : game.Favorite;

        if (selectedPicks.Contains(team))
        {
            // Deselect this team
            selectedPicks.Remove(team);
        }
        else
        {
            // Check if the opposing team is selected, if so, remove it first
            if (selectedPicks.Contains(opposingTeam))
            {
                selectedPicks.Remove(opposingTeam);
            }

            // Only add if we have room
            if (selectedPicks.Count < 6)
            {
                selectedPicks.Add(team);
            }
        }
    }

    private List<GameDisplayModel> GetGamesForDisplay()
    {
        if (gamesFromDb != null)
        {
            return gamesFromDb.Games.Select(g => new GameDisplayModel
            {
                Id = g.Id,
                Favorite = g.Favorite,
                Underdog = g.Underdog,
                Line = g.Line,
                GameDate = g.GameDate,
                VsAt = "vs",
                IsLocked = g.IsLocked
            }).ToList();
        }

        return new List<GameDisplayModel>();
    }

    private async Task SubmitPicksToDatabase()
    {
        if (selectedPicks.Count != 6 || !AuthStateService.IsAuthenticated)
            return;

        isSubmitting = true;
        errorMessage = null;
        successMessage = null;

        try
        {
            var picks = selectedPicks
                .Where(team => teamToGameId.ContainsKey(team))
                .Select(team => new ApiService.PickSubmission
                {
                    GameId = teamToGameId[team],
                    SelectedTeam = team
                })
                .ToList();

            if (picks.Count < 6)
            {
                errorMessage = "Could not map all picks to games. Please try again.";
                return;
            }

            var result = await ApiService.SubmitUserPicksAsync(selectedYear, selectedWeek, picks);

            if (result?.Success == true)
            {
                successMessage = $"Your picks have been saved! {result.PicksSubmitted} picks submitted.";
                if (result.LockedGames?.Any() == true)
                {
                    successMessage += $" Note: {result.LockedGames.Count} picks were rejected because the games are locked.";
                }
            }
            else
            {
                errorMessage = result?.Message ?? "Failed to save picks. Please try again.";
            }
        }
        catch (Exception ex)
        {
            errorMessage = $"Error saving picks: {ex.Message}";
            Logger.LogError(ex, "Error submitting picks to database");
        }
        finally
        {
            isSubmitting = false;
        }
    }

    private async Task GeneratePicks()
    {
        if (selectedPicks.Count != 6 || gamesFromDb == null)
            return;

        isDownloading = true;

        try
        {
            var userPicks = new UserPicks
            {
                Name = userName,
                Week = selectedWeek,
                Year = selectedYear,
                Picks = selectedPicks,
                SubmittedAt = DateTime.UtcNow
            };

            var excelBytes = await ApiService.SubmitPicksAsync(userPicks);

            if (excelBytes != null)
            {
                // Download file using JavaScript interop
                var fileName = $"{SanitizeFileName(userName)}_Week_{selectedWeek}_Picks.xlsx";
                await JSRuntime.InvokeVoidAsync("downloadFile", fileName, Convert.ToBase64String(excelBytes));

                // Show success message and reset
                await JSRuntime.InvokeVoidAsync("alert", "Your picks have been downloaded successfully!");
                GoBack();
            }
            else
            {
                errorMessage = "Failed to generate picks file. Please try again.";
            }
        }
        catch (Exception ex)
        {
            errorMessage = $"Error downloading picks: {ex.Message}";
        }
        finally
        {
            isDownloading = false;
        }
    }

    private string SanitizeFileName(string input)
    {
        var invalidChars = Path.GetInvalidFileNameChars();
        // Replace invalid characters with underscore, but keep spaces
        var sanitized = string.Concat(input.Select(c => invalidChars.Contains(c) ? '_' : c));
        return sanitized.TrimEnd();
    }

    private string GetButtonClass(bool isSelected, TeamColors? teamColors)
    {
        // If selected, use bright green with custom styling
        if (isSelected)
        {
            return "btn-selected";
        }

        // If no team colors, use default Bootstrap styles
        if (teamColors == null)
        {
            return "btn-outline-primary";
        }

        // Use team-colored class for unselected buttons with team colors
        return "team-color-btn";
    }

    private string GetButtonStyle(bool isSelected, TeamColors? teamColors)
    {
        // If selected, use bright green with dark border
        if (isSelected)
        {
            return "background-color: #00ff00; border: 3px solid #000000; color: #000000; font-weight: bold;";
        }

        // If no colors, don't add custom styles
        if (teamColors == null)
        {
            return string.Empty;
        }

        // Validate that colors are not empty
        if (string.IsNullOrWhiteSpace(teamColors.Primary) || string.IsNullOrWhiteSpace(teamColors.Secondary))
        {
            return string.Empty;
        }

        // Apply team colors with good contrast for text
        var brightness = GetBrightness(teamColors.Primary);
        var textColor = brightness > 128 ? "#000000" : "#FFFFFF";

        return $"background-color: {teamColors.Primary}; border-color: {teamColors.Secondary}; color: {textColor};";
    }

    private int GetBrightness(string hexColor)
    {
        try
        {
            // Remove # if present
            hexColor = hexColor.TrimStart('#');

            // Validate length
            if (hexColor.Length != 6)
            {
                return 128; // Return middle brightness as default
            }

            // Parse RGB values
            var r = Convert.ToInt32(hexColor.Substring(0, 2), 16);
            var g = Convert.ToInt32(hexColor.Substring(2, 2), 16);
            var b = Convert.ToInt32(hexColor.Substring(4, 2), 16);

            // Calculate perceived brightness using standard formula
            return (int)((r * 299 + g * 587 + b * 114) / 1000);
        }
        catch
        {
            // Return middle brightness as fallback
            return 128;
        }
    }

    private async Task PrintPicksSheet()
    {
        if (selectedPicks.Count != 6 || gamesFromDb == null || isPrinting)
            return;

        isPrinting = true;
        try
        {
            var picksHtml = GenerateLargeTextPicksHtml();
            await JSRuntime.InvokeVoidAsync("openPrintableWindow", picksHtml);
        }
        catch (Exception ex)
        {
            errorMessage = $"Failed to open print window: {ex.Message}. Please check if popups are blocked.";
        }
        finally
        {
            isPrinting = false;
        }
    }

    private string GenerateLargeTextPicksHtml()
    {
        var picksLines = new List<string>();

        foreach (var teamName in selectedPicks)
        {
            // Find the game where this team is either favorite or underdog
            var game = gamesFromDb!.Games.FirstOrDefault(g =>
                g.Favorite.Equals(teamName, StringComparison.OrdinalIgnoreCase) ||
                g.Underdog.Equals(teamName, StringComparison.OrdinalIgnoreCase));

            if (game != null)
            {
                // Format spread: favorite gets negative line, underdog gets positive
                var isFavorite = game.Favorite.Equals(teamName, StringComparison.OrdinalIgnoreCase);
                var spreadDisplay = isFavorite ? game.Line.ToString("0.#") : $"+{(-game.Line):0.#}";
                picksLines.Add($"{spreadDisplay} {teamName}");
            }
            else
            {
                picksLines.Add(teamName);
            }
        }

        return PrintPageGenerator.GeneratePicksHtml(picksLines, selectedWeek);
    }

    /// <summary>
    /// Display model for games loaded from the database.
    /// </summary>
    private class GameDisplayModel
    {
        public int Id { get; set; }
        public string Favorite { get; set; } = string.Empty;
        public string Underdog { get; set; } = string.Empty;
        public decimal Line { get; set; }
        public DateTime GameDate { get; set; }
        public string VsAt { get; set; } = "vs";
        public bool IsLocked { get; set; }
    }
}
