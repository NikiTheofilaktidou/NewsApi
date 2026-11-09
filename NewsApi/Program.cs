using NewsApi.Application.Interfaces;
using NewsApi.Application.Services;
using NewsApi.Infrastructure.Configuration;
using NewsApi.Infrastructure.Providers;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.Configure<NewsApiOptions>(builder.Configuration.GetSection("NewsApi"));
builder.Services.Configure<WeatherApiOptions>(builder.Configuration.GetSection("WeatherApi"));
builder.Services.Configure<GNewsOptions>(builder.Configuration.GetSection("GNews"));
builder.Services.AddHttpClient<NewsApiProvider>(); 
builder.Services.AddHttpClient<WeatherApiProvider>();
builder.Services.AddHttpClient<GNewsProvider>();
builder.Services.AddScoped<IExternalApiProvider, NewsApiProvider>();
builder.Services.AddScoped<IExternalApiProvider, WeatherApiProvider>();
builder.Services.AddScoped<IExternalApiProvider, GNewsProvider>();
builder.Services.AddScoped<IFeedService, FeedService>();
builder.Services.AddHostedService<ProviderAnomalyMonitor>();
builder.Services.AddSingleton<IProviderStatisticsService,ProviderStatisticsService>();

builder.Services.AddMemoryCache();

var jwtSettings = builder.Configuration.GetSection("Jwt");

var jwtKey = jwtSettings["Key"]
    ?? throw new InvalidOperationException("JWT signing key is missing.");

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,

            ValidIssuer = jwtSettings["Issuer"],
            ValidAudience = jwtSettings["Audience"],

            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtKey))
        };
    });

builder.Services.AddAuthorization();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
public partial class Program { }
