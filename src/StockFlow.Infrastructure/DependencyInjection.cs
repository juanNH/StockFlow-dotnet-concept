using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using StackExchange.Redis;
using StockFlow.Application.Abstractions;
using StockFlow.Application.Identity;
using StockFlow.Application.Products;
using StockFlow.Infrastructure.Caching;
using StockFlow.Infrastructure.Integrations;
using StockFlow.Infrastructure.Identity;
using StockFlow.Infrastructure.Persistence;

namespace StockFlow.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var postgresConnectionString = configuration.GetConnectionString("Postgres")
            ?? throw new InvalidOperationException("ConnectionStrings:Postgres is required.");
        var redisConnectionString = configuration["Redis:ConnectionString"]
            ?? throw new InvalidOperationException("Redis:ConnectionString is required.");
        var frankfurterBaseUrl = configuration["ExternalServices:Frankfurter:BaseUrl"]
            ?? throw new InvalidOperationException("ExternalServices:Frankfurter:BaseUrl is required.");

        services.AddSingleton(_ => NpgsqlDataSource.Create(postgresConnectionString));
        services.AddDbContext<StockFlowDbContext>((serviceProvider, options) =>
            options.UseNpgsql(serviceProvider.GetRequiredService<NpgsqlDataSource>()));
        services.AddScoped<IDbConnectionFactory, NpgsqlDbConnectionFactory>();
        services.AddScoped<IProductRepository, EfProductRepository>();

        services.AddOptions<JwtOptions>()
            .Bind(configuration.GetSection(JwtOptions.SectionName))
            .Validate(options => IsValidJwtOptions(options),
                "Authentication:Jwt requires a valid issuer, audience, 32-byte signing key, and lifetime of 5-60 minutes.")
            .ValidateOnStart();
        services.AddIdentityCore<StockFlowUser>(options =>
        {
            options.User.RequireUniqueEmail = true;
            options.Password.RequiredLength = IdentityPasswordPolicy.MinimumLength;
            options.Password.RequireDigit = true;
            options.Password.RequireLowercase = true;
            options.Password.RequireUppercase = true;
            options.Password.RequireNonAlphanumeric = false;
            options.Lockout.AllowedForNewUsers = false;
        }).AddEntityFrameworkStores<StockFlowDbContext>();
        services.AddSingleton(TimeProvider.System);
        services.AddScoped<JwtAccessTokenIssuer>();
        services.AddScoped<IOrganizationIdentityService, OrganizationIdentityService>();

        services.AddSingleton<IConnectionMultiplexer>(_ =>
        {
            var redisOptions = ConfigurationOptions.Parse(redisConnectionString);
            redisOptions.AbortOnConnectFail = false;
            redisOptions.ConnectTimeout = 1_000;
            redisOptions.AsyncTimeout = 1_000;
            return ConnectionMultiplexer.Connect(redisOptions);
        });
        services.AddScoped<ICacheStore, RedisCacheStore>();

        services.AddHttpClient<IExchangeRateProvider, FrankfurterExchangeRateProvider>(client =>
        {
            client.BaseAddress = new Uri(frankfurterBaseUrl, UriKind.Absolute);
            client.Timeout = TimeSpan.FromSeconds(5);
        });

        return services;
    }

    private static bool IsValidJwtOptions(JwtOptions options)
    {
        try
        {
            options.Validate();
            return true;
        }
        catch (InvalidOperationException)
        {
            return false;
        }
    }
}
