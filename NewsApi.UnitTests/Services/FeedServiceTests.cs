using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using NewsApi.Application.Interfaces;
using NewsApi.Application.Models;
using NewsApi.Application.Services;
using NewsApi.Domain.Entities;

namespace NewsApi.UnitTests.Services
{
    public class FeedServiceTests
    {
        [Fact]
        public async Task GetFeedAsync_EmptyQuery_ThrowsArgumentException()
        {
            // Arrange
            var service = new FeedService(Enumerable.Empty<IExternalApiProvider>(), NullLogger<FeedService>.Instance, new MemoryCache(new MemoryCacheOptions()), new ProviderStatisticsService());

            var request = new FeedRequest { Query = "" };

            //Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => service.GetFeedAsync(request, CancellationToken.None));
        }

        [Fact]
        public async Task GetFeedAsync_MultipleProviders_ReturnsCombinedFeedItems()
        {
            // Arrange
            var provider1 = new FakeExternalApiProvider("NewsAPI", new List<FeedItem> { new() { Title = "News Item", Provider = "NewsAPI" } });
            var provider2 = new FakeExternalApiProvider("OpenWeatherMap", new List<FeedItem> { new() { Title = "Weather item", Provider = "OpenWeatherMap" } });

            var service = new FeedService([provider1, provider2], NullLogger<FeedService>.Instance, new MemoryCache(new MemoryCacheOptions()), new ProviderStatisticsService());

            var request = new FeedRequest { Query = "Athens" };

            // Act 
            var result = (await service.GetFeedAsync(request, CancellationToken.None)).ToList();

            // Assert
            Assert.Equal(2, result.Count);
            Assert.Contains(result, item => item.Title == "News Item");
            Assert.Contains(result, item => item.Title == "Weather item");

        }

        [Fact]
        public async Task GetFeedAsync_OneProviderFails_ReturnsResultsFromRestProviders()
        {
            // Arrange
            var failingProvider = new FakeExternalApiProvider("NewsAPI", new List<FeedItem>(), shouldFail: true);

            var successfulProvider = new FakeExternalApiProvider("OpenWeatherMap", new List<FeedItem> { new() { Title = "Weather item", Provider = "OpenWeatherMap" } });

            var service = new FeedService([failingProvider, successfulProvider], NullLogger<FeedService>.Instance, new MemoryCache(new MemoryCacheOptions()), new ProviderStatisticsService());

            var request = new FeedRequest { Query = "Athens" };

            // Act
            var result = (await service.GetFeedAsync(request, CancellationToken.None)).ToList();

            // Assert
            Assert.Single(result);
            Assert.Equal("Weather item", result[0].Title);
        }

        [Fact]
        public async Task GetFeedAsync_SortByDateDescending_ReturnsNewestFirst()
        {
            // Arrange
            var olderItem = new FeedItem
            {
                Title = "Then news",
                PublishedAt = DateTime.UtcNow.AddDays(-2)
            };

            var newerItem = new FeedItem
            {
                Title = "Now news",
                PublishedAt = DateTime.UtcNow
            };

            var provider = new FakeExternalApiProvider("NewsAPI", new[] { olderItem, newerItem });

            var service = new FeedService(new[] { provider }, NullLogger<FeedService>.Instance, new MemoryCache(new MemoryCacheOptions()), new ProviderStatisticsService());

            var request = new FeedRequest
            {
                Query = "Athens",
                SortBy = "date",
                Descending = true
            };

            // Act
            var result = (await service.GetFeedAsync(
                request,
                CancellationToken.None)).ToList();

            // Assert
            Assert.Equal(2, result.Count);
            Assert.Equal("Now news", result[0].Title);
            Assert.Equal("Then news", result[1].Title);
        }

