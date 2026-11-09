using System.Net.Http.Json;
using NewsApi.Application.Interfaces;
using NewsApi.Domain.Entities;
using NewsApi.Infrastructure.Configuration;
using Microsoft.Extensions.Options;

namespace NewsApi.Infrastructure.Providers
{
    public class WeatherApiProvider : IExternalApiProvider
    {
        private readonly HttpClient _httpClient;
        private readonly WeatherApiOptions _options;

        public WeatherApiProvider(HttpClient httpClient, IOptions<WeatherApiOptions> options)
        {
            _httpClient = httpClient;
            _options = options.Value;
        }
        public string Name => "OpenWeatherMap";

        public async Task<IEnumerable<FeedItem>> FetchAsync(string query, CancellationToken cancellationToken)
        {
            var url = new Uri(new Uri(_options.BaseUrl),$"weather?q={Uri.EscapeDataString(query)}&appid={_options.ApiKey}&units=metric");
            using var response = await _httpClient.GetAsync(url, cancellationToken);

            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadFromJsonAsync<WeatherApiResponse>(cancellationToken: cancellationToken);

            if (result is null)
            {
                throw new InvalidOperationException("OpenWeatherMap returned an empty response.");
            }

            var feedItem = new FeedItem
            {
                Provider = Name,
                Title = $"Weather in {result.Name}",
                Description = $"{result.Weather?.FirstOrDefault()?.Description}. Temperature: {result.Main?.Temp}°C",
                PublishedAt = DateTime.UtcNow,
                Category = "Weather"
            };

            return [feedItem];
        }
        private sealed class WeatherApiResponse
        {
            public string? Name { get; set; }
            public WeatherMain? Main { get; set; }
            public List<WeatherDescription>? Weather { get; set; }
        }

        private sealed class WeatherMain
        {
            public double Temp { get; set; }
        }

        private sealed class WeatherDescription
        {
            public string? Description { get; set; }
        }
    }
}
