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
/// Tests for BowlPickService implementation.
/// Uses InMemory database provider for testing.
/// </summary>
public class BowlPickServiceTests : IDisposable
{
    private readonly AtsDbContext _context;
    private readonly IBowlPickService _bowlPickService;
    private readonly Mock<ILogger<BowlPickService>> _mockLogger;
    private readonly Guid _testUserId;

    public BowlPickServiceTests()
    {
        var options = new DbContextOptionsBuilder<AtsDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new AtsDbContext(options);
        _mockLogger = new Mock<ILogger<BowlPickService>>();
        _bowlPickService = new BowlPickService(_context, _mockLogger.Object);
        _testUserId = Guid.NewGuid();

        // Create test user
        _context.Users.Add(new User
        {
            Id = _testUserId,
            GoogleSubjectId = "google-test-bowl-user",
            Email = "bowluser@test.com",
            DisplayName = "Bowl User",
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

    private BowlGameEntity CreateBowlGame(int year, int gameNumber, string favorite, string underdog, DateTime gameDate)
    {
        var game = new BowlGameEntity
        {
            Year = year,
            GameNumber = gameNumber,
            BowlName = $"Bowl {gameNumber}",
            Favorite = favorite,
            Underdog = underdog,
            Line = -7m,
            GameDate = gameDate
        };
        _context.BowlGames.Add(game);
        _context.SaveChanges();
        return game;
    }

    #endregion

    #region SubmitBowlPicksAsync Tests

    [Fact]
    public async Task SubmitBowlPicksAsync_WithUnlockedGames_SubmitsPicks()
    {
        // Arrange
        var futureDate = DateTime.UtcNow.AddDays(1);
        var game1 = CreateBowlGame(2024, 1, "USC", "Penn State", futureDate);
        var game2 = CreateBowlGame(2024, 2, "Georgia", "Notre Dame", futureDate);

        var picks = new List<BowlPickSubmission>
        {
            new(game1.Id, "USC", 2, "USC"),
            new(game2.Id, "Notre Dame", 1, "Georgia")
        };

        // Act
        var result = await _bowlPickService.SubmitBowlPicksAsync(_testUserId, 2024, picks);

        // Assert
        result.Success.Should().BeTrue();
        result.PicksSubmitted.Should().Be(2);
        result.PicksRejected.Should().Be(0);

        var savedPicks = await _context.BowlPicks.Where(p => p.UserId == _testUserId).ToListAsync();
        savedPicks.Should().HaveCount(2);
    }

    [Fact]
    public async Task SubmitBowlPicksAsync_WithLockedGame_RejectsLockedPicks()
    {
        // Arrange
        var pastDate = DateTime.UtcNow.AddHours(-1);
        var futureDate = DateTime.UtcNow.AddDays(1);
        var lockedGame = CreateBowlGame(2024, 1, "Past Team", "Other", pastDate);
        var unlockedGame = CreateBowlGame(2024, 2, "Future Team", "Other", futureDate);

        var picks = new List<BowlPickSubmission>
        {
            new(lockedGame.Id, "Past Team", 2, "Past Team"),
            new(unlockedGame.Id, "Future Team", 1, "Future Team")
        };

        // Act
        var result = await _bowlPickService.SubmitBowlPicksAsync(_testUserId, 2024, picks);

        // Assert
        result.Success.Should().BeTrue();
        result.PicksSubmitted.Should().Be(1);
        result.PicksRejected.Should().Be(1);
        result.RejectedPicks.Should().Contain(r => r.BowlGameId == lockedGame.Id);
    }

    [Fact]
    public async Task SubmitBowlPicksAsync_WithDuplicateConfidencePoints_ReturnsFailure()
    {
        // Arrange
        var futureDate = DateTime.UtcNow.AddDays(1);
        var game1 = CreateBowlGame(2024, 1, "USC", "Penn State", futureDate);
        var game2 = CreateBowlGame(2024, 2, "Georgia", "Notre Dame", futureDate);

        var picks = new List<BowlPickSubmission>
        {
            new(game1.Id, "USC", 5, "USC"),
            new(game2.Id, "Georgia", 5, "Georgia") // Same confidence points
        };

        // Act
        var result = await _bowlPickService.SubmitBowlPicksAsync(_testUserId, 2024, picks);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("unique");
    }

    [Fact]
    public async Task SubmitBowlPicksAsync_WithExistingPick_UpdatesPick()
    {
        // Arrange
        var futureDate = DateTime.UtcNow.AddDays(1);
        var game = CreateBowlGame(2024, 1, "USC", "Penn State", futureDate);

        // Create initial pick
        var initialPick = new BowlPickEntity
        {
            UserId = _testUserId,
            BowlGameId = game.Id,
            SpreadPick = "USC",
            ConfidencePoints = 5,
            OutrightWinnerPick = "USC",
            SubmittedAt = DateTime.UtcNow.AddHours(-2),
            Year = 2024
        };
        _context.BowlPicks.Add(initialPick);
        await _context.SaveChangesAsync();

        // Submit updated pick
        var picks = new List<BowlPickSubmission>
        {
            new(game.Id, "Penn State", 10, "Penn State") // Changed pick
        };

        // Act
        var result = await _bowlPickService.SubmitBowlPicksAsync(_testUserId, 2024, picks);

        // Assert
        result.Success.Should().BeTrue();
        result.PicksSubmitted.Should().Be(1);

        var updatedPick = await _context.BowlPicks
            .FirstOrDefaultAsync(p => p.UserId == _testUserId && p.BowlGameId == game.Id);
        updatedPick.Should().NotBeNull();
        updatedPick!.SpreadPick.Should().Be("Penn State");
        updatedPick.ConfidencePoints.Should().Be(10);
        updatedPick.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task SubmitBowlPicksAsync_WithNonExistentGame_RejectsPick()
    {
        // Arrange
        var picks = new List<BowlPickSubmission>
        {
            new(9999, "NonExistent Team", 5, "NonExistent Team")
        };

        // Act
        var result = await _bowlPickService.SubmitBowlPicksAsync(_testUserId, 2024, picks);

        // Assert
        result.PicksRejected.Should().Be(1);
        result.RejectedPicks.First().Reason.Should().Contain("not found");
    }

    [Fact]
    public async Task SubmitBowlPicksAsync_WithInvalidSpreadPick_RejectsPick()
    {
        // Arrange
        var futureDate = DateTime.UtcNow.AddDays(1);
        var game = CreateBowlGame(2024, 1, "USC", "Penn State", futureDate);

        var picks = new List<BowlPickSubmission>
        {
            new(game.Id, "InvalidTeam", 5, "USC")
        };

        // Act
        var result = await _bowlPickService.SubmitBowlPicksAsync(_testUserId, 2024, picks);

        // Assert
        result.PicksRejected.Should().Be(1);
        result.RejectedPicks.First().Reason.Should().Contain("Invalid spread pick");
    }

    [Fact]
    public async Task SubmitBowlPicksAsync_WithInvalidOutrightPick_RejectsPick()
    {
        // Arrange
        var futureDate = DateTime.UtcNow.AddDays(1);
        var game = CreateBowlGame(2024, 1, "USC", "Penn State", futureDate);

        var picks = new List<BowlPickSubmission>
        {
            new(game.Id, "USC", 5, "InvalidTeam")
        };

        // Act
        var result = await _bowlPickService.SubmitBowlPicksAsync(_testUserId, 2024, picks);

        // Assert
        result.PicksRejected.Should().Be(1);
        result.RejectedPicks.First().Reason.Should().Contain("Invalid outright winner pick");
    }

    [Fact]
    public async Task SubmitBowlPicksAsync_WithEmptyPicks_ReturnsSuccess()
    {
        // Arrange
        var picks = new List<BowlPickSubmission>();

        // Act
        var result = await _bowlPickService.SubmitBowlPicksAsync(_testUserId, 2024, picks);

        // Assert
        result.Success.Should().BeTrue();
        result.PicksSubmitted.Should().Be(0);
    }

    #endregion

    #region GetUserBowlPicksAsync Tests

    [Fact]
    public async Task GetUserBowlPicksAsync_ReturnsPicksForYear()
    {
        // Arrange
        var game1 = CreateBowlGame(2024, 1, "Team A", "Team B", DateTime.UtcNow);
        var game2 = CreateBowlGame(2024, 2, "Team C", "Team D", DateTime.UtcNow);
        var game3 = CreateBowlGame(2023, 1, "Team E", "Team F", DateTime.UtcNow); // Different year

        _context.BowlPicks.AddRange(
            new BowlPickEntity { UserId = _testUserId, BowlGameId = game1.Id, SpreadPick = "Team A", ConfidencePoints = 2, OutrightWinnerPick = "Team A", SubmittedAt = DateTime.UtcNow, Year = 2024 },
            new BowlPickEntity { UserId = _testUserId, BowlGameId = game2.Id, SpreadPick = "Team D", ConfidencePoints = 1, OutrightWinnerPick = "Team C", SubmittedAt = DateTime.UtcNow, Year = 2024 },
            new BowlPickEntity { UserId = _testUserId, BowlGameId = game3.Id, SpreadPick = "Team E", ConfidencePoints = 1, OutrightWinnerPick = "Team E", SubmittedAt = DateTime.UtcNow, Year = 2023 }
        );
        await _context.SaveChangesAsync();

        // Act
        var result = await _bowlPickService.GetUserBowlPicksAsync(_testUserId, 2024);

        // Assert
        result.Should().HaveCount(2);
        result.Should().OnlyContain(p => p.Year == 2024);
    }

    [Fact]
    public async Task GetUserBowlPicksAsync_ReturnsOrderedByGameNumber()
    {
        // Arrange
        var game1 = CreateBowlGame(2024, 3, "Team A", "Team B", DateTime.UtcNow);
        var game2 = CreateBowlGame(2024, 1, "Team C", "Team D", DateTime.UtcNow);
        var game3 = CreateBowlGame(2024, 2, "Team E", "Team F", DateTime.UtcNow);

        _context.BowlPicks.AddRange(
            new BowlPickEntity { UserId = _testUserId, BowlGameId = game1.Id, SpreadPick = "Team A", ConfidencePoints = 3, OutrightWinnerPick = "Team A", SubmittedAt = DateTime.UtcNow, Year = 2024 },
            new BowlPickEntity { UserId = _testUserId, BowlGameId = game2.Id, SpreadPick = "Team C", ConfidencePoints = 1, OutrightWinnerPick = "Team C", SubmittedAt = DateTime.UtcNow, Year = 2024 },
            new BowlPickEntity { UserId = _testUserId, BowlGameId = game3.Id, SpreadPick = "Team E", ConfidencePoints = 2, OutrightWinnerPick = "Team E", SubmittedAt = DateTime.UtcNow, Year = 2024 }
        );
        await _context.SaveChangesAsync();

        // Act
        var result = await _bowlPickService.GetUserBowlPicksAsync(_testUserId, 2024);

        // Assert
        result.Should().HaveCount(3);
        result[0].BowlGame!.GameNumber.Should().Be(1);
        result[1].BowlGame!.GameNumber.Should().Be(2);
        result[2].BowlGame!.GameNumber.Should().Be(3);
    }

    [Fact]
    public async Task GetUserBowlPicksAsync_WithNoPicksForYear_ReturnsEmptyList()
    {
        // Arrange - no picks

        // Act
        var result = await _bowlPickService.GetUserBowlPicksAsync(_testUserId, 2024);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetUserBowlPicksAsync_OnlyReturnsUsersPicks()
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

        var game = CreateBowlGame(2024, 1, "Team", "Other", DateTime.UtcNow);

        _context.BowlPicks.AddRange(
            new BowlPickEntity { UserId = _testUserId, BowlGameId = game.Id, SpreadPick = "Team", ConfidencePoints = 5, OutrightWinnerPick = "Team", SubmittedAt = DateTime.UtcNow, Year = 2024 },
            new BowlPickEntity { UserId = otherUserId, BowlGameId = game.Id, SpreadPick = "Other", ConfidencePoints = 5, OutrightWinnerPick = "Other", SubmittedAt = DateTime.UtcNow, Year = 2024 }
        );
        await _context.SaveChangesAsync();

        // Act
        var result = await _bowlPickService.GetUserBowlPicksAsync(_testUserId, 2024);

        // Assert
        result.Should().HaveCount(1);
        result.First().SpreadPick.Should().Be("Team");
    }

    #endregion

    #region GetUsersWithBowlPicksAsync Tests

    [Fact]
    public async Task GetUsersWithBowlPicksAsync_ReturnsDistinctUserIds()
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

        var game1 = CreateBowlGame(2024, 1, "Team A", "Team B", DateTime.UtcNow);
        var game2 = CreateBowlGame(2024, 2, "Team C", "Team D", DateTime.UtcNow);

        _context.BowlPicks.AddRange(
            new BowlPickEntity { UserId = _testUserId, BowlGameId = game1.Id, SpreadPick = "Team A", ConfidencePoints = 1, OutrightWinnerPick = "Team A", SubmittedAt = DateTime.UtcNow, Year = 2024 },
            new BowlPickEntity { UserId = _testUserId, BowlGameId = game2.Id, SpreadPick = "Team C", ConfidencePoints = 2, OutrightWinnerPick = "Team C", SubmittedAt = DateTime.UtcNow, Year = 2024 },
            new BowlPickEntity { UserId = otherUserId, BowlGameId = game1.Id, SpreadPick = "Team B", ConfidencePoints = 1, OutrightWinnerPick = "Team B", SubmittedAt = DateTime.UtcNow, Year = 2024 }
        );
        await _context.SaveChangesAsync();

        // Act
        var result = await _bowlPickService.GetUsersWithBowlPicksAsync(2024);

        // Assert
        result.Should().HaveCount(2);
        result.Should().Contain(_testUserId);
        result.Should().Contain(otherUserId);
    }

    [Fact]
    public async Task GetUsersWithBowlPicksAsync_OnlyReturnsUsersForSpecifiedYear()
    {
        // Arrange
        var game2024 = CreateBowlGame(2024, 1, "Team A", "Team B", DateTime.UtcNow);
        var game2023 = CreateBowlGame(2023, 1, "Team C", "Team D", DateTime.UtcNow);

        _context.BowlPicks.AddRange(
            new BowlPickEntity { UserId = _testUserId, BowlGameId = game2024.Id, SpreadPick = "Team A", ConfidencePoints = 1, OutrightWinnerPick = "Team A", SubmittedAt = DateTime.UtcNow, Year = 2024 },
            new BowlPickEntity { UserId = _testUserId, BowlGameId = game2023.Id, SpreadPick = "Team C", ConfidencePoints = 1, OutrightWinnerPick = "Team C", SubmittedAt = DateTime.UtcNow, Year = 2023 }
        );
        await _context.SaveChangesAsync();

        // Act
        var result = await _bowlPickService.GetUsersWithBowlPicksAsync(2024);

        // Assert
        result.Should().HaveCount(1);
        result.Should().Contain(_testUserId);
    }

    #endregion
}
