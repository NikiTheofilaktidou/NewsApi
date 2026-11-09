using NewsApi.Application.Services;

namespace NewsApi.UnitTests;

public class ProviderStatisticsServiceTests
{
    [Fact]
    public void RecordSuccess_ShouldIncrementSuccessfulRequests()
    {
        // Arrange
        var service = new ProviderStatisticsService();

        // Act
        service.RecordSuccess("GNews", 100);

        // Assert
        var statistics = service.GetAll()["GNews"];

        Assert.Equal(1, statistics.TotalRequests);
        Assert.Equal(1, statistics.SuccessfulRequests);
        Assert.Equal(0, statistics.FailedRequests);
    }

    [Fact]
    public void RecordFailure_ShouldIncrementFailedRequests()
    {
        // Arrange
        var service = new ProviderStatisticsService();

        // Act
        service.RecordFailure("GNews", 200);

        // Assert
        var statistics = service.GetAll()["GNews"];

        Assert.Equal(1, statistics.TotalRequests);
        Assert.Equal(0, statistics.SuccessfulRequests);
        Assert.Equal(1, statistics.FailedRequests);
    }

    [Fact]
    public void RecordRequests_ShouldCalculateAverageResponseTime()
    {
        // Arrange
        var service = new ProviderStatisticsService();

        // Act
        service.RecordSuccess("GNews", 100);
        service.RecordSuccess("GNews", 200);

        // Assert
        var statistics = service.GetAll()["GNews"];

        Assert.Equal(150, statistics.AverageResponseTimeMs);
    }

    [Fact]
    public void RecordRequestsConcurrently_ShouldPreserveAllCounts()
    {
        // Arrange
        var service = new ProviderStatisticsService();

        // Act
        Parallel.For(0, 1000, _ =>
        {
            service.RecordSuccess("GNews", 100);
        });

        // Assert
        var statistics = service.GetAll()["GNews"];

        Assert.Equal(1000, statistics.TotalRequests);
        Assert.Equal(1000, statistics.SuccessfulRequests);
        Assert.Equal(0, statistics.FailedRequests);
    }

    [Fact]
    public void RecordSuccess_ShouldCountFastRequests()
    {
        var service = new ProviderStatisticsService();

        service.RecordSuccess("GNews", 50);

        var stats = service.GetAll()["GNews"];

        Assert.Equal(1, stats.FastRequests);
        Assert.Equal(0, stats.AverageRequests);
        Assert.Equal(0, stats.SlowRequests);
    }

    [Fact]
    public void RecordSuccess_ShouldCountAverageRequests()
    {
        var service = new ProviderStatisticsService();

        service.RecordSuccess("GNews", 150);

        var stats = service.GetAll()["GNews"];

        Assert.Equal(0, stats.FastRequests);
        Assert.Equal(1, stats.AverageRequests);
        Assert.Equal(0, stats.SlowRequests);
    }

    [Fact]
    public void RecordSuccess_ShouldCountSlowRequests()
    {
        var service = new ProviderStatisticsService();

        service.RecordSuccess("GNews", 250);

        var stats = service.GetAll()["GNews"];

        Assert.Equal(0, stats.FastRequests);
        Assert.Equal(0, stats.AverageRequests);
        Assert.Equal(1, stats.SlowRequests);
    }
}