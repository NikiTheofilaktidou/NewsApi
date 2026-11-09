using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using NewsApi.Application.Interfaces;
using NewsApi.Application.Models;
using NewsApi.Domain.Entities;
using System.Diagnostics;

namespace NewsApi.Application.Services
{
    public class FeedService : IFeedService
    {
        private readonly ILogger<FeedService> _logger;
        private readonly IEnumerable<IExternalApiProvider> _providers;
        private readonly IMemoryCache _cache;
        private readonly IProviderStatisticsService _statistics;
        public FeedService(IEnumerable<IExternalApiProvider> providers, ILogger<FeedService> logger, IMemoryCache cache, IProviderStatisticsService statistics)
        {
            _providers = providers;
            _logger = logger;
            _cache = cache;
            _statistics = statistics;
        }
        public async Task<IEnumerable<FeedItem>> GetFeedAsync(FeedRequest request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.Query))
            {
                throw new ArgumentException("Query cannot be empty.", nameof(request.Query));
            }

            // Caching 
            var cacheKey = $"feed:{request.Query}:{request.Category}:{request.SortBy}:{request.Descending}";

            if (_cache.TryGetValue(cacheKey, out List<FeedItem>? cachedItems))
            {
                return cachedItems!;
            }

            var tasks = _providers.Select(provider => FetchSafelyAsync(provider, request, cancellationToken));
            var results = await Task.WhenAll(tasks);
            var feedItems = results.SelectMany(result => result);

            // Filter by category if specified
            if (!string.IsNullOrEmpty(request.Category))
            {
                feedItems = feedItems.Where(item => item.Category.Equals(request.Category, StringComparison.OrdinalIgnoreCase));
            }

            // Sort the feed items based on the request parameters
            feedItems = request.SortBy.ToLower() switch
            {
                "title" => request.Descending ? feedItems.OrderByDescending(item => item.Title) : feedItems.OrderBy(item => item.Title),
                _ => request.Descending ? feedItems.OrderByDescending(item => item.PublishedAt) : feedItems.OrderBy(item => item.PublishedAt)
            };

            var finalItems = feedItems.ToList();

            _cache.Set(cacheKey, finalItems, TimeSpan.FromMinutes(2));

            return finalItems;
        }


        private async Task<IEnumerable<FeedItem>> FetchSafelyAsync(IExternalApiProvider provider,FeedRequest request,CancellationToken cancellationToken)
        {
            var stopwatch = Stopwatch.StartNew();

            try
            {
                var result = await provider.FetchAsync(request.Query, cancellationToken);

                stopwatch.Stop();

                _statistics.RecordSuccess( provider.Name, stopwatch.Elapsed.TotalMilliseconds);

                return result;
            }
            catch (OperationCanceledException)
                when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _statistics.RecordFailure(provider.Name,stopwatch.Elapsed.TotalMilliseconds);

                if (stopwatch.Elapsed.TotalSeconds > 5)
                {
                    _logger.LogWarning("Anomaly detected: {ProviderName} took {ElapsedMs} ms to respond.", provider.Name,stopwatch.Elapsed.TotalMilliseconds);
                }
                _logger.LogError(ex,"{ProviderName} provider failed.", provider.Name);

                return Enumerable.Empty<FeedItem>();
            }
        }

    }
}
