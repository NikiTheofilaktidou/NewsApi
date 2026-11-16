using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NewsApi.Application.Configuration;
using NewsApi.Application.Interfaces;

namespace NewsApi.Application.Services;

public class ProviderAnomalyMonitor : BackgroundService
{
    private readonly IProviderStatisticsService _statistics;
    private readonly FeedOptions _options;
    private readonly ILogger<ProviderAnomalyMonitor> _logger;

    public ProviderAnomalyMonitor(IProviderStatisticsService statistics, IOptions<FeedOptions> options, ILogger<ProviderAnomalyMonitor> logger)
    {
        _statistics = statistics;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var recentAverages = _statistics.GetRecentAverageResponseTimes(TimeSpan.FromMinutes(_options.AnomalyDetectionWindow));

            var allStatistics = _statistics.GetAll();

            foreach (var provider in recentAverages)
            {
                if (!allStatistics.TryGetValue(provider.Key, out var stats))
                    continue;

                if (stats.AverageResponseTimeMs <= 0)
                    continue;

                if (provider.Value > stats.AverageResponseTimeMs * _options.AnomalyThresholdMultiplier)
                {
                    _logger.LogWarning("Performance anomaly detected for {Provider}: " + "Recent average {Recent} ms, overall average {Overall} ms.", provider.Key,provider.Value, stats.AverageResponseTimeMs);
                }
            }

            await Task.Delay( TimeSpan.FromMinutes(_options.AnomalyCheckInterval),stoppingToken);
        }
    }
}