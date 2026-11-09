using System.Net.Http.Json;
using System.Text.Json.Serialization;
using NewsApi.Application.Interfaces;
using NewsApi.Domain.Entities;
using NewsApi.Infrastructure.Configuration;
using Microsoft.Extensions.Options;

namespace NewsApi.Infrastructure.Providers;

public class GNewsProvider : IExternalApiProvider
{
    private readonly HttpClient _httpClient;
    private readonly GNewsOptions _options;

    public GNewsProvider(HttpClient httpClient,IOptions<GNewsOptions> options)
    {
        _httpClient = httpClient;
        _options = options.Value;
    }

    public string Name => "GNews";

    public async Task<IEnumerable<FeedItem>> FetchAsync(string query, CancellationToken cancellationToken)
    {
        var url = new Uri( new Uri(_options.BaseUrl), $"search?q={Uri.EscapeDataString(query)}" + $"&lang=en&max=10&apikey={Uri.EscapeDataString(_options.ApiKey)}");

        using var response = await _httpClient.GetAsync(url, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);

            throw new HttpRequestException($"GNews returned {(int)response.StatusCode}: {errorBody}");
        }

        var result = await response.Content.ReadFromJsonAsync<GNewsApiResponse>(cancellationToken: cancellationToken);

        if (result is null)
        {
            throw new InvalidOperationException( "GNews returned an empty response.");
        }

        var items = result.Articles ?? [];

        return items.Where(item => !string.IsNullOrWhiteSpace(item.Title)).Select(item => new FeedItem
            {
                Id = $"gnews-{item.Url}",
                Provider = Name,
                Title = item.Title!,
                Description = item.Description ?? string.Empty,
                Url = item.Url,
                PublishedAt = item.PublishedAt?.UtcDateTime ?? DateTime.UtcNow,
                Category = "News"
            }).ToList();
    }

    private sealed class GNewsApiResponse
    {
        [JsonPropertyName("articles")]
        public List<GNewsArticle>? Articles { get; set; }
    }

    private sealed class GNewsArticle
    {
        [JsonPropertyName("title")]
        public string? Title { get; set; }

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("url")]
        public string? Url { get; set; }

        [JsonPropertyName("publishedAt")]
        public DateTimeOffset? PublishedAt { get; set; }
    }
}