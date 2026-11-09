
using System.Collections.Concurrent;
using NewsApi.Application.Interfaces;
using NewsApi.Application.Models;

namespace NewsApi.Application.Services;

public class ProviderStatisticsService : IProviderStatisticsService
{
    private readonly ConcurrentDictionary<string, ProviderStatistics> _statistics = new();
    private readonly ConcurrentDictionary<string, ConcurrentQueue<(DateTime Timestamp, double ElapsedMs)>> _recentSamples = new();
    public void RecordSuccess(string providerName, double elapsedMilliseconds)
    {
        UpdateStatistics(providerName, elapsedMilliseconds, true);
    }

    public void RecordFailure(string providerName, double elapsedMilliseconds)
    {
        UpdateStatistics(providerName, elapsedMilliseconds, false);
    }

    public IReadOnlyDictionary<string, ProviderStatistics> GetAll()
    {
        return new Dictionary<string, ProviderStatistics>(_statistics);
    }

    private void UpdateStatistics(string providerName, double elapsedMilliseconds, bool succeeded)
    {
        _statistics.AddOrUpdate(providerName, key => CreateStatistics(elapsedMilliseconds, succeeded), (key, current) =>
            {
                var totalRequests = current.TotalRequests + 1;

                var averageResponseTime = ((current.AverageResponseTimeMs * current.TotalRequests) + elapsedMilliseconds) / totalRequests;

                return new ProviderStatistics
                {
                    TotalRequests = totalRequests,
                    SuccessfulRequests = current.SuccessfulRequests
                        + (succeeded ? 1 : 0),
                    FailedRequests = current.FailedRequests
                        + (succeeded ? 0 : 1),
                    AverageResponseTimeMs = averageResponseTime,

                    FastRequests = current.FastRequests
                        + (elapsedMilliseconds < 100 ? 1 : 0),
                    AverageRequests = current.AverageRequests
                        + (elapsedMilliseconds >= 100 && elapsedMilliseconds <= 200 ? 1 : 0),
                    SlowRequests = current.SlowRequests
                        + (elapsedMilliseconds > 200 ? 1 : 0)
                };
            });
        var samples = _recentSamples.GetOrAdd(providerName, _ => new ConcurrentQueue<(DateTime, double)>());
        samples.Enqueue((DateTime.UtcNow, elapsedMilliseconds));
    }

    private static ProviderStatistics CreateStatistics(double elapsedMilliseconds, bool succeeded)
    {
        return new ProviderStatistics
        {
            TotalRequests = 1,
            SuccessfulRequests = succeeded ? 1 : 0,
            FailedRequests = succeeded ? 0 : 1,
            AverageResponseTimeMs = elapsedMilliseconds,

            FastRequests = elapsedMilliseconds < 100 ? 1 : 0,
            AverageRequests = elapsedMilliseconds >= 100 && elapsedMilliseconds <= 200 ? 1 : 0,
            SlowRequests = elapsedMilliseconds > 200 ? 1 : 0
        };
    }

    public IReadOnlyDictionary<string, double> GetRecentAverageResponseTimes(TimeSpan window)
    {
        var cutoff = DateTime.UtcNow - window;
        var result = new Dictionary<string, double>();

        foreach (var entry in _recentSamples)
        {
            while (entry.Value.TryPeek(out var sample) && sample.Timestamp < cutoff)
            {
                entry.Value.TryDequeue(out _);
            }

            var samples = entry.Value.ToArray();

            if (samples.Length > 0)
            {
                result[entry.Key] = samples.Average(x => x.ElapsedMs);
            }
        }

        return result;
    }
}
