using AgainstTheSpread.Data;
using AgainstTheSpread.Data.Entities;
using AgainstTheSpread.Data.Interfaces;
using AgainstTheSpread.Data.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace AgainstTheSpread.Tests.Data.Services;

public class LeaderboardServiceTests
{
    private readonly AtsDbContext _context;
    private readonly LeaderboardService _service;

    public LeaderboardServiceTests()
    {
        var options = new DbContextOptionsBuilder<AtsDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new AtsDbContext(options);
        var logger = Mock.Of<ILogger<LeaderboardService>>();
        _service = new LeaderboardService(_context, logger);
    }

    #region GetWeeklyLeaderboardAsync Tests

    [Fact]
    public async Task GetWeeklyLeaderboardAsync_WithNoPicks_ReturnsEmptyList()
    {
        // Act
        var result = await _service.GetWeeklyLeaderboardAsync(2025, 1);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetWeeklyLeaderboardAsync_CalculatesWinsCorrectly()
    {
        // Arrange
        var user = new User { Id = Guid.NewGuid(), Email = "test@test.com", DisplayName = "Test User" };
        _context.Users.Add(user);

        var game1 = CreateGame(2025, 1, "Alabama", "Tennessee", -7m, "Alabama"); // User wins
        var game2 = CreateGame(2025, 1, "Georgia", "Florida", -10m, "Florida");  // User loses

        _context.Games.AddRange(game1, game2);

        var pick1 = new Pick { UserId = user.Id, GameId = game1.Id, Year = 2025, Week = 1, SelectedTeam = "Alabama", Game = game1, User = user };
        var pick2 = new Pick { UserId = user.Id, GameId = game2.Id, Year = 2025, Week = 1, SelectedTeam = "Georgia", Game = game2, User = user };

        _context.Picks.AddRange(pick1, pick2);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetWeeklyLeaderboardAsync(2025, 1);

        // Assert
        result.Should().HaveCount(1);
        var entry = result.First();
        entry.DisplayName.Should().Be("Test User");
        entry.Wins.Should().Be(1);
        entry.Losses.Should().Be(1);
        entry.WinPercentage.Should().Be(50);
    }

    [Fact]
    public async Task GetWeeklyLeaderboardAsync_HandlesPushesCorrectly()
    {
        // Arrange
        var user = new User { Id = Guid.NewGuid(), Email = "test@test.com", DisplayName = "Test User" };
        _context.Users.Add(user);

        var game = CreateGame(2025, 1, "Alabama", "Tennessee", -7m, null, isPush: true);
        _context.Games.Add(game);

        var pick = new Pick { UserId = user.Id, GameId = game.Id, Year = 2025, Week = 1, SelectedTeam = "Alabama", Game = game, User = user };
        _context.Picks.Add(pick);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetWeeklyLeaderboardAsync(2025, 1);

        // Assert
        result.Should().HaveCount(1);
        var entry = result.First();
        entry.Wins.Should().Be(0.5m);
        entry.Losses.Should().Be(0);
        entry.Pushes.Should().Be(1);
    }

    [Fact]
    public async Task GetWeeklyLeaderboardAsync_OrdersByWinsThenLosses()
    {
        // Arrange
        var user1 = new User { Id = Guid.NewGuid(), Email = "user1@test.com", DisplayName = "User 1" };
        var user2 = new User { Id = Guid.NewGuid(), Email = "user2@test.com", DisplayName = "User 2" };
        var user3 = new User { Id = Guid.NewGuid(), Email = "user3@test.com", DisplayName = "User 3" };
        _context.Users.AddRange(user1, user2, user3);

        var game1 = CreateGame(2025, 1, "Alabama", "Tennessee", -7m, "Alabama");
        var game2 = CreateGame(2025, 1, "Georgia", "Florida", -10m, "Georgia");
        _context.Games.AddRange(game1, game2);

        // User1: 2 wins, 0 losses
        _context.Picks.Add(new Pick { UserId = user1.Id, GameId = game1.Id, Year = 2025, Week = 1, SelectedTeam = "Alabama", Game = game1, User = user1 });
        _context.Picks.Add(new Pick { UserId = user1.Id, GameId = game2.Id, Year = 2025, Week = 1, SelectedTeam = "Georgia", Game = game2, User = user1 });

        // User2: 1 win, 1 loss
        _context.Picks.Add(new Pick { UserId = user2.Id, GameId = game1.Id, Year = 2025, Week = 1, SelectedTeam = "Alabama", Game = game1, User = user2 });
        _context.Picks.Add(new Pick { UserId = user2.Id, GameId = game2.Id, Year = 2025, Week = 1, SelectedTeam = "Florida", Game = game2, User = user2 });

        // User3: 1 win, 1 loss (same as user2)
        _context.Picks.Add(new Pick { UserId = user3.Id, GameId = game1.Id, Year = 2025, Week = 1, SelectedTeam = "Tennessee", Game = game1, User = user3 });
        _context.Picks.Add(new Pick { UserId = user3.Id, GameId = game2.Id, Year = 2025, Week = 1, SelectedTeam = "Georgia", Game = game2, User = user3 });

        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetWeeklyLeaderboardAsync(2025, 1);

        // Assert
        result.Should().HaveCount(3);
        result[0].DisplayName.Should().Be("User 1"); // 2 wins
        result[0].Wins.Should().Be(2);
    }

    [Fact]
    public async Task GetWeeklyLeaderboardAsync_OnlyIncludesGamesWithResults()
    {
        // Arrange
        var user = new User { Id = Guid.NewGuid(), Email = "test@test.com", DisplayName = "Test User" };
        _context.Users.Add(user);

        var gameWithResult = CreateGame(2025, 1, "Alabama", "Tennessee", -7m, "Alabama");
        var gameWithoutResult = new GameEntity
        {
            Year = 2025,
            Week = 1,
            Favorite = "Georgia",
            Underdog = "Florida",
            Line = -10m,
            GameDate = DateTime.UtcNow.AddDays(1)
            // No SpreadWinner/IsPush set, so HasResult will be false
        };
        _context.Games.AddRange(gameWithResult, gameWithoutResult);

        _context.Picks.Add(new Pick { UserId = user.Id, GameId = gameWithResult.Id, Year = 2025, Week = 1, SelectedTeam = "Alabama", Game = gameWithResult, User = user });
        _context.Picks.Add(new Pick { UserId = user.Id, GameId = gameWithoutResult.Id, Year = 2025, Week = 1, SelectedTeam = "Georgia", Game = gameWithoutResult, User = user });
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetWeeklyLeaderboardAsync(2025, 1);

        // Assert
        result.Should().HaveCount(1);
        result.First().Wins.Should().Be(1);
        result.First().Losses.Should().Be(0);
    }

    #endregion

    #region GetSeasonLeaderboardAsync Tests

    [Fact]
    public async Task GetSeasonLeaderboardAsync_AggregatesAcrossWeeks()
    {
        // Arrange
        var user = new User { Id = Guid.NewGuid(), Email = "test@test.com", DisplayName = "Test User" };
        _context.Users.Add(user);

        var week1Game = CreateGame(2025, 1, "Alabama", "Tennessee", -7m, "Alabama");
        var week2Game = CreateGame(2025, 2, "Georgia", "Florida", -10m, "Georgia");
        _context.Games.AddRange(week1Game, week2Game);

        _context.Picks.Add(new Pick { UserId = user.Id, GameId = week1Game.Id, Year = 2025, Week = 1, SelectedTeam = "Alabama", Game = week1Game, User = user });
        _context.Picks.Add(new Pick { UserId = user.Id, GameId = week2Game.Id, Year = 2025, Week = 2, SelectedTeam = "Georgia", Game = week2Game, User = user });
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetSeasonLeaderboardAsync(2025);

        // Assert
        result.Should().HaveCount(1);
        var entry = result.First();
        entry.TotalWins.Should().Be(2);
        entry.TotalLosses.Should().Be(0);
        entry.WeeksPlayed.Should().Be(2);
    }

    [Fact]
    public async Task GetSeasonLeaderboardAsync_CountsPerfectWeeks()
    {
        // Arrange
        var user = new User { Id = Guid.NewGuid(), Email = "test@test.com", DisplayName = "Test User" };
        _context.Users.Add(user);

        // Create 6 games for week 1 (perfect week)
        for (int i = 1; i <= 6; i++)
        {
            var game = CreateGame(2025, 1, $"Team{i}A", $"Team{i}B", -7m, $"Team{i}A");
            _context.Games.Add(game);
            _context.Picks.Add(new Pick { UserId = user.Id, GameId = game.Id, Year = 2025, Week = 1, SelectedTeam = $"Team{i}A", Game = game, User = user });
        }

        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetSeasonLeaderboardAsync(2025);

        // Assert
        result.Should().HaveCount(1);
        result.First().PerfectWeeks.Should().Be(1);
        result.First().TotalWins.Should().Be(6);
    }

    [Fact]
    public async Task GetSeasonLeaderboardAsync_NoPerfectWeekIfNotEnoughWins()
    {
        // Arrange
        var user = new User { Id = Guid.NewGuid(), Email = "test@test.com", DisplayName = "Test User" };
        _context.Users.Add(user);

        // Create 5 wins (not perfect)
        for (int i = 1; i <= 5; i++)
        {
            var game = CreateGame(2025, 1, $"Team{i}A", $"Team{i}B", -7m, $"Team{i}A");
            _context.Games.Add(game);
            _context.Picks.Add(new Pick { UserId = user.Id, GameId = game.Id, Year = 2025, Week = 1, SelectedTeam = $"Team{i}A", Game = game, User = user });
        }

        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetSeasonLeaderboardAsync(2025);

        // Assert
        result.Should().HaveCount(1);
        result.First().PerfectWeeks.Should().Be(0);
    }

    [Fact]
    public async Task GetSeasonLeaderboardAsync_OrdersByTotalWinsThenWinPercentage()
    {
        // Arrange
        var user1 = new User { Id = Guid.NewGuid(), Email = "user1@test.com", DisplayName = "User 1" };
        var user2 = new User { Id = Guid.NewGuid(), Email = "user2@test.com", DisplayName = "User 2" };
        _context.Users.AddRange(user1, user2);

        // User1: 5 wins across 2 weeks
        for (int i = 1; i <= 5; i++)
        {
            var game = CreateGame(2025, 1, $"Team{i}A", $"Team{i}B", -7m, $"Team{i}A");
            _context.Games.Add(game);
            _context.Picks.Add(new Pick { UserId = user1.Id, GameId = game.Id, Year = 2025, Week = 1, SelectedTeam = $"Team{i}A", Game = game, User = user1 });
        }

        // User2: 3 wins
        for (int i = 6; i <= 8; i++)
        {
            var game = CreateGame(2025, 1, $"Team{i}A", $"Team{i}B", -7m, $"Team{i}A");
            _context.Games.Add(game);
            _context.Picks.Add(new Pick { UserId = user2.Id, GameId = game.Id, Year = 2025, Week = 1, SelectedTeam = $"Team{i}A", Game = game, User = user2 });
        }

        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetSeasonLeaderboardAsync(2025);

        // Assert
        result.Should().HaveCount(2);
        result[0].DisplayName.Should().Be("User 1"); // More wins
        result[0].TotalWins.Should().Be(5);
        result[1].DisplayName.Should().Be("User 2");
        result[1].TotalWins.Should().Be(3);
    }

    #endregion

    #region GetUserSeasonHistoryAsync Tests

    [Fact]
    public async Task GetUserSeasonHistoryAsync_ReturnsNullForNonExistentUser()
    {
        // Act
        var result = await _service.GetUserSeasonHistoryAsync(Guid.NewGuid(), 2025);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetUserSeasonHistoryAsync_ReturnsEmptyHistoryForUserWithNoPicks()
    {
        // Arrange
        var user = new User { Id = Guid.NewGuid(), Email = "test@test.com", DisplayName = "Test User" };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetUserSeasonHistoryAsync(user.Id, 2025);

        // Assert
        result.Should().NotBeNull();
        result!.DisplayName.Should().Be("Test User");
        result.Weeks.Should().BeEmpty();
        result.TotalWins.Should().Be(0);
    }

    [Fact]
    public async Task GetUserSeasonHistoryAsync_ReturnsDetailedPickHistory()
    {
        // Arrange
        var user = new User { Id = Guid.NewGuid(), Email = "test@test.com", DisplayName = "Test User" };
        _context.Users.Add(user);

        var game1 = CreateGame(2025, 1, "Alabama", "Tennessee", -7m, "Alabama");
        var game2 = CreateGame(2025, 1, "Georgia", "Florida", -10m, "Florida");
        _context.Games.AddRange(game1, game2);

        _context.Picks.Add(new Pick { UserId = user.Id, GameId = game1.Id, Year = 2025, Week = 1, SelectedTeam = "Alabama", Game = game1, User = user });
        _context.Picks.Add(new Pick { UserId = user.Id, GameId = game2.Id, Year = 2025, Week = 1, SelectedTeam = "Georgia", Game = game2, User = user });
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetUserSeasonHistoryAsync(user.Id, 2025);

        // Assert
        result.Should().NotBeNull();
        result!.TotalWins.Should().Be(1);
        result.TotalLosses.Should().Be(1);
        result.Weeks.Should().HaveCount(1);

        var weekHistory = result.Weeks.First();
        weekHistory.Week.Should().Be(1);
        weekHistory.Picks.Should().HaveCount(2);

        var winningPick = weekHistory.Picks.First(p => p.SelectedTeam == "Alabama");
        winningPick.IsWin.Should().BeTrue();

        var losingPick = weekHistory.Picks.First(p => p.SelectedTeam == "Georgia");
        losingPick.IsWin.Should().BeFalse();
    }

    [Fact]
    public async Task GetUserSeasonHistoryAsync_MarksPerfectWeeks()
    {
        // Arrange
        var user = new User { Id = Guid.NewGuid(), Email = "test@test.com", DisplayName = "Test User" };
        _context.Users.Add(user);

        // Create 6 wins for week 1
        for (int i = 1; i <= 6; i++)
        {
            var game = CreateGame(2025, 1, $"Team{i}A", $"Team{i}B", -7m, $"Team{i}A");
            _context.Games.Add(game);
            _context.Picks.Add(new Pick { UserId = user.Id, GameId = game.Id, Year = 2025, Week = 1, SelectedTeam = $"Team{i}A", Game = game, User = user });
        }

        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetUserSeasonHistoryAsync(user.Id, 2025);

        // Assert
        result.Should().NotBeNull();
        result!.Weeks.First().IsPerfect.Should().BeTrue();
    }

    [Fact]
    public async Task GetUserSeasonHistoryAsync_IncludesGamesWithoutResults()
    {
        // Arrange
        var user = new User { Id = Guid.NewGuid(), Email = "test@test.com", DisplayName = "Test User" };
        _context.Users.Add(user);

        var gameWithResult = CreateGame(2025, 1, "Alabama", "Tennessee", -7m, "Alabama");
        var gamePending = new GameEntity
        {
            Year = 2025,
            Week = 1,
            Favorite = "Georgia",
            Underdog = "Florida",
            Line = -10m,
            GameDate = DateTime.UtcNow.AddDays(1)
            // No SpreadWinner/IsPush set, so HasResult will be false
        };
        _context.Games.AddRange(gameWithResult, gamePending);

        _context.Picks.Add(new Pick { UserId = user.Id, GameId = gameWithResult.Id, Year = 2025, Week = 1, SelectedTeam = "Alabama", Game = gameWithResult, User = user });
        _context.Picks.Add(new Pick { UserId = user.Id, GameId = gamePending.Id, Year = 2025, Week = 1, SelectedTeam = "Georgia", Game = gamePending, User = user });
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetUserSeasonHistoryAsync(user.Id, 2025);

        // Assert
        result.Should().NotBeNull();
        result!.Weeks.First().Picks.Should().HaveCount(2);

        var pendingPick = result.Weeks.First().Picks.First(p => p.SelectedTeam == "Georgia");
        pendingPick.HasResult.Should().BeFalse();
        pendingPick.IsWin.Should().BeNull();
    }

    #endregion

    #region Helper Methods

    private GameEntity CreateGame(int year, int week, string favorite, string underdog, decimal line, string? spreadWinner, bool isPush = false)
    {
        return new GameEntity
        {
            Year = year,
            Week = week,
            Favorite = favorite,
            Underdog = underdog,
            Line = line,
            GameDate = DateTime.UtcNow.AddDays(-1),
            // HasResult is computed from SpreadWinner/IsPush, so setting either makes HasResult true
            SpreadWinner = spreadWinner,
            IsPush = isPush,
            FavoriteScore = 24,
            UnderdogScore = 14
        };
    }

    #endregion
}
