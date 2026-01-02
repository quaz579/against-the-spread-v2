using AgainstTheSpread.Web.Services;
using Microsoft.AspNetCore.Components;

namespace AgainstTheSpread.Web.Pages;

public partial class Leaderboard : ComponentBase
{
    [Inject]
    private ApiService ApiService { get; set; } = default!;

    private int selectedYear = DateTime.Now.Year;
    private int selectedWeek = 1;
    private string selectedView = "season";
    private List<int> availableYears = new();
    private List<int> availableWeeks = new();
    private bool isLoading = true;
    private string? errorMessage;

    private ApiService.SeasonLeaderboardResponse? seasonLeaderboard;
    private ApiService.WeeklyLeaderboardResponse? weeklyLeaderboard;

    protected override async Task OnInitializedAsync()
    {
        // Set up available years (current year and previous few)
        var currentYear = DateTime.Now.Year;
        availableYears = Enumerable.Range(currentYear - 2, 3).Reverse().ToList();

        await LoadData();
    }

    private async Task LoadData()
    {
        isLoading = true;
        errorMessage = null;
        StateHasChanged();

        try
        {
            // Load available weeks for this year
            availableWeeks = await ApiService.GetAvailableWeeksAsync(selectedYear);
            if (availableWeeks.Any())
            {
                selectedWeek = availableWeeks.Max();
            }

            if (selectedView == "season")
            {
                await LoadSeasonLeaderboard();
            }
            else
            {
                await LoadWeeklyLeaderboard();
            }
        }
        catch (Exception ex)
        {
            errorMessage = $"Failed to load leaderboard: {ex.Message}";
        }
        finally
        {
            isLoading = false;
            StateHasChanged();
        }
    }

    private async Task OnViewChanged()
    {
        if (selectedView == "season")
        {
            await LoadSeasonLeaderboard();
        }
        else
        {
            await LoadWeeklyLeaderboard();
        }
    }

    private async Task LoadSeasonLeaderboard()
    {
        isLoading = true;
        errorMessage = null;
        StateHasChanged();

        try
        {
            seasonLeaderboard = await ApiService.GetSeasonLeaderboardAsync(selectedYear);
        }
        catch (Exception ex)
        {
            errorMessage = $"Failed to load season leaderboard: {ex.Message}";
        }
        finally
        {
            isLoading = false;
            StateHasChanged();
        }
    }

    private async Task LoadWeeklyLeaderboard()
    {
        isLoading = true;
        errorMessage = null;
        StateHasChanged();

        try
        {
            weeklyLeaderboard = await ApiService.GetWeeklyLeaderboardAsync(selectedWeek, selectedYear);
        }
        catch (Exception ex)
        {
            errorMessage = $"Failed to load weekly leaderboard: {ex.Message}";
        }
        finally
        {
            isLoading = false;
            StateHasChanged();
        }
    }
}
