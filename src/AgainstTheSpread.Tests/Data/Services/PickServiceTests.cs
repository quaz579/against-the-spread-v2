using AgainstTheSpread.Data;
using AgainstTheSpread.Data.Entities;
using AgainstTheSpread.Data.Interfaces;
using AgainstTheSpread.Data.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace AgainstTheSpread.Tests.Data.Services;

/// <summary>
/// Tests for PickService implementation.
/// Uses InMemory database provider for testing.
/// </summary>
public class PickServiceTests : IDisposable
{
    private readonly AtsDbContext _context;
    private readonly IPickService _pickService;
    private readonly Mock<ILogger<PickService>> _mockLogger;
    private readonly Guid _testUserId;

    public PickServiceTests()
    {
        var options = new DbContextOptionsBuilder<AtsDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new AtsDbContext(options);
        _mockLogger = new Mock<ILogger<PickService>>();
        _pickService = new PickService(_context, _mockLogger.Object);
        _testUserId = Guid.NewGuid();

        // Create test user
        _context.Users.Add(new User
        {
            Id = _testUserId,
            GoogleSubjectId = "google-test-pick-user",
            Email = "pickuser@test.com",
            DisplayName = "Pick User",
            CreatedAt = DateTime.UtcNow,
            LastLoginAt = DateTime.UtcNow,
            IsActive = true
        });
        _context.SaveChanges();
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    #region Helper Methods

    private GameEntity CreateGame(int year, int week, string favorite, string underdog, DateTime gameDate)
    {
        var game = new GameEntity
        {
            Year = year,
            Week = week,
            Favorite = favorite,
            Underdog = underdog,
            Line = -7.5m,
            GameDate = gameDate
        };
        _context.Games.Add(game);
        _context.SaveChanges();
        return game;
    }

    #endregion

    #region SubmitPicksAsync Tests

    [Fact]
    public async Task SubmitPicksAsync_WithUnlockedGames_SubmitsPicks()
    {
        // Arrange
        var futureDate = DateTime.UtcNow.AddDays(1);
        var game1 = CreateGame(2024, 1, "Alabama", "Auburn", futureDate);
        var game2 = CreateGame(2024, 1, "Georgia", "Florida", futureDate);

        var picks = new List<PickSubmission>
        {
            new(game1.Id, "Alabama"),
            new(game2.Id, "Florida")
        };

        // Act
        var result = await _pickService.SubmitPicksAsync(_testUserId, 2024, 1, picks);

        // Assert
        result.Success.Should().BeTrue();
        result.PicksSubmitted.Should().Be(2);
        result.PicksRejected.Should().Be(0);

        var savedPicks = await _context.Picks.Where(p => p.UserId == _testUserId).ToListAsync();
        savedPicks.Should().HaveCount(2);
    }

    [Fact]
    public async Task SubmitPicksAsync_WithLockedGame_RejectsLockedPicks()
    {
        // Arrange
        var pastDate = DateTime.UtcNow.AddHours(-1);
        var futureDate = DateTime.UtcNow.AddDays(1);
        var lockedGame = CreateGame(2024, 1, "Past Team", "Other", pastDate);
        var unlockedGame = CreateGame(2024, 1, "Future Team", "Other", futureDate);

        var picks = new List<PickSubmission>
        {
            new(lockedGame.Id, "Past Team"),
            new(unlockedGame.Id, "Future Team")
        };

        // Act
        var result = await _pickService.SubmitPicksAsync(_testUserId, 2024, 1, picks);

        // Assert
        result.Success.Should().BeTrue(); // Partial success
        result.PicksSubmitted.Should().Be(1);
        result.PicksRejected.Should().Be(1);
        result.RejectedPicks.Should().Contain(r => r.GameId == lockedGame.Id);
        result.RejectedPicks.First().Reason.Should().Contain("locked");
    }

    [Fact]
    public async Task SubmitPicksAsync_WithAllLockedGames_RejectsAll()
    {
        // Arrange
        var pastDate = DateTime.UtcNow.AddHours(-1);
        var game1 = CreateGame(2024, 1, "Past Team 1", "Other", pastDate);
        var game2 = CreateGame(2024, 1, "Past Team 2", "Other", pastDate);

        var picks = new List<PickSubmission>
        {
            new(game1.Id, "Past Team 1"),
            new(game2.Id, "Past Team 2")
        };

        // Act
        var result = await _pickService.SubmitPicksAsync(_testUserId, 2024, 1, picks);

        // Assert
        result.Success.Should().BeTrue(); // Still success with rejection info
        result.PicksSubmitted.Should().Be(0);
        result.PicksRejected.Should().Be(2);
    }

    [Fact]
    public async Task SubmitPicksAsync_WithExistingPick_UpdatesPick()
    {
        // Arrange
        var futureDate = DateTime.UtcNow.AddDays(1);
        var game = CreateGame(2024, 1, "Alabama", "Auburn", futureDate);

        // Create initial pick
        var initialPick = new Pick
        {
            UserId = _testUserId,
            GameId = game.Id,
            SelectedTeam = "Alabama",
            SubmittedAt = DateTime.UtcNow.AddHours(-2),
            Year = 2024,
            Week = 1
        };
        _context.Picks.Add(initialPick);
        await _context.SaveChangesAsync();

        // Submit new pick for same game
        var picks = new List<PickSubmission>
        {
            new(game.Id, "Auburn") // Changed pick
        };

        // Act
        var result = await _pickService.SubmitPicksAsync(_testUserId, 2024, 1, picks);

        // Assert
        result.Success.Should().BeTrue();
        result.PicksSubmitted.Should().Be(1);

        var updatedPick = await _context.Picks
            .FirstOrDefaultAsync(p => p.UserId == _testUserId && p.GameId == game.Id);
        updatedPick.Should().NotBeNull();
        updatedPick!.SelectedTeam.Should().Be("Auburn");
        updatedPick.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task SubmitPicksAsync_WithNonExistentGame_RejectsPick()
    {
        // Arrange
        var picks = new List<PickSubmission>
        {
            new(9999, "NonExistent Team")
        };

        // Act
        var result = await _pickService.SubmitPicksAsync(_testUserId, 2024, 1, picks);

        // Assert
        result.PicksRejected.Should().Be(1);
        result.RejectedPicks.First().Reason.Should().Contain("not found");
    }

    [Fact]
    public async Task SubmitPicksAsync_WithInvalidTeamSelection_RejectsPick()
    {
        // Arrange
        var futureDate = DateTime.UtcNow.AddDays(1);
        var game = CreateGame(2024, 1, "Alabama", "Auburn", futureDate);

        var picks = new List<PickSubmission>
        {
            new(game.Id, "NotATeam") // Invalid team
        };

        // Act
        var result = await _pickService.SubmitPicksAsync(_testUserId, 2024, 1, picks);

        // Assert
        result.PicksRejected.Should().Be(1);
        result.RejectedPicks.First().Reason.Should().Contain("Invalid team selection");
    }

    [Fact]
    public async Task SubmitPicksAsync_WithEmptyPicks_ReturnsSuccess()
    {
        // Arrange
        var picks = new List<PickSubmission>();

        // Act
        var result = await _pickService.SubmitPicksAsync(_testUserId, 2024, 1, picks);

        // Assert
        result.Success.Should().BeTrue();
        result.PicksSubmitted.Should().Be(0);
    }

    [Fact]
    public async Task SubmitPicksAsync_SetsYearAndWeekCorrectly()
    {
        // Arrange
        var futureDate = DateTime.UtcNow.AddDays(1);
        var game = CreateGame(2025, 5, "Test", "Other", futureDate);

        var picks = new List<PickSubmission>
        {
            new(game.Id, "Test")
        };

        // Act
        await _pickService.SubmitPicksAsync(_testUserId, 2025, 5, picks);

        // Assert
        var savedPick = await _context.Picks.FirstOrDefaultAsync(p => p.GameId == game.Id);
        savedPick.Should().NotBeNull();
        savedPick!.Year.Should().Be(2025);
        savedPick.Week.Should().Be(5);
    }

    #endregion

    #region GetUserPicksAsync Tests

    [Fact]
    public async Task GetUserPicksAsync_ReturnsPicksForWeek()
    {
        // Arrange
        var game1 = CreateGame(2024, 3, "Team A", "Team B", DateTime.UtcNow);
        var game2 = CreateGame(2024, 3, "Team C", "Team D", DateTime.UtcNow);
        var game3 = CreateGame(2024, 4, "Team E", "Team F", DateTime.UtcNow); // Different week

        _context.Picks.AddRange(
            new Pick { UserId = _testUserId, GameId = game1.Id, SelectedTeam = "Team A", SubmittedAt = DateTime.UtcNow, Year = 2024, Week = 3 },
            new Pick { UserId = _testUserId, GameId = game2.Id, SelectedTeam = "Team D", SubmittedAt = DateTime.UtcNow, Year = 2024, Week = 3 },
            new Pick { UserId = _testUserId, GameId = game3.Id, SelectedTeam = "Team E", SubmittedAt = DateTime.UtcNow, Year = 2024, Week = 4 }
        );
        await _context.SaveChangesAsync();

        // Act
        var result = await _pickService.GetUserPicksAsync(_testUserId, 2024, 3);

        // Assert
        result.Should().HaveCount(2);
        result.Should().OnlyContain(p => p.Week == 3);
    }

    [Fact]
    public async Task GetUserPicksAsync_WithNoPicksForWeek_ReturnsEmptyList()
    {
        // Arrange - no picks

        // Act
        var result = await _pickService.GetUserPicksAsync(_testUserId, 2024, 10);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetUserPicksAsync_OnlyReturnsUsersPicks()
    {
        // Arrange
        var otherUserId = Guid.NewGuid();
        _context.Users.Add(new User
        {
            Id = otherUserId,
            GoogleSubjectId = "other-user",
            Email = "other@test.com",
            DisplayName = "Other",
            CreatedAt = DateTime.UtcNow,
            LastLoginAt = DateTime.UtcNow
        });

        var game = CreateGame(2024, 1, "Team", "Other", DateTime.UtcNow);

        _context.Picks.AddRange(
            new Pick { UserId = _testUserId, GameId = game.Id, SelectedTeam = "Team", SubmittedAt = DateTime.UtcNow, Year = 2024, Week = 1 },
            new Pick { UserId = otherUserId, GameId = game.Id, SelectedTeam = "Other", SubmittedAt = DateTime.UtcNow, Year = 2024, Week = 1 }
        );
        await _context.SaveChangesAsync();

        // Act
        var result = await _pickService.GetUserPicksAsync(_testUserId, 2024, 1);

        // Assert
        result.Should().HaveCount(1);
        result.First().SelectedTeam.Should().Be("Team");
    }

    #endregion

    #region GetUserSeasonPicksAsync Tests

    [Fact]
    public async Task GetUserSeasonPicksAsync_ReturnsAllPicksForYear()
    {
        // Arrange
        var game1 = CreateGame(2024, 1, "W1 Team", "Other", DateTime.UtcNow);
        var game2 = CreateGame(2024, 5, "W5 Team", "Other", DateTime.UtcNow);
        var game3 = CreateGame(2024, 10, "W10 Team", "Other", DateTime.UtcNow);
        var game4 = CreateGame(2023, 1, "Last Year", "Other", DateTime.UtcNow); // Different year

        _context.Picks.AddRange(
            new Pick { UserId = _testUserId, GameId = game1.Id, SelectedTeam = "W1 Team", SubmittedAt = DateTime.UtcNow, Year = 2024, Week = 1 },
            new Pick { UserId = _testUserId, GameId = game2.Id, SelectedTeam = "W5 Team", SubmittedAt = DateTime.UtcNow, Year = 2024, Week = 5 },
            new Pick { UserId = _testUserId, GameId = game3.Id, SelectedTeam = "W10 Team", SubmittedAt = DateTime.UtcNow, Year = 2024, Week = 10 },
            new Pick { UserId = _testUserId, GameId = game4.Id, SelectedTeam = "Last Year", SubmittedAt = DateTime.UtcNow, Year = 2023, Week = 1 }
        );
        await _context.SaveChangesAsync();

        // Act
        var result = await _pickService.GetUserSeasonPicksAsync(_testUserId, 2024);

        // Assert
        result.Should().HaveCount(3);
        result.Should().OnlyContain(p => p.Year == 2024);
    }

    [Fact]
    public async Task GetUserSeasonPicksAsync_WithNoPicksForYear_ReturnsEmptyList()
    {
        // Arrange - no picks

        // Act
        var result = await _pickService.GetUserSeasonPicksAsync(_testUserId, 2024);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetUserSeasonPicksAsync_IncludesGameInformation()
    {
        // Arrange
        var game = CreateGame(2024, 1, "Team A", "Team B", DateTime.UtcNow);

        _context.Picks.Add(new Pick
        {
            UserId = _testUserId,
            GameId = game.Id,
            SelectedTeam = "Team A",
            SubmittedAt = DateTime.UtcNow,
            Year = 2024,
            Week = 1
        });
        await _context.SaveChangesAsync();

        // Act
        var result = await _pickService.GetUserSeasonPicksAsync(_testUserId, 2024);

        // Assert
        result.Should().HaveCount(1);
        result.First().Game.Should().NotBeNull();
        result.First().Game!.Favorite.Should().Be("Team A");
    }

    #endregion
}
