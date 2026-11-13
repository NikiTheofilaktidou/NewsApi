using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NewsApi.Application.Interfaces;
using NewsApi.Domain.Entities;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Encodings.Web;

namespace NewsApi.IntegrationTests;

public class FeedApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public FeedApiTests(WebApplicationFactory<Program> factory)
    {
        var application = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IExternalApiProvider>();
                services.AddSingleton<IExternalApiProvider>( new FakeExternalApiProvider());
                services.AddAuthentication("Test").AddScheme<AuthenticationSchemeOptions, TestAuthHandler>("Test", options => { });
            });
        });

        _client = application.CreateClient();
        _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "test-token");
    }

    [Fact]
    public async Task GetFeed_WithValidQuery_ReturnsOk()
    {
        // Act
        var response = await _client.GetAsync("/api/v1/feed?query=Athens");

        // Assert
        Assert.Equal( System.Net.HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetFeed_WithoutQuery_ReturnsBadRequest()
    {
        // Act
        var response = await _client.GetAsync("/api/v1/feed");

        // Assert
        Assert.Equal(System.Net.HttpStatusCode.BadRequest,response.StatusCode);
    }

    [Fact]
    public async Task GetFeed_WithValidQuery_ReturnsFeedItems()
    {
        // Act
        var response = await _client.GetAsync("/api/v1/feed?query=Athens");

        var items = await response.Content.ReadFromJsonAsync<List<FeedItem>>();

        // Assert
        Assert.NotNull(items);
        Assert.Single(items);
        Assert.Equal("Test feed item", items[0].Title);
    }

    [Fact]
    public async Task GetFeed_WithInvalidToken_ReturnsUnauthorized()
    {
        // Arrange
        _client.DefaultRequestHeaders.Authorization =  new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "invalid-token");

        // Act
        var response = await _client.GetAsync( "/api/v1/feed?query=Athens");

        // Assert
        Assert.Equal( System.Net.HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetFeed_WithInvalidCategory_ReturnsBadRequest()
    {
        var response = await _client.GetAsync("/api/v1/feed?query=Athens&category=InvalidCategory");

        Assert.Equal(System.Net.HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetFeed_WithValidCategory_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/v1/feed?query=Athens&category=News");

        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetFeed_WithLowercaseCategory_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/v1/feed?query=Athens&category=news");
        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
    }

    private class FakeExternalApiProvider : IExternalApiProvider
    {
        public string Name => "FakeProvider";

        public IEnumerable<string> Categories => ["News"];

        public Task<IEnumerable<FeedItem>> FetchAsync(string query, CancellationToken cancellationToken)
        {
            IEnumerable<FeedItem> items =
            [
                new FeedItem
                {
                    Provider = Name,
                    Title = "Test feed item",
                    Description = "Test description",
                    Category = "News",
                    PublishedAt = DateTime.UtcNow
                }
            ];

            return Task.FromResult(items);
        }
    }

    private class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        public TestAuthHandler(IOptionsMonitor<AuthenticationSchemeOptions> options,ILoggerFactory logger, UrlEncoder encoder) : base(options, logger, encoder)
        {
        }

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            var authorizationHeader = Request.Headers.Authorization.ToString();

            if (authorizationHeader != "Bearer test-token")
            {
                return Task.FromResult( AuthenticateResult.Fail("Invalid test token."));
            }

            var claims = new[] { new Claim(ClaimTypes.Name, "TestUser") };

            var identity = new ClaimsIdentity(claims, "Test");
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, "Test");

            return Task.FromResult(AuthenticateResult.Success(ticket));
        }
    }
}