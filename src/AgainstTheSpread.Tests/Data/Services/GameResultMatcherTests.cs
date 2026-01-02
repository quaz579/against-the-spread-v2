using AgainstTheSpread.Core.Interfaces;
using AgainstTheSpread.Data.Entities;
using AgainstTheSpread.Data.Interfaces;
using AgainstTheSpread.Data.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace AgainstTheSpread.Tests.Data.Services;

public class GameResultMatcherTests
{
    private readonly Mock<IGameService> _mockGameService;
    private readonly Mock<ITeamNameNormalizer> _mockNormalizer;
    private readonly Mock<ILogger<GameResultMatcher>> _mockLogger;
    private readonly GameResultMatcher _matcher;

    public GameResultMatcherTests()
    {
        _mockGameService = new Mock<IGameService>();
        _mockNormalizer = new Mock<ITeamNameNormalizer>();
        _mockLogger = new Mock<ILogger<GameResultMatcher>>();

        _matcher = new GameResultMatcher(
            _mockGameService.Object,
            _mockNormalizer.Object,
            _mockLogger.Object);

        // Default normalizer behavior: pass through unchanged, case-insensitive
        _mockNormalizer
            .Setup(n => n.NormalizeBatchAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IEnumerable<string> names, CancellationToken _) =>
            {
                var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                foreach (var name in names)
                {
                    if (!result.ContainsKey(name))
                    {
                        result[name] = name;
                    }
                }
                return result;
            });
    }

    #region Happy Path Tests

    [Fact]
    public async Task MatchResultsToGamesAsync_WithMatchingGames_ReturnsMatchedResults()
    {
        // Arrange
        var games = new List<GameEntity>
        {
            new() { Id = 1, Year = 2025, Week = 1, Favorite = "Alabama", Underdog = "Tennessee", Line = -7m, GameDate = DateTime.UtcNow.AddDays(-1) }
        };
        _mockGameService.Setup(s => s.GetWeekGamesAsync(2025, 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(games);

        var externalResults = new List<ExternalGameResult>
        {
            new() { HomeTeam = "Alabama", AwayTeam = "Tennessee", HomeScore = 28, AwayScore = 14, IsCompleted = true }
        };

        // Act
        var result = await _matcher.MatchResultsToGamesAsync(2025, 1, externalResults);

        // Assert
        result.Matched.Should().HaveCount(1);
        result.Matched[0].GameId.Should().Be(1);
        result.Matched[0].FavoriteScore.Should().Be(28);
        result.Matched[0].UnderdogScore.Should().Be(14);
        result.Unmatched.Should().BeEmpty();
    }

    [Fact]
    public async Task MatchResultsToGamesAsync_MapsScoresCorrectly_WhenFavoriteIsHomeTeam()
    {
        // Arrange
        var games = new List<GameEntity>
        {
            new() { Id = 1, Year = 2025, Week = 1, Favorite = "Alabama", Underdog = "Tennessee", Line = -7m, GameDate = DateTime.UtcNow.AddDays(-1) }
        };
        _mockGameService.Setup(s => s.GetWeekGamesAsync(2025, 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(games);

        // Home team (Alabama) is the favorite
        var externalResults = new List<ExternalGameResult>
        {
            new() { HomeTeam = "Alabama", AwayTeam = "Tennessee", HomeScore = 35, AwayScore = 21, IsCompleted = true }
        };

        // Act
        var result = await _matcher.MatchResultsToGamesAsync(2025, 1, externalResults);

        // Assert
        result.Matched.Should().HaveCount(1);
        result.Matched[0].FavoriteScore.Should().Be(35); // Home score
        result.Matched[0].UnderdogScore.Should().Be(21); // Away score
    }

    [Fact]
    public async Task MatchResultsToGamesAsync_MapsScoresCorrectly_WhenFavoriteIsAwayTeam()
    {
        // Arrange
        var games = new List<GameEntity>
        {
            new() { Id = 1, Year = 2025, Week = 1, Favorite = "Alabama", Underdog = "Tennessee", Line = -7m, GameDate = DateTime.UtcNow.AddDays(-1) }
        };
        _mockGameService.Setup(s => s.GetWeekGamesAsync(2025, 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(games);

        // Away team (Alabama) is the favorite
        var externalResults = new List<ExternalGameResult>
        {
            new() { HomeTeam = "Tennessee", AwayTeam = "Alabama", HomeScore = 21, AwayScore = 35, IsCompleted = true }
        };

        // Act
        var result = await _matcher.MatchResultsToGamesAsync(2025, 1, externalResults);

        // Assert
        result.Matched.Should().HaveCount(1);
        result.Matched[0].FavoriteScore.Should().Be(35); // Away score (Alabama is favorite)
        result.Matched[0].UnderdogScore.Should().Be(21); // Home score (Tennessee is underdog)
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task MatchResultsToGamesAsync_SkipsGamesWithExistingResults()
    {
        // Arrange
        var games = new List<GameEntity>
        {
            new()
            {
                Id = 1, Year = 2025, Week = 1, Favorite = "Alabama", Underdog = "Tennessee",
                Line = -7m, GameDate = DateTime.UtcNow.AddDays(-1),
                FavoriteScore = 28, UnderdogScore = 14, SpreadWinner = "Alabama", IsPush = false // Already has result
            }
        };
        _mockGameService.Setup(s => s.GetWeekGamesAsync(2025, 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(games);

        var externalResults = new List<ExternalGameResult>
        {
            new() { HomeTeam = "Alabama", AwayTeam = "Tennessee", HomeScore = 35, AwayScore = 21, IsCompleted = true }
        };

        // Act
        var result = await _matcher.MatchResultsToGamesAsync(2025, 1, externalResults);

        // Assert
        result.Matched.Should().BeEmpty();
        result.GamesWithExistingResults.Should().Be(1);
    }

    [Fact]
    public async Task MatchResultsToGamesAsync_ReturnsUnmatched_WhenGameNotFound()
    {
        // Arrange
        var games = new List<GameEntity>
        {
            new() { Id = 1, Year = 2025, Week = 1, Favorite = "Alabama", Underdog = "Tennessee", Line = -7m, GameDate = DateTime.UtcNow.AddDays(-1) }
        };
        _mockGameService.Setup(s => s.GetWeekGamesAsync(2025, 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(games);

        // External result for a game not in the database
        var externalResults = new List<ExternalGameResult>
        {
            new() { HomeTeam = "Georgia", AwayTeam = "Florida", HomeScore = 30, AwayScore = 20, IsCompleted = true }
        };

        // Act
        var result = await _matcher.MatchResultsToGamesAsync(2025, 1, externalResults);

        // Assert
        result.Matched.Should().BeEmpty();
        result.Unmatched.Should().HaveCount(1);
        result.Unmatched[0].HomeTeam.Should().Be("Georgia");
        result.Unmatched[0].AwayTeam.Should().Be("Florida");
        result.Unmatched[0].Reason.Should().Contain("not found");
    }

    [Fact]
    public async Task MatchResultsToGamesAsync_NormalizesTeamNames_BeforeMatching()
    {
        // Arrange
        var games = new List<GameEntity>
        {
            new() { Id = 1, Year = 2025, Week = 1, Favorite = "South Florida", Underdog = "Florida State", Line = -3m, GameDate = DateTime.UtcNow.AddDays(-1) }
        };
        _mockGameService.Setup(s => s.GetWeekGamesAsync(2025, 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(games);

        // Setup normalizer to convert aliases
        _mockNormalizer
            .Setup(n => n.NormalizeBatchAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "USF", "South Florida" },
                { "FSU", "Florida State" },
                { "South Florida", "South Florida" },
                { "Florida State", "Florida State" }
            });

        // External result uses aliases
        var externalResults = new List<ExternalGameResult>
        {
            new() { HomeTeam = "USF", AwayTeam = "FSU", HomeScore = 24, AwayScore = 17, IsCompleted = true }
        };

        // Act
        var result = await _matcher.MatchResultsToGamesAsync(2025, 1, externalResults);

        // Assert
        result.Matched.Should().HaveCount(1);
        result.Matched[0].GameId.Should().Be(1);
        result.Matched[0].FavoriteScore.Should().Be(24); // USF (South Florida) is favorite and home
        result.Matched[0].UnderdogScore.Should().Be(17);
    }

    [Fact]
    public async Task MatchResultsToGamesAsync_HandlesEmptyResultsList()
    {
        // Arrange
        _mockGameService.Setup(s => s.GetWeekGamesAsync(2025, 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<GameEntity>());

        // Act
        var result = await _matcher.MatchResultsToGamesAsync(2025, 1, new List<ExternalGameResult>());

        // Assert
        result.Matched.Should().BeEmpty();
        result.Unmatched.Should().BeEmpty();
        result.GamesWithExistingResults.Should().Be(0);
    }

    [Fact]
    public async Task MatchResultsToGamesAsync_SkipsIncompleteGames()
    {
        // Arrange
        var games = new List<GameEntity>
        {
            new() { Id = 1, Year = 2025, Week = 1, Favorite = "Alabama", Underdog = "Tennessee", Line = -7m, GameDate = DateTime.UtcNow.AddDays(-1) }
        };
        _mockGameService.Setup(s => s.GetWeekGamesAsync(2025, 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(games);

        // Incomplete game
        var externalResults = new List<ExternalGameResult>
        {
            new() { HomeTeam = "Alabama", AwayTeam = "Tennessee", HomeScore = 14, AwayScore = 7, IsCompleted = false }
        };

        // Act
        var result = await _matcher.MatchResultsToGamesAsync(2025, 1, externalResults);

        // Assert
        result.Matched.Should().BeEmpty();
        result.Unmatched.Should().BeEmpty();
    }

    [Fact]
    public async Task MatchResultsToGamesAsync_ReturnsUnmatchedForAll_WhenNoGamesInDatabase()
    {
        // Arrange
        _mockGameService.Setup(s => s.GetWeekGamesAsync(2025, 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<GameEntity>());

        var externalResults = new List<ExternalGameResult>
        {
            new() { HomeTeam = "Alabama", AwayTeam = "Tennessee", HomeScore = 28, AwayScore = 14, IsCompleted = true },
            new() { HomeTeam = "Georgia", AwayTeam = "Florida", HomeScore = 30, AwayScore = 20, IsCompleted = true }
        };

        // Act
        var result = await _matcher.MatchResultsToGamesAsync(2025, 1, externalResults);

        // Assert
        result.Matched.Should().BeEmpty();
        result.Unmatched.Should().HaveCount(2);
    }

    [Fact]
    public async Task MatchResultsToGamesAsync_MatchesMultipleGames()
    {
        // Arrange
        var games = new List<GameEntity>
        {
            new() { Id = 1, Year = 2025, Week = 1, Favorite = "Alabama", Underdog = "Tennessee", Line = -7m, GameDate = DateTime.UtcNow.AddDays(-1) },
            new() { Id = 2, Year = 2025, Week = 1, Favorite = "Georgia", Underdog = "Florida", Line = -10m, GameDate = DateTime.UtcNow.AddDays(-1) },
            new() { Id = 3, Year = 2025, Week = 1, Favorite = "Ohio State", Underdog = "Michigan", Line = -3m, GameDate = DateTime.UtcNow.AddDays(-1) }
        };
        _mockGameService.Setup(s => s.GetWeekGamesAsync(2025, 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(games);

        var externalResults = new List<ExternalGameResult>
        {
            new() { HomeTeam = "Alabama", AwayTeam = "Tennessee", HomeScore = 28, AwayScore = 14, IsCompleted = true },
            new() { HomeTeam = "Florida", AwayTeam = "Georgia", HomeScore = 17, AwayScore = 31, IsCompleted = true }, // Georgia (away) is favorite
            new() { HomeTeam = "Michigan", AwayTeam = "Ohio State", HomeScore = 20, AwayScore = 24, IsCompleted = true } // OSU (away) is favorite
        };

        // Act
        var result = await _matcher.MatchResultsToGamesAsync(2025, 1, externalResults);

        // Assert
        result.Matched.Should().HaveCount(3);

        var alabamaGame = result.Matched.First(m => m.GameId == 1);
        alabamaGame.FavoriteScore.Should().Be(28);
        alabamaGame.UnderdogScore.Should().Be(14);

        var georgiaGame = result.Matched.First(m => m.GameId == 2);
        georgiaGame.FavoriteScore.Should().Be(31); // Georgia's score (away team)
        georgiaGame.UnderdogScore.Should().Be(17); // Florida's score (home team)

        var osuGame = result.Matched.First(m => m.GameId == 3);
        osuGame.FavoriteScore.Should().Be(24); // OSU's score (away team)
        osuGame.UnderdogScore.Should().Be(20); // Michigan's score (home team)
    }

    #endregion

    #region Case Sensitivity Tests

    [Fact]
    public async Task MatchResultsToGamesAsync_IsCaseInsensitive()
    {
        // Arrange
        var games = new List<GameEntity>
        {
            new() { Id = 1, Year = 2025, Week = 1, Favorite = "ALABAMA", Underdog = "tennessee", Line = -7m, GameDate = DateTime.UtcNow.AddDays(-1) }
        };
        _mockGameService.Setup(s => s.GetWeekGamesAsync(2025, 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(games);

        var externalResults = new List<ExternalGameResult>
        {
            new() { HomeTeam = "alabama", AwayTeam = "TENNESSEE", HomeScore = 28, AwayScore = 14, IsCompleted = true }
        };

        // Act
        var result = await _matcher.MatchResultsToGamesAsync(2025, 1, externalResults);

        // Assert
        result.Matched.Should().HaveCount(1);
    }

    #endregion
}
