using AgainstTheSpread.Web.Services;
using Microsoft.AspNetCore.Components;

namespace AgainstTheSpread.Web.Pages;

public partial class UserHistory : ComponentBase
{
    [Parameter]
    public string UserId { get; set; } = string.Empty;

    [Inject]
    private ApiService ApiService { get; set; } = default!;

    [Inject]
    private NavigationManager NavigationManager { get; set; } = default!;

    private int selectedYear = DateTime.Now.Year;
    private List<int> availableYears = new();
    private bool isLoading = true;
    private string? errorMessage;

    private ApiService.UserSeasonHistoryDto? userHistory;

    protected override async Task OnInitializedAsync()
    {
        // Parse year from query string if provided
        var uri = NavigationManager.ToAbsoluteUri(NavigationManager.Uri);
        var queryParams = System.Web.HttpUtility.ParseQueryString(uri.Query);
        var yearStr = queryParams["year"];
        if (!string.IsNullOrEmpty(yearStr) && int.TryParse(yearStr, out var year))
        {
            selectedYear = year;
        }

        // Set up available years
        var currentYear = DateTime.Now.Year;
        availableYears = Enumerable.Range(currentYear - 2, 3).Reverse().ToList();

        await LoadUserHistory();
    }

    private async Task LoadUserHistory()
    {
        if (!Guid.TryParse(UserId, out var userGuid))
        {
            errorMessage = "Invalid user ID";
            isLoading = false;
            return;
        }

        isLoading = true;
        errorMessage = null;
        StateHasChanged();

        try
        {
            userHistory = await ApiService.GetUserSeasonHistoryAsync(userGuid, selectedYear);
        }
        catch (Exception ex)
        {
            errorMessage = $"Failed to load player history: {ex.Message}";
        }
        finally
        {
            isLoading = false;
            StateHasChanged();
        }
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
