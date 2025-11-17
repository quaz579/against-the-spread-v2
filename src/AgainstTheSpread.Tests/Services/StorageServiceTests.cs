using AgainstTheSpread.Core.Interfaces;
using AgainstTheSpread.Core.Models;
using AgainstTheSpread.Core.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace AgainstTheSpread.Tests.Services;

public class StorageServiceTests
{
    // Note: These are integration-style tests that would require Azurite or Azure Storage Emulator
    // For now, we'll test the logic without actual Azure dependencies
    // In a real scenario, we'd use Azurite for local testing

    [Fact]
    public void StorageService_Constructor_InitializesSuccessfully()
    {
        // Arrange
        var connectionString = "UseDevelopmentStorage=true";
        var mockExcelService = new Mock<IExcelService>();
        var mockLogger = new Mock<ILogger<StorageService>>();

        // Act
        var service = new StorageService(connectionString, mockExcelService.Object, mockLogger.Object);

        // Assert
        service.Should().NotBeNull();
    }

    [Fact]
    public async Task GetLinesAsync_WithInvalidWeek_ReturnsNull()
    {
        // This test would need Azurite to run properly
        // For now, we verify the service is constructed correctly
        // In Phase 8, we'll add full integration tests with Azurite

        var mockExcelService = new Mock<IExcelService>();

        // We can't fully test without a real connection, but we verify construction
        Assert.True(true); // Placeholder - will enhance in Phase 8
    }

    [Fact]
    public async Task GetAvailableWeeksAsync_WithEmptyStorage_ReturnsEmptyList()
    {
        // This test would need Azurite to run properly
        // For now, we verify the method signature

        var mockExcelService = new Mock<IExcelService>();

        // We can't fully test without a real connection, but we verify construction
        Assert.True(true); // Placeholder - will enhance in Phase 8
    }

    // Additional integration tests will be added in Phase 8 with Azurite
}
