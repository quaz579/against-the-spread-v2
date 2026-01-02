using AgainstTheSpread.Data;
using AgainstTheSpread.Data.Entities;
using AgainstTheSpread.Data.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace AgainstTheSpread.Tests.Data.Services;

public class TeamNameNormalizerTests : IDisposable
{
    private readonly AtsDbContext _context;
    private readonly Mock<ILogger<TeamNameNormalizer>> _mockLogger;
    private readonly TeamNameNormalizer _normalizer;

    public TeamNameNormalizerTests()
    {
        var options = new DbContextOptionsBuilder<AtsDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new AtsDbContext(options);
        _mockLogger = new Mock<ILogger<TeamNameNormalizer>>();
        _normalizer = new TeamNameNormalizer(_context, _mockLogger.Object);

        // Seed test data
        SeedTestData();
    }

    private void SeedTestData()
    {
        var now = DateTime.UtcNow;
        _context.TeamAliases.AddRange(
            new TeamAliasEntity { Alias = "South Florida", CanonicalName = "South Florida", CreatedAt = now },
            new TeamAliasEntity { Alias = "USF", CanonicalName = "South Florida", CreatedAt = now },
            new TeamAliasEntity { Alias = "Florida State", CanonicalName = "Florida State", CreatedAt = now },
            new TeamAliasEntity { Alias = "FSU", CanonicalName = "Florida State", CreatedAt = now },
            new TeamAliasEntity { Alias = "Florida St", CanonicalName = "Florida State", CreatedAt = now },
            new TeamAliasEntity { Alias = "Miami", CanonicalName = "Miami", CreatedAt = now },
            new TeamAliasEntity { Alias = "Miami (FL)", CanonicalName = "Miami", CreatedAt = now },
            new TeamAliasEntity { Alias = "The U", CanonicalName = "Miami", CreatedAt = now }
        );
        _context.SaveChanges();
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    [Fact]
    public async Task NormalizeAsync_KnownAlias_ReturnsCanonicalName()
    {
        // Act
        var result = await _normalizer.NormalizeAsync("USF");

        // Assert
        result.Should().Be("South Florida");
    }

    [Fact]
    public async Task NormalizeAsync_CanonicalName_ReturnsSameName()
    {
        // Act
        var result = await _normalizer.NormalizeAsync("South Florida");

        // Assert
        result.Should().Be("South Florida");
    }

    [Fact]
    public async Task NormalizeAsync_CaseInsensitive_ReturnsCanonicalName()
    {
        // Act
        var result1 = await _normalizer.NormalizeAsync("usf");
        var result2 = await _normalizer.NormalizeAsync("USF");
        var result3 = await _normalizer.NormalizeAsync("Usf");

        // Assert
        result1.Should().Be("South Florida");
        result2.Should().Be("South Florida");
        result3.Should().Be("South Florida");
    }

    [Fact]
    public async Task NormalizeAsync_UnknownTeam_ReturnsOriginalName()
    {
        // Act
        var result = await _normalizer.NormalizeAsync("Unknown University");

        // Assert
        result.Should().Be("Unknown University");
    }

    [Fact]
    public async Task NormalizeAsync_UnknownTeam_LogsWarning()
    {
        // Act
        await _normalizer.NormalizeAsync("Unknown University");

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Unknown team")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task NormalizeAsync_WhitespaceTrimmed_ReturnsCanonicalName()
    {
        // Act
        var result = await _normalizer.NormalizeAsync("  USF  ");

        // Assert
        result.Should().Be("South Florida");
    }

    [Fact]
    public async Task NormalizeAsync_EmptyString_ReturnsEmptyString()
    {
        // Act
        var result = await _normalizer.NormalizeAsync("");

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task NormalizeAsync_Null_ReturnsEmptyString()
    {
        // Act
        var result = await _normalizer.NormalizeAsync(null!);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task NormalizeBatchAsync_MultipleTeams_ReturnsAllCanonicalNames()
    {
        // Arrange
        var teams = new[] { "USF", "FSU", "Miami", "Unknown Team" };

        // Act
        var result = await _normalizer.NormalizeBatchAsync(teams);

        // Assert
        result.Should().HaveCount(4);
        result["USF"].Should().Be("South Florida");
        result["FSU"].Should().Be("Florida State");
        result["Miami"].Should().Be("Miami");
        result["Unknown Team"].Should().Be("Unknown Team");
    }

    [Fact]
    public async Task AreTeamsEqualAsync_SameCanonical_ReturnsTrue()
    {
        // Act
        var result = await _normalizer.AreTeamsEqualAsync("USF", "South Florida");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task AreTeamsEqualAsync_DifferentTeams_ReturnsFalse()
    {
        // Act
        var result = await _normalizer.AreTeamsEqualAsync("USF", "FSU");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task AreTeamsEqualAsync_BothAliases_ReturnsTrue()
    {
        // Arrange - both are aliases for Florida State
        // Act
        var result = await _normalizer.AreTeamsEqualAsync("FSU", "Florida St");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task GetAllCanonicalNamesAsync_ReturnsDistinctCanonicals()
    {
        // Act
        var result = await _normalizer.GetAllCanonicalNamesAsync();

        // Assert
        result.Should().HaveCount(3);
        result.Should().Contain("South Florida");
        result.Should().Contain("Florida State");
        result.Should().Contain("Miami");
    }

    [Fact]
    public async Task RefreshCacheAsync_UpdatesCache()
    {
        // Arrange - first load the cache
        await _normalizer.NormalizeAsync("USF");

        // Add a new alias
        _context.TeamAliases.Add(new TeamAliasEntity
        {
            Alias = "Bulls",
            CanonicalName = "South Florida",
            CreatedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        // Before refresh, new alias not in cache (would pass through)
        // After refresh, it should work

        // Act
        await _normalizer.RefreshCacheAsync();
        var result = await _normalizer.NormalizeAsync("Bulls");

        // Assert
        result.Should().Be("South Florida");
    }
}
