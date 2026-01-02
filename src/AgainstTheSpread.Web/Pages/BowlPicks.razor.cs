using AgainstTheSpread.Core.Models;
using AgainstTheSpread.Web.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace AgainstTheSpread.Web.Pages;

public partial class BowlPicks : ComponentBase
{
    [Inject]
    private ApiService ApiService { get; set; } = default!;

    [Inject]
    private ITeamLogoService TeamLogoService { get; set; } = default!;

    [Inject]
    private ITeamColorService TeamColorService { get; set; } = default!;

    [Inject]
    private HttpClient HttpClient { get; set; } = default!;

    [Inject]
    private IJSRuntime JSRuntime { get; set; } = default!;

    private string userName = "";
    private int selectedYear = DateTime.Now.Year;
    private BowlLines? bowlLines = null;
    private readonly Dictionary<int, BowlPick> picks = new();
    private bool isLoading = false;
    private bool isDownloading = false;
    private string? errorMessage = null;

    protected override async Task OnInitializedAsync()
    {
        // Initialize the team logo service and team color service
        await TeamLogoService.InitializeAsync(HttpClient);
        await TeamColorService.InitializeAsync(HttpClient);
    }

    private async Task LoadBowlLines()
    {
        if (string.IsNullOrWhiteSpace(userName))
            return;

        isLoading = true;
        errorMessage = null;

        try
        {
            bowlLines = await ApiService.GetBowlLinesAsync(selectedYear);

            if (bowlLines == null || bowlLines.Games.Count == 0)
            {
                errorMessage = $"No bowl games available for {selectedYear}. Please check back later or contact your administrator.";
                bowlLines = null;
            }
            else
            {
                // Initialize picks dictionary
                picks.Clear();
                foreach (var game in bowlLines.Games)
                {
                    picks[game.GameNumber] = new BowlPick
                    {
                        GameNumber = game.GameNumber
                    };
                }
            }
        }
        catch (Exception ex)
        {
            errorMessage = $"Failed to load bowl games: {ex.Message}";
            bowlLines = null;
        }
        finally
        {
            isLoading = false;
        }
    }

    private void GoBack()
    {
        bowlLines = null;
        picks.Clear();
    }

    private BowlPick GetOrCreatePick(int gameNumber)
    {
        if (!picks.ContainsKey(gameNumber))
        {
            picks[gameNumber] = new BowlPick { GameNumber = gameNumber };
        }
        return picks[gameNumber];
    }

    private void SetSpreadPick(int gameNumber, string team)
    {
        GetOrCreatePick(gameNumber).SpreadPick = team;
    }

    private void SetConfidence(int gameNumber, int confidence)
    {
        GetOrCreatePick(gameNumber).ConfidencePoints = confidence;
    }

    private void HandleConfidenceChange(int gameNumber, ChangeEventArgs e)
    {
        if (int.TryParse(e.Value?.ToString(), out int confidence))
        {
            SetConfidence(gameNumber, confidence);
        }
    }

    private void SetOutrightWinner(int gameNumber, string team)
    {
        GetOrCreatePick(gameNumber).OutrightWinner = team;
    }

    private bool IsConfidencePointUsed(int confidence, int excludeGameNumber)
    {
        return picks.Any(p => p.Value.ConfidencePoints == confidence && p.Key != excludeGameNumber);
    }

    private bool IsConfidencePointDuplicate(int confidence)
    {
        if (confidence == 0) return false;
        return picks.Values.Count(p => p.ConfidencePoints == confidence) > 1;
    }

    private int CurrentConfidenceSum => picks.Values.Sum(p => p.ConfidencePoints);

    private int ExpectedConfidenceSum => bowlLines != null ? bowlLines.TotalGames * (bowlLines.TotalGames + 1) / 2 : 0;

    private bool IsConfidenceSumValid => CurrentConfidenceSum == ExpectedConfidenceSum;

    private List<int> DuplicateConfidencePoints => picks.Values
        .Where(p => p.ConfidencePoints > 0)
        .GroupBy(p => p.ConfidencePoints)
        .Where(g => g.Count() > 1)
        .Select(g => g.Key)
        .ToList();

    private int CompletedPicksCount => picks.Values.Count(p => 
        !string.IsNullOrEmpty(p.SpreadPick) && 
        p.ConfidencePoints > 0 && 
        !string.IsNullOrEmpty(p.OutrightWinner));

    private bool IsAllPicksValid
    {
        get
        {
            if (bowlLines == null) return false;
            if (picks.Count != bowlLines.TotalGames) return false;
            if (!picks.Values.All(p => p.IsValid(bowlLines.TotalGames))) return false;
            if (!IsConfidenceSumValid) return false;
            if (DuplicateConfidencePoints.Any()) return false;
            return true;
        }
    }

    private async Task GenerateBowlPicks()
    {
        if (!IsAllPicksValid || bowlLines == null)
            return;

        isDownloading = true;

        try
        {
            var userPicks = new BowlUserPicks
            {
                Name = userName,
                Year = selectedYear,
                TotalGames = bowlLines.TotalGames,
                Picks = picks.Values.ToList(),
                SubmittedAt = DateTime.UtcNow
            };

            var excelBytes = await ApiService.SubmitBowlPicksAsync(userPicks);

            if (excelBytes != null)
            {
                // Download file using JavaScript interop
                var fileName = $"{SanitizeFileName(userName)}_Bowl_Picks_{selectedYear}.xlsx";
                await JSRuntime.InvokeVoidAsync("downloadFile", fileName, Convert.ToBase64String(excelBytes));

                // Show success message and reset
                await JSRuntime.InvokeVoidAsync("alert", "✅ Your bowl picks have been downloaded successfully!");
                GoBack();
            }
            else
            {
                errorMessage = "Failed to generate bowl picks file. Please try again.";
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
        var sanitized = string.Concat(input.Select(c => invalidChars.Contains(c) ? '_' : c));
        return sanitized.TrimEnd();
    }
}
