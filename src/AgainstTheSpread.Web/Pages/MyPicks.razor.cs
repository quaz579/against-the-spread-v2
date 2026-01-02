using AgainstTheSpread.Web.Services;
using Microsoft.AspNetCore.Components;

namespace AgainstTheSpread.Web.Pages;

public partial class MyPicks : ComponentBase
{
    [Inject]
    private ApiService ApiService { get; set; } = default!;

    [Inject]
    private IAuthStateService AuthState { get; set; } = default!;

    [Inject]
    private NavigationManager NavigationManager { get; set; } = default!;

    private int selectedYear = DateTime.Now.Year;
    private List<int> availableYears = new();
    private bool isLoading = true;
    private string? errorMessage;

    private ApiService.UserSeasonHistoryDto? myHistory;

    protected override async Task OnInitializedAsync()
    {
        // Set up available years
        var currentYear = DateTime.Now.Year;
        availableYears = Enumerable.Range(currentYear - 2, 3).Reverse().ToList();

        // Wait for auth state to initialize
        await AuthState.InitializeAsync();

        if (AuthState.IsAuthenticated)
        {
            await LoadMyHistory();
        }
        else
        {
            isLoading = false;
        }
    }

    private async Task LoadMyHistory()
    {
        isLoading = true;
        errorMessage = null;
        StateHasChanged();

        try
        {
            myHistory = await ApiService.GetMySeasonHistoryAsync(selectedYear);
        }
        catch (Exception ex)
        {
            errorMessage = $"Failed to load your pick history: {ex.Message}";
        }
        finally
        {
            isLoading = false;
            StateHasChanged();
        }
    }

    private void SignIn()
    {
        AuthState.Login("/my-picks");
    }

    private string GetPickRowClass(ApiService.UserPickHistoryDto pick)
    {
        if (!pick.HasResult) return "";
        if (pick.IsPush == true) return "table-secondary";
        if (pick.IsWin == true) return "table-success";
        return "table-danger";
    }

    private string FormatLine(decimal line)
    {
        if (line == 0) return "PK";
        return line > 0 ? $"+{line}" : line.ToString();
    }
}
