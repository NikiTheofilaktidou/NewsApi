
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;

namespace NewsApi.IntegrationTests;

public class JwtAuthenticationTests
{
    private const string TestKey = "ThisIsJustATestSigningKeyThatIsLongEnoughForHmacSha256!";

    [Fact]
    public async Task GetFeed_WithExpiredJwt_ReturnsUnauthorized()
    {
        // Arrange
        using var factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureAppConfiguration((_, configuration) =>
                {
                    configuration.AddInMemoryCollection(
                        new Dictionary<string, string?>
                        {
                            ["Jwt:Key"] = TestKey,
                            ["Jwt:Issuer"] = "NewsApi",
                            ["Jwt:Audience"] = "NewsApiClient"
                        });
                });
            });

        using var client = factory.CreateClient();

        var expiredToken = CreateExpiredToken();

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", expiredToken);

        // Act
        var response = await client.GetAsync("/api/v1/feed?query=Athens");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private static string CreateExpiredToken()
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(TestKey));

        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(issuer: "NewsApi",audience: "NewsApiClient", claims:
            [
                new Claim(ClaimTypes.Name, "TestUser")
            ],
            notBefore: DateTime.UtcNow.AddMinutes(-20),
            expires: DateTime.UtcNow.AddMinutes(-10),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
