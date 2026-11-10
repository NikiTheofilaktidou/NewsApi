
# NewsApi - Aggregated Feed Service

## Overview

A .NET 10 ASP.NET Core Web API that aggregates feed data from multiple external providers:

- NewsAPI
- GNews
- OpenWeatherMap

The service fetches data asynchronously, combines results, supports filtering and sorting, and handles individual provider failures without failing the entire request.

## Architecture

The solution follows a layered architecture:

- **NewsApi.Domain** – Core entities and domain models.
- **NewsApi.Application** – Business logic, interfaces, and services.
- **NewsApi.Infrastructure** – External API integrations and configuration.
- **NewsApi** – ASP.NET Core API and HTTP endpoints.
- **NewsApi.UnitTests** – Unit tests.
- **NewsApi.IntegrationTests** – API integration tests.

## Configuration

The application requires API keys for the external providers.

Configure them using .NET User Secrets:

```bash
dotnet user-secrets init --project NewsApi
dotnet user-secrets set "NewsApi:ApiKey" "YOUR_NEWSAPI_KEY" --project NewsApi
dotnet user-secrets set "GNews:ApiKey" "YOUR_GNEWS_KEY" --project NewsApi
dotnet user-secrets set "WeatherApi:ApiKey" "YOUR_OPENWEATHERMAP_KEY" --project NewsApi
```

To run the application locally with live external data, provide valid API keys for the three providers.

Base URLs are configured in `appsettings.json`.

## Authentication

The API uses JWT Bearer authentication to protect its endpoints.

### JWT Configuration

Configure the JWT credentials using .NET User Secrets:

```bash
dotnet user-secrets set "Jwt:Username" "admin" --project NewsApi
dotnet user-secrets set "Jwt:Password" "YOUR_PASSWORD" --project NewsApi
dotnet user-secrets set "Jwt:Key" "YOUR_KEY" --project NewsApi
```

The JWT issuer and audience are already configured in `appsettings.json`.

Replace the placeholders with your existing values. The signing key should be a long, randomly generated secret. Never commit credentials or signing keys to source control.

### Obtaining an Access Token

Send a POST request to:

`http://localhost:5007/api/v1/auth/token`

With the following JSON body:

```json
{
  "username": "admin",
  "password": "YOUR_PASSWORD"
}
```

The response contains an `accessToken`.

### Using the Access Token

Protected endpoints require JWT Bearer authentication.

Include the token in the Authorization header:

```text
Authorization: Bearer YOUR_ACCESS_TOKEN
```

The access token expires after one hour.

### Protected Endpoints

- `GET /api/v1/feed`
- `GET /api/v1/ProviderStatistics`

## Running the Application

```bash
dotnet run --project NewsApi
```

## API Endpoints

### Get Aggregated Feed

`GET /api/v1/feed`

Query parameters:

| Parameter | Description |
|---|---|
| `query` | Search term (required) |
| `category` | Optional category filter |
| `sortBy` | Sort by `date` or `title` |
| `descending` | Sort direction (default: `true`) |

Example:

`GET /api/v1/feed?query=Athens&category=News&sortBy=date&descending=true`


### Example Response

A successful `GET /api/v1/feed` request returns a JSON array of feed items:

```json
[
  {
    "id": "7ff98bd0-a278-4c7c-8df4-d2567e8bca5f",
    "provider": "OpenWeatherMap",
    "title": "Weather in Athens",
    "description": "few clouds. Temperature: 17,91°C",
    "url": null,
    "publishedAt": "2026-11-10T16:15:21.7732541Z",
    "category": "Weather"
  },
  {
    "id": "gnews-example",
    "provider": "GNews",
    "title": "Example news title",
    "description": "Example news description.",
    "url": "https://example.com/article",
    "publishedAt": "2026-11-10T15:30:00Z",
    "category": "News"
  }
]
```

**Response fields:**

- `id`: Unique identifier for the feed item.
- `provider`: Source of the item (NewsAPI, GNews, or OpenWeatherMap).
- `title`: Title of the news article or weather report.
- `description`: Short description or weather information.
- `url`: Link to the original article; can be `null` for weather items.
- `publishedAt`: Publication or retrieval timestamp.
- `category`: Item category, such as `News` or `Weather`.

**Note:** The response may contain different items depending on the search query and the available providers.


### Provider Statistics

`GET /api/v1/ProviderStatistics`

Returns request counts, successful requests, failed requests, and average response times for each provider.

### Performance Monitoring

A background service checks provider performance every minute.

- Calculates average response time over the last 5 minutes.
- Compares it with the provider's overall average response time.
- Logs a warning when recent performance is more than 50% slower.


## Caching

Aggregated feed responses are cached in memory for 2 minutes.

## Error Handling

If an external provider fails, the service logs the error and continues aggregating results from the remaining providers.

## Testing

Run all tests:

```bash
dotnet test
```

Run unit tests only:

```bash
dotnet test NewsApi.UnitTests
```

Run integration tests only:

```bash
dotnet test NewsApi.IntegrationTests
```
