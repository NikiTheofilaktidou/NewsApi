using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NewsApi.Application.Interfaces;

namespace NewsApi.Application.Services;

public class ProviderAnomalyMonitor : BackgroundService
{
    private readonly IProviderStatisticsService _statistics;
    private readonly ILogger<ProviderAnomalyMonitor> _logger;

    public ProviderAnomalyMonitor(IProviderStatisticsService statistics, ILogger<ProviderAnomalyMonitor> logger)
    {
        _statistics = statistics;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var recentAverages = _statistics.GetRecentAverageResponseTimes(TimeSpan.FromMinutes(5));

            var allStatistics = _statistics.GetAll();

            foreach (var provider in recentAverages)
            {
                if (!allStatistics.TryGetValue(provider.Key, out var stats))
                    continue;

                if (stats.AverageResponseTimeMs <= 0)
                    continue;

                if (provider.Value > stats.AverageResponseTimeMs * 1.5)
                {
                    _logger.LogWarning("Performance anomaly detected for {Provider}: " + "Recent average {Recent} ms, overall average {Overall} ms.", provider.Key,provider.Value, stats.AverageResponseTimeMs);
                }
            }

            await Task.Delay( TimeSpan.FromMinutes(1),stoppingToken);
        }
    }
}