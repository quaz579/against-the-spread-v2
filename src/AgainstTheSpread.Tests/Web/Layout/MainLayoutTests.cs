using AgainstTheSpread.Web.Layout;
using AgainstTheSpread.Web.Services;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;

namespace AgainstTheSpread.Tests.Web.Layout;

public class MainLayoutTests : TestContext
{
    private readonly Mock<ILogger<ApiService>> _loggerMock;
    private readonly ApiService _apiService;

    public MainLayoutTests()
    {
        _loggerMock = new Mock<ILogger<ApiService>>();
        var httpClient = new HttpClient { BaseAddress = new Uri("http://localhost/") };
        _apiService = new ApiService(httpClient, _loggerMock.Object);
        
        Services.AddSingleton(_apiService);
    }

    [Theory]
    [InlineData("http://localhost/", "", true)]
    [InlineData("http://localhost/", "/", true)]
    [InlineData("http://localhost/picks", "picks", true)]
    [InlineData("http://localhost/picks/", "picks", true)]
    [InlineData("http://localhost/leaderboard", "leaderboard", true)]
    [InlineData("http://localhost/my-picks", "my-picks", true)]
    [InlineData("http://localhost/picks", "leaderboard", false)]
    [InlineData("http://localhost/", "picks", false)]
    public void IsCurrentPage_ReturnsCorrectValue_ForBasicPaths(string currentUrl, string relativePath, bool expected)
    {
        // Arrange
        var nav = Services.GetRequiredService<NavigationManager>();
        var baseUri = "http://localhost/";
        
        // Use reflection to set the protected Uri property
        typeof(NavigationManager)
            .GetProperty("Uri")!
            .SetValue(nav, currentUrl);
        typeof(NavigationManager)
            .GetProperty("BaseUri")!
            .SetValue(nav, baseUri);

        // Act
        var cut = RenderComponent<MainLayout>();
        var instance = cut.Instance;
        
        // Use reflection to call the private IsCurrentPage method
        var method = typeof(MainLayout).GetMethod("IsCurrentPage", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var result = (bool)method!.Invoke(instance, new object[] { relativePath })!;

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("http://localhost/picks?week=1", "picks?week=1", true)]
    [InlineData("http://localhost/picks?week=1&year=2024", "picks?week=1&year=2024", true)]
    [InlineData("http://localhost/leaderboard?year=2024", "leaderboard?year=2024", true)]
    [InlineData("http://localhost/?test=value", "?test=value", true)]
    [InlineData("http://localhost/picks?week=1", "leaderboard", false)]
    public void IsCurrentPage_HandlesQueryStrings_AsPartOfPath(string currentUrl, string relativePath, bool expected)
    {
        // NOTE: Current implementation treats query strings as part of the path
        // This test documents existing behavior, not ideal behavior
        
        // Arrange
        var nav = Services.GetRequiredService<NavigationManager>();
        var baseUri = "http://localhost/";
        
        typeof(NavigationManager)
            .GetProperty("Uri")!
            .SetValue(nav, currentUrl);
        typeof(NavigationManager)
            .GetProperty("BaseUri")!
            .SetValue(nav, baseUri);

        // Act
        var cut = RenderComponent<MainLayout>();
        var instance = cut.Instance;
        
        var method = typeof(MainLayout).GetMethod("IsCurrentPage", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var result = (bool)method!.Invoke(instance, new object[] { relativePath })!;

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("http://localhost/picks/", "picks/", true)]
    [InlineData("http://localhost/picks", "picks/", true)]
    [InlineData("http://localhost/picks/", "picks", true)]
    public void IsCurrentPage_HandlesTrailingSlashes_Correctly(string currentUrl, string relativePath, bool expected)
    {
        // Arrange
        var nav = Services.GetRequiredService<NavigationManager>();
        var baseUri = "http://localhost/";
        
        typeof(NavigationManager)
            .GetProperty("Uri")!
            .SetValue(nav, currentUrl);
        typeof(NavigationManager)
            .GetProperty("BaseUri")!
            .SetValue(nav, baseUri);

        // Act
        var cut = RenderComponent<MainLayout>();
        var instance = cut.Instance;
        
        var method = typeof(MainLayout).GetMethod("IsCurrentPage", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var result = (bool)method!.Invoke(instance, new object[] { relativePath })!;

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void MainLayout_RendersMobileBottomNav()
    {
        // Act
        var cut = RenderComponent<MainLayout>();

        // Assert
        var nav = cut.Find(".mobile-bottom-nav");
        Assert.NotNull(nav);
        Assert.Equal("navigation", nav.GetAttribute("role"));
        Assert.Equal("Main mobile navigation", nav.GetAttribute("aria-label"));
    }

    [Fact]
    public void MainLayout_BottomNavLinks_HaveAriaLabels()
    {
        // Act
        var cut = RenderComponent<MainLayout>();

        // Assert
        var links = cut.FindAll(".bottom-nav-item");
        Assert.Equal(4, links.Count);
        
        Assert.Equal("Navigate to Home", links[0].GetAttribute("aria-label"));
        Assert.Equal("Navigate to Picks", links[1].GetAttribute("aria-label"));
        Assert.Equal("Navigate to Leaderboard", links[2].GetAttribute("aria-label"));
        Assert.Equal("Navigate to My Picks", links[3].GetAttribute("aria-label"));
    }

    [Fact]
    public void MainLayout_BottomNavSVGs_HaveAriaHidden()
    {
        // Act
        var cut = RenderComponent<MainLayout>();

        // Assert
        var svgs = cut.FindAll(".mobile-bottom-nav svg");
        Assert.Equal(4, svgs.Count);
        
        foreach (var svg in svgs)
        {
            Assert.Equal("true", svg.GetAttribute("aria-hidden"));
        }
    }

    [Fact]
    public void MainLayout_AppliesActiveClass_ToCurrentPage()
    {
        // Arrange
        var nav = Services.GetRequiredService<NavigationManager>();
        typeof(NavigationManager)
            .GetProperty("Uri")!
            .SetValue(nav, "http://localhost/picks");
        typeof(NavigationManager)
            .GetProperty("BaseUri")!
            .SetValue(nav, "http://localhost/");

        // Act
        var cut = RenderComponent<MainLayout>();

        // Assert
        var links = cut.FindAll(".bottom-nav-item");
        Assert.Contains("active", links[1].GetAttribute("class")); // Picks link should be active
        Assert.DoesNotContain("active", links[0].GetAttribute("class")); // Home should not be active
    }
}
