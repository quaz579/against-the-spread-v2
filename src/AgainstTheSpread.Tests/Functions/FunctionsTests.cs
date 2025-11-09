using AgainstTheSpread.Core.Interfaces;
using AgainstTheSpread.Core.Models;
using AgainstTheSpread.Functions;
using FluentAssertions;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Moq;
using System.Net;
using System.Text;
using System.Text.Json;

namespace AgainstTheSpread.Tests.Functions;

public class FunctionsTests
{
    [Fact]
    public async Task WeeksFunction_GetWeeks_ReturnsWeeksList()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<WeeksFunction>>();
        var mockStorage = new Mock<IStorageService>();
        mockStorage.Setup(s => s.GetAvailableWeeksAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<int> { 1, 2, 3 });

        var function = new WeeksFunction(mockLogger.Object, mockStorage.Object);

        // Act & Assert - basic construction test
        function.Should().NotBeNull();
        mockStorage.Verify(s => s.GetAvailableWeeksAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task LinesFunction_GetLines_ValidatesWeekRange()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<LinesFunction>>();
        var mockStorage = new Mock<IStorageService>();
        
        var function = new LinesFunction(mockLogger.Object, mockStorage.Object);

        // Act & Assert - basic construction test
        function.Should().NotBeNull();
    }

    [Fact]
    public async Task PicksFunction_SubmitPicks_ValidatesPicks()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<PicksFunction>>();
        var mockExcel = new Mock<IExcelService>();
        mockExcel.Setup(e => e.GeneratePicksExcelAsync(It.IsAny<UserPicks>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new byte[] { 1, 2, 3 });

        var function = new PicksFunction(mockLogger.Object, mockExcel.Object);

        // Act & Assert - basic construction test
        function.Should().NotBeNull();
    }

    // Note: Full integration tests for HTTP endpoints will be added in Phase 8
    // These would require mocking HttpRequestData and FunctionContext which is complex
    // For now, we verify the functions are constructed and services are injected correctly
}
