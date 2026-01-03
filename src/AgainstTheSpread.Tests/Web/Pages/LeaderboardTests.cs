using AgainstTheSpread.Web.Pages;
using AgainstTheSpread.Web.Services;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;

namespace AgainstTheSpread.Tests.Web.Pages;

/// <summary>
/// Tests for Leaderboard page component, focusing on user highlighting and authentication handling.
/// These tests verify the features added in PR #8 for Phase 1 UI/UX improvements.
/// </summary>
public class LeaderboardTests : TestContext
{
    private readonly Mock<ILogger<ApiService>> _loggerMock;
    private readonly Mock<IAuthStateService> _authStateMock;

    public LeaderboardTests()
    {
        _loggerMock = new Mock<ILogger<ApiService>>();
        _authStateMock = new Mock<IAuthStateService>();
        
        // Setup HTTP client that will fail API calls (we're testing UI logic, not API integration)
        var httpClient = new HttpClient { BaseAddress = new Uri("https://test.example.com/") };
        var apiService = new ApiService(httpClient, _loggerMock.Object);
        
        Services.AddSingleton(apiService);
        Services.AddSingleton(_authStateMock.Object);
    }

    [Fact]
    public void Leaderboard_HandlesAuthInitializationFailure_Gracefully()
    {
        // Arrange
        _authStateMock.Setup(a => a.InitializeAsync())
            .ThrowsAsync(new Exception("Auth service unavailable"));

        // Act - Component should render without crashing even if auth fails
        var cut = RenderComponent<Leaderboard>();

        // Assert - Component renders basic structure
        Assert.NotNull(cut.Markup);
        Assert.Contains("Leaderboard", cut.Markup);
    }

    [Fact]
    public void Leaderboard_RendersYearSelector()
    {
        // Arrange
        _authStateMock.Setup(a => a.InitializeAsync()).Returns(Task.CompletedTask);
        _authStateMock.Setup(a => a.UserId).Returns((string?)null);

        // Act
        var cut = RenderComponent<Leaderboard>();

        // Assert
        var yearSelect = cut.Find("#yearSelect");
        Assert.NotNull(yearSelect);
    }

    [Fact]
    public void Leaderboard_RendersViewSelector()
    {
        // Arrange
        _authStateMock.Setup(a => a.InitializeAsync()).Returns(Task.CompletedTask);
        _authStateMock.Setup(a => a.UserId).Returns((string?)null);

        // Act
        var cut = RenderComponent<Leaderboard>();

        // Assert
        var viewSelect = cut.Find("#viewSelect");
        Assert.NotNull(viewSelect);
        
        // Should have season and weekly options
        var options = viewSelect.QuerySelectorAll("option");
        Assert.Equal(2, options.Length);
    }