        [Fact]
        public async Task GetFeedAsync_CategorySpecified_ReturnsOnlyMatchingItems()
        {
            // Arrange
            var provider = new FakeExternalApiProvider("TestProvider", new List<FeedItem>
                {
            new() { Title = "News item", Category = "News" },
            new() { Title = "Weather item", Category = "Weather" },
            new() { Title = "Social item", Category = "SocialMedia" }
                });

            var service = new FeedService(new[] { provider }, NullLogger<FeedService>.Instance, new MemoryCache(new MemoryCacheOptions()), new ProviderStatisticsService());

            var request = new FeedRequest
            {
                Query = "Athens",
                Category = "News"
            };

            // Act
            var result = (await service.GetFeedAsync(request, CancellationToken.None)).ToList();

            // Assert
            Assert.Single(result);
            Assert.Equal("News", result[0].Category);
        }

        [Fact]
        public async Task GetFeedAsync_SortByDateAscending_ReturnsOldestFirst()
        {
            // Arrange
            var olderItem = new FeedItem
            {
                Title = "Older news",
                PublishedAt = DateTime.UtcNow.AddDays(-2)
            };

            var newerItem = new FeedItem
            {
                Title = "Newer news",
                PublishedAt = DateTime.UtcNow
            };

            var provider = new FakeExternalApiProvider("NewsAPI", new[] { newerItem, olderItem });

            var service = new FeedService(new[] { provider }, NullLogger<FeedService>.Instance, new MemoryCache(new MemoryCacheOptions()), new ProviderStatisticsService());

            var request = new FeedRequest
            {
                Query = "Athens",
                SortBy = "date",
                Descending = false
            };

            // Act
            var result = (await service.GetFeedAsync(request, CancellationToken.None)).ToList();

            // Assert
            Assert.Equal(2, result.Count);
            Assert.Equal("Older news", result[0].Title);
            Assert.Equal("Newer news", result[1].Title);
        }

        [Fact]
        public async Task GetFeedAsync_SortByTitleAscending_ReturnsAlphabetically()
        {
            // Arrange
            var provider = new FakeExternalApiProvider("TestProvider", new List<FeedItem>
            {
            new() { Title = "Weather" },
            new() { Title = "Apple" },
            new() { Title = "News" }
                });

            var service = new FeedService(new[] { provider }, NullLogger<FeedService>.Instance, new MemoryCache(new MemoryCacheOptions()), new ProviderStatisticsService());

            var request = new FeedRequest
            {
                Query = "Athens",
                SortBy = "title",
                Descending = false
            };

            // Act
            var result = (await service.GetFeedAsync(request, CancellationToken.None)).ToList();

            // Assert
            Assert.Equal(3, result.Count);
            Assert.Equal("Apple", result[0].Title);
            Assert.Equal("News", result[1].Title);
            Assert.Equal("Weather", result[2].Title);
        }

        [Fact]
        public async Task GetFeedAsync_WhenCalledTwice_UsesCache()
        {
            // Arrange
            var provider = new FakeExternalApiProvider("TestProvider",[ new FeedItem
            {
                Title = "Cached article",
                Category = "News",
                PublishedAt = DateTime.UtcNow
            }
                ]);

            var service = new FeedService([provider],NullLogger<FeedService>.Instance, new MemoryCache(new MemoryCacheOptions()), new ProviderStatisticsService());

            var request = new FeedRequest
            {
                Query = "Athens"
            };

            // Act
            await service.GetFeedAsync(request, CancellationToken.None);
            await service.GetFeedAsync(request, CancellationToken.None);

            // Assert
            Assert.Equal(1, provider.CallCount);
        }

        private class FakeExternalApiProvider : IExternalApiProvider
        {
            public string Name { get; }
            private readonly IEnumerable<FeedItem> _feedItems;
            private readonly bool _shouldFail;
            public int CallCount { get; private set; }

            public FakeExternalApiProvider(string name, IEnumerable<FeedItem> feedItems, bool shouldFail = false)
            {
                Name = name;
                _feedItems = feedItems;
                _shouldFail = shouldFail;
            }

            public Task<IEnumerable<FeedItem>> FetchAsync(string query, CancellationToken cancellationToken)
            {
                if (_shouldFail)
                {
                    throw new HttpRequestException("Provider failed.");
                }

                // Increment each time the provider fetches data.
                CallCount++;

                return Task.FromResult(_feedItems);
            }
        }
    }
}
