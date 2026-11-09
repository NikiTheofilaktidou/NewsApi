using System.Net.Http.Json;
using NewsApi.Application.Interfaces;
using NewsApi.Domain.Entities;
using NewsApi.Infrastructure.Configuration;
using Microsoft.Extensions.Options;

namespace NewsApi.Infrastructure.Providers
{
    public class NewsApiProvider : IExternalApiProvider
    {
        private readonly HttpClient _httpClient;
        private readonly NewsApiOptions _options;
        public NewsApiProvider(HttpClient httpClient, IOptions<NewsApiOptions> options)
        {
            _httpClient = httpClient;
            _options = options.Value;
        }
        public string Name => "NewsApi";

        public async Task<IEnumerable<FeedItem>> FetchAsync(string query, CancellationToken cancellationToken)
        {
            var url = new Uri(new Uri(_options.BaseUrl), $"everything?q={Uri.EscapeDataString(query)}&sortBy=publishedAt&pageSize=100");

            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Add("X-Api-Key", _options.ApiKey);
            request.Headers.UserAgent.ParseAdd("NewsApi/1.0");

            using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(
                    cancellationToken);

                throw new HttpRequestException(
                    $"{Name} returned {(int)response.StatusCode}: {errorBody}");
            }

            var result = await response.Content.ReadFromJsonAsync<NewsApiResponse>(cancellationToken: cancellationToken);

            if (result is null)
            {
                throw new InvalidOperationException("News API returned an empty response.");
            }

            if (result.Status != "ok")
            {
                throw new InvalidOperationException(
                    $"News API error: {result.Message}");
            }

            var articles = result.Articles ?? [];

            var feedItems = articles
                .Where(article => !string.IsNullOrWhiteSpace(article.Title))
                .Select(article => new FeedItem
                {
                    Provider = Name,
                    Title = article.Title!,
                    Description = article.Description ?? article.Content ?? string.Empty,
                    Url = article.Url,
                    PublishedAt = article.PublishedAt?.UtcDateTime ?? DateTime.UtcNow,
                    Category = "News"
                }).ToList();

            return feedItems;
        }

        private sealed class NewsApiResponse
        {
            public string? Status { get; set; }
            public string? Message { get; set; }
            public List<NewsArticle>? Articles { get; set; }
        }

        private sealed class NewsArticle
        {
            public string? Title { get; set; }
            public string? Description { get; set; }
            public string? Content { get; set; }
            public string? Url { get; set; }
            public DateTimeOffset? PublishedAt { get; set; }
        }
    }
}
