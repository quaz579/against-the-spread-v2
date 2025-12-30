using AgainstTheSpread.Core.Interfaces;
using AgainstTheSpread.Core.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace AgainstTheSpread.Tests.Services;

/// <summary>
/// Tests for CollegeFootballDataProvider implementation.
/// Uses mocked HttpClient to simulate API responses.
/// </summary>
public class CollegeFootballDataProviderTests
{
    private readonly Mock<ILogger<CollegeFootballDataProvider>> _mockLogger;

    public CollegeFootballDataProviderTests()
    {
        _mockLogger = new Mock<ILogger<CollegeFootballDataProvider>>();
    }

    #region Helper Methods

    private CollegeFootballDataProvider CreateProvider(HttpClient httpClient)
    {
        return new CollegeFootballDataProvider(httpClient, _mockLogger.Object);
    }

    private HttpClient CreateMockHttpClient(HttpStatusCode statusCode, string jsonContent)
    {
        var mockHandler = new Mock<HttpMessageHandler>();
        mockHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = statusCode,
                Content = new StringContent(jsonContent, System.Text.Encoding.UTF8, "application/json")
            });

        return new HttpClient(mockHandler.Object);
    }

    #endregion

    #region GetWeeklyGamesAsync Tests

    [Fact]
    public async Task GetWeeklyGamesAsync_WithValidResponse_ReturnsGames()
    {
        // Arrange
        var linesJson = @"[
            {
                ""id"": 123,
                ""homeTeam"": ""Alabama"",
                ""awayTeam"": ""Auburn"",
                ""startDate"": ""2024-11-30T19:00:00Z"",
                ""lines"": [
                    { ""provider"": ""consensus"", ""spread"": -14.5, ""overUnder"": 55.5 }
                ]
            },
            {
                ""id"": 456,
                ""homeTeam"": ""Georgia"",
                ""awayTeam"": ""Florida"",
                ""startDate"": ""2024-11-30T15:30:00Z"",
                ""lines"": [
                    { ""provider"": ""consensus"", ""spread"": 7.0, ""overUnder"": 48.0 }
                ]
            }
        ]";

        var httpClient = CreateMockHttpClient(HttpStatusCode.OK, linesJson);
        var provider = CreateProvider(httpClient);

        // Act
        var result = await provider.GetWeeklyGamesAsync(2024, 14);

        // Assert
        result.Should().HaveCount(2);

        // Game 1: Alabama favored at home
        var game1 = result.FirstOrDefault(g => g.HomeTeam == "Alabama");
        game1.Should().NotBeNull();
        game1!.Favorite.Should().Be("Alabama");
        game1.Underdog.Should().Be("Auburn");
        game1.Line.Should().Be(-14.5m);

        // Game 2: Florida favored away (spread is positive = away favored)
        var game2 = result.FirstOrDefault(g => g.HomeTeam == "Georgia");
        game2.Should().NotBeNull();
        game2!.Favorite.Should().Be("Florida");
        game2.Underdog.Should().Be("Georgia");
        game2.Line.Should().Be(-7m);
    }

    [Fact]
    public async Task GetWeeklyGamesAsync_WithEmptyResponse_ReturnsEmptyList()
    {
        // Arrange
        var httpClient = CreateMockHttpClient(HttpStatusCode.OK, "[]");
        var provider = CreateProvider(httpClient);

        // Act
        var result = await provider.GetWeeklyGamesAsync(2024, 1);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetWeeklyGamesAsync_WithGamesWithoutLines_ExcludesThem()
    {
        // Arrange
        var linesJson = @"[
            {
                ""id"": 123,
                ""homeTeam"": ""Alabama"",
                ""awayTeam"": ""Auburn"",
                ""startDate"": ""2024-11-30T19:00:00Z"",
                ""lines"": []
            },
            {
                ""id"": 456,
                ""homeTeam"": ""Georgia"",
                ""awayTeam"": ""Florida"",
                ""startDate"": ""2024-11-30T15:30:00Z"",
                ""lines"": [
                    { ""provider"": ""consensus"", ""spread"": -7.0 }
                ]
            }
        ]";

        var httpClient = CreateMockHttpClient(HttpStatusCode.OK, linesJson);
        var provider = CreateProvider(httpClient);

        // Act
        var result = await provider.GetWeeklyGamesAsync(2024, 14);

        // Assert
        result.Should().HaveCount(1);
        result[0].HomeTeam.Should().Be("Georgia");
    }

    [Fact]
    public async Task GetWeeklyGamesAsync_WithNullSpread_ExcludesGame()
    {
        // Arrange
        var linesJson = @"[
            {
                ""id"": 123,
                ""homeTeam"": ""Alabama"",
                ""awayTeam"": ""Auburn"",
                ""startDate"": ""2024-11-30T19:00:00Z"",
                ""lines"": [
                    { ""provider"": ""consensus"", ""spread"": null }
                ]
            }
        ]";

        var httpClient = CreateMockHttpClient(HttpStatusCode.OK, linesJson);
        var provider = CreateProvider(httpClient);

        // Act
        var result = await provider.GetWeeklyGamesAsync(2024, 14);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetWeeklyGamesAsync_WithPickemGame_HandlesZeroSpread()
    {
        // Arrange
        var linesJson = @"[
            {
                ""id"": 123,
                ""homeTeam"": ""Alabama"",
                ""awayTeam"": ""Auburn"",
                ""startDate"": ""2024-11-30T19:00:00Z"",
                ""lines"": [
                    { ""provider"": ""consensus"", ""spread"": 0 }
                ]
            }
        ]";

        var httpClient = CreateMockHttpClient(HttpStatusCode.OK, linesJson);
        var provider = CreateProvider(httpClient);

        // Act
        var result = await provider.GetWeeklyGamesAsync(2024, 14);

        // Assert
        result.Should().HaveCount(1);
        result[0].Line.Should().Be(0);
        result[0].Favorite.Should().Be("Alabama"); // Home team by convention for pick'ems
        result[0].Underdog.Should().Be("Auburn");
    }

    [Fact]
    public async Task GetWeeklyGamesAsync_PrefersConsensusProvider()
    {
        // Arrange
        var linesJson = @"[
            {
                ""id"": 123,
                ""homeTeam"": ""Alabama"",
                ""awayTeam"": ""Auburn"",
                ""startDate"": ""2024-11-30T19:00:00Z"",
                ""lines"": [
                    { ""provider"": ""DraftKings"", ""spread"": -10.0 },
                    { ""provider"": ""consensus"", ""spread"": -14.5 },
                    { ""provider"": ""Bovada"", ""spread"": -15.0 }
                ]
            }
        ]";

        var httpClient = CreateMockHttpClient(HttpStatusCode.OK, linesJson);
        var provider = CreateProvider(httpClient);

        // Act
        var result = await provider.GetWeeklyGamesAsync(2024, 14);

        // Assert
        result.Should().HaveCount(1);
        result[0].Line.Should().Be(-14.5m); // Should use consensus
        result[0].LineProvider.Should().Be("consensus");
    }

    [Fact]
    public async Task GetWeeklyGamesAsync_WithHttpError_ThrowsException()
    {
        // Arrange
        var httpClient = CreateMockHttpClient(HttpStatusCode.InternalServerError, "{}");
        var provider = CreateProvider(httpClient);

        // Act & Assert
        await Assert.ThrowsAsync<HttpRequestException>(
            () => provider.GetWeeklyGamesAsync(2024, 14));
    }

    #endregion

    #region GetBowlGamesAsync Tests

    [Fact]
    public async Task GetBowlGamesAsync_WithValidResponse_ReturnsBowlGames()
    {
        // Arrange
        var bowlsJson = @"[
            {
                ""id"": 789,
                ""homeTeam"": ""USC"",
                ""awayTeam"": ""Penn State"",
                ""startDate"": ""2025-01-01T17:00:00Z"",
                ""notes"": ""Rose Bowl"",
                ""lines"": [
                    { ""provider"": ""consensus"", ""spread"": -3.5 }
                ]
            }
        ]";

        var httpClient = CreateMockHttpClient(HttpStatusCode.OK, bowlsJson);
        var provider = CreateProvider(httpClient);

        // Act
        var result = await provider.GetBowlGamesAsync(2024);

        // Assert
        result.Should().HaveCount(1);
        result[0].BowlName.Should().Be("Rose Bowl");
        result[0].Favorite.Should().Be("USC");
        result[0].Underdog.Should().Be("Penn State");
        result[0].Line.Should().Be(-3.5m);
    }

    [Fact]
    public async Task GetBowlGamesAsync_WithNoBowlName_GeneratesName()
    {
        // Arrange
        var bowlsJson = @"[
            {
                ""id"": 789,
                ""homeTeam"": ""USC"",
                ""awayTeam"": ""Penn State"",
                ""startDate"": ""2025-01-01T17:00:00Z"",
                ""notes"": null,
                ""lines"": [
                    { ""provider"": ""consensus"", ""spread"": -3.5 }
                ]
            }
        ]";

        var httpClient = CreateMockHttpClient(HttpStatusCode.OK, bowlsJson);
        var provider = CreateProvider(httpClient);

        // Act
        var result = await provider.GetBowlGamesAsync(2024);

        // Assert
        result.Should().HaveCount(1);
        result[0].BowlName.Should().Be("Penn State vs USC");
    }

    #endregion

    #region GetWeeklyResultsAsync Tests

    [Fact]
    public async Task GetWeeklyResultsAsync_WithCompletedGames_ReturnsResults()
    {
        // Arrange
        var gamesJson = @"[
            {
                ""id"": 123,
                ""season"": 2024,
                ""week"": 14,
                ""homeTeam"": ""Alabama"",
                ""awayTeam"": ""Auburn"",
                ""homePoints"": 28,
                ""awayPoints"": 14,
                ""completed"": true
            },
            {
                ""id"": 456,
                ""season"": 2024,
                ""week"": 14,
                ""homeTeam"": ""Georgia"",
                ""awayTeam"": ""Florida"",
                ""homePoints"": null,
                ""awayPoints"": null,
                ""completed"": false
            }
        ]";

        var httpClient = CreateMockHttpClient(HttpStatusCode.OK, gamesJson);
        var provider = CreateProvider(httpClient);

        // Act
        var result = await provider.GetWeeklyResultsAsync(2024, 14);

        // Assert
        result.Should().HaveCount(1); // Only completed games
        result[0].HomeTeam.Should().Be("Alabama");
        result[0].HomeScore.Should().Be(28);
        result[0].AwayScore.Should().Be(14);
        result[0].IsCompleted.Should().BeTrue();
    }

    #endregion

    #region GetGameResultAsync Tests

    [Fact]
    public async Task GetGameResultAsync_WithExistingGame_ReturnsResult()
    {
        // Arrange
        var gamesJson = @"[
            {
                ""id"": 123,
                ""season"": 2024,
                ""week"": 14,
                ""homeTeam"": ""Alabama"",
                ""awayTeam"": ""Auburn"",
                ""homePoints"": 28,
                ""awayPoints"": 14,
                ""completed"": true
            }
        ]";

        var httpClient = CreateMockHttpClient(HttpStatusCode.OK, gamesJson);
        var provider = CreateProvider(httpClient);

        // Act
        var result = await provider.GetGameResultAsync("123");

        // Assert
        result.Should().NotBeNull();
        result!.GameId.Should().Be("123");
        result.HomeScore.Should().Be(28);
        result.AwayScore.Should().Be(14);
    }

    [Fact]
    public async Task GetGameResultAsync_WithNonExistentGame_ReturnsNull()
    {
        // Arrange
        var httpClient = CreateMockHttpClient(HttpStatusCode.OK, "[]");
        var provider = CreateProvider(httpClient);

        // Act
        var result = await provider.GetGameResultAsync("99999");

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region ProviderName Test

    [Fact]
    public void ProviderName_ReturnsCorrectName()
    {
        // Arrange
        var httpClient = new HttpClient();
        var provider = CreateProvider(httpClient);

        // Act
        var name = provider.ProviderName;

        // Assert
        name.Should().Be("CollegeFootballData");
    }

    #endregion
}
