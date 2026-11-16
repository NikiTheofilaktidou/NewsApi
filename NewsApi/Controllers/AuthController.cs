using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using NewsApi.Configuration;

namespace NewsApi.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IConfiguration _configuration;
    private readonly JwtOptions _options;

    public AuthController(IConfiguration configuration, IOptions<JwtOptions> options)
    {
        _configuration = configuration;
        _options = options.Value;
    }

    [AllowAnonymous]
    [HttpPost("token")]
    public IActionResult GenerateToken([FromBody] LoginRequest request)
    {
        var jwtSettings = _configuration.GetSection("Jwt");

        var username = jwtSettings["Username"];
        var password = jwtSettings["Password"];

        if (request.Username != username || request.Password != password)
        {
            return Unauthorized(new { error = "Invalid credentials." });
        }

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings["Key"]!));

        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        
        var token = new JwtSecurityToken(issuer: jwtSettings["Issuer"], audience: jwtSettings["Audience"], claims:
            [
                new Claim(ClaimTypes.Name, request.Username)
            ],
            expires: DateTime.UtcNow.AddMinutes(_options.TokenExpiration),
            signingCredentials: credentials);

        return Ok(new
        {
            accessToken = new JwtSecurityTokenHandler().WriteToken(token),
            tokenType = "Bearer",
            expiresIn = _options.TokenExpiration + " minutes"
        });
    }

    public class LoginRequest
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }
}