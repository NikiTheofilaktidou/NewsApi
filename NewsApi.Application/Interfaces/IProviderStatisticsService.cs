using NewsApi.Application.Models;

namespace NewsApi.Application.Interfaces;

public interface IProviderStatisticsService
{
    void RecordSuccess(string providerName, double elapsedMs);

    void RecordFailure(string providerName, double elapsedMs);

    IReadOnlyDictionary<string, ProviderStatistics> GetAll();
    IReadOnlyDictionary<string, double> GetRecentAverageResponseTimes(TimeSpan window);
}