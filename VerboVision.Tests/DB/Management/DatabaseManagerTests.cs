using Microsoft.Extensions.Logging;
using Moq;
using VerboVision.DataLayer.DB.Context;
using VerboVision.DataLayer.DB.Management;

namespace VerboVision.Tests.DB.Management;

public class DatabaseManagerTests
{
    private readonly Mock<ILogger<ApiAppContext>> _loggerMock = new();

    [Fact]
    public async Task CreateAsync_NullConnectionString_ThrowsArgumentNullException()
    {
        // ARRANGE
        // (null connection string, valid logger)

        // ACT & ASSERT
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            DatabaseManager.CreateAsync(null!, _loggerMock.Object));
    }

    [Fact]
    public async Task CreateAsync_EmptyConnectionString_ThrowsArgumentNullException()
    {
        // ARRANGE
        // (empty connection string, valid logger)

        // ACT & ASSERT
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            DatabaseManager.CreateAsync("", _loggerMock.Object));
    }

    [Fact]
    public async Task DeleteAsync_NullConnectionString_ThrowsArgumentNullException()
    {
        // ARRANGE
        // (null connection string, valid logger)

        // ACT & ASSERT
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            DatabaseManager.DeleteAsync(null!, _loggerMock.Object));
    }

    [Fact]
    public async Task DeleteAsync_EmptyConnectionString_ThrowsArgumentNullException()
    {
        // ARRANGE
        // (empty connection string, valid logger)

        // ACT & ASSERT
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            DatabaseManager.DeleteAsync("", _loggerMock.Object));
    }
}
