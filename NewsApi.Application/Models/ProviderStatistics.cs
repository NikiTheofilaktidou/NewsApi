namespace NewsApi.Application.Models;

public class ProviderStatistics
{
    public long TotalRequests { get; init; }
    public long SuccessfulRequests { get; init; }
    public long FailedRequests { get; init; }

    public double AverageResponseTimeMs { get; init; }

    public long FastRequests { get; init; }
    public long AverageRequests { get; init; }
    public long SlowRequests { get; init; }
}