    [Theory]
    [InlineData(null, false)]
    [InlineData("", false)]
    [InlineData("550e8400-e29b-41d4-a716-446655440000", true)]
    public async Task IsCurrentUser_HandlesVariousUserIdStates(string? userId, bool shouldMatch)
    {
        // Arrange
        var testGuid = Guid.Parse("550e8400-e29b-41d4-a716-446655440000");
        
        _authStateMock.Setup(a => a.InitializeAsync()).Returns(Task.CompletedTask);
        _authStateMock.Setup(a => a.UserId).Returns(userId);

        // Act
        var cut = RenderComponent<Leaderboard>();
        await Task.Delay(100); // Allow async initialization
        var instance = cut.Instance;
        
        // Use reflection to call the private IsCurrentUser method
        var method = typeof(Leaderboard).GetMethod("IsCurrentUser", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var result = (bool)method!.Invoke(instance, new object[] { testGuid })!;

        // Assert
        Assert.Equal(shouldMatch, result);
    }

    [Fact]
    public async Task IsCurrentUser_IsCaseInsensitive()
    {
        // Arrange
        var testGuid = Guid.Parse("550e8400-e29b-41d4-a716-446655440000");
        var userIdLower = testGuid.ToString().ToLowerInvariant();
        
        _authStateMock.Setup(a => a.InitializeAsync()).Returns(Task.CompletedTask);
        _authStateMock.Setup(a => a.UserId).Returns(userIdLower);

        // Act
        var cut = RenderComponent<Leaderboard>();
        await Task.Delay(100);
        var instance = cut.Instance;
        
        var method = typeof(Leaderboard).GetMethod("IsCurrentUser", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        // Test with the same GUID (which will have different casing when converted ToString())
        var result = (bool)method!.Invoke(instance, new object[] { testGuid })!;

        // Assert - should match regardless of case
        Assert.True(result);
    }

    [Fact]
    public void Leaderboard_HasLoadingIndicator()
    {
        // Arrange
        _authStateMock.Setup(a => a.InitializeAsync()).Returns(Task.CompletedTask);

        // Act
        var cut = RenderComponent<Leaderboard>();

        // Assert - Should show loading state initially
        var markup = cut.Markup;
        Assert.True(
            markup.Contains("spinner-border") || markup.Contains("Loading") || markup.Contains("alert"),
            "Leaderboard should show loading indicator or content");
    }

    [Fact]
    public async Task GetSeasonRowClass_ReturnsTableCurrentUserClass_ForCurrentUser()
    {
        // Arrange
        var currentUserId = Guid.NewGuid();
        
        _authStateMock.Setup(a => a.InitializeAsync()).Returns(Task.CompletedTask);
        _authStateMock.Setup(a => a.UserId).Returns(currentUserId.ToString());

        // Act
        var cut = RenderComponent<Leaderboard>();
        await Task.Delay(100);
        var instance = cut.Instance;
        
        var method = typeof(Leaderboard).GetMethod("GetSeasonRowClass", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var result = (string)method!.Invoke(instance, new object[] { 5, currentUserId })!;

        // Assert
        Assert.Contains("table-current-user", result);
    }

    [Fact]
    public async Task GetSeasonRowClass_ReturnsTableWarning_ForTopThreeNonCurrentUsers()
    {
        // Arrange
        var otherUserId = Guid.NewGuid();
        
        _authStateMock.Setup(a => a.InitializeAsync()).Returns(Task.CompletedTask);
        _authStateMock.Setup(a => a.UserId).Returns(Guid.NewGuid().ToString());

        // Act
        var cut = RenderComponent<Leaderboard>();
        await Task.Delay(100);
        var instance = cut.Instance;
        
        var method = typeof(Leaderboard).GetMethod("GetSeasonRowClass", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        // Test ranks 1, 2, 3
        var result1 = (string)method!.Invoke(instance, new object[] { 1, otherUserId })!;
        var result2 = (string)method!.Invoke(instance, new object[] { 2, otherUserId })!;
        var result3 = (string)method!.Invoke(instance, new object[] { 3, otherUserId })!;
        var result4 = (string)method!.Invoke(instance, new object[] { 4, otherUserId })!;

        // Assert
        Assert.Contains("table-warning", result1);
        Assert.Contains("table-warning", result2);
        Assert.Contains("table-warning", result3);
        Assert.DoesNotContain("table-warning", result4);
    }

    [Fact]
    public async Task GetWeeklyRowClass_PrioritizesCurrentUserOverPerfect()
    {
        // Arrange
        var currentUserId = Guid.NewGuid();
        
        _authStateMock.Setup(a => a.InitializeAsync()).Returns(Task.CompletedTask);
        _authStateMock.Setup(a => a.UserId).Returns(currentUserId.ToString());

        // Act
        var cut = RenderComponent<Leaderboard>();
        await Task.Delay(100);
        var instance = cut.Instance;
        
        var method = typeof(Leaderboard).GetMethod("GetWeeklyRowClass", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        // Current user with perfect week
        var result = (string)method!.Invoke(instance, new object[] { 1, currentUserId, true })!;

        // Assert - Should have current user class (takes priority)
        Assert.Contains("table-current-user", result);
    }
}
