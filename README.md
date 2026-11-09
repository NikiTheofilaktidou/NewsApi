
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
