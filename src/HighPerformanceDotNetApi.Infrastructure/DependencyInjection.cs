using HighPerformanceDotNetApi.Application.Pricing;
using HighPerformanceDotNetApi.Application.Products;
using HighPerformanceDotNetApi.Infrastructure.Caching;
using HighPerformanceDotNetApi.Infrastructure.Data;
using HighPerformanceDotNetApi.Infrastructure.Pricing;
using HighPerformanceDotNetApi.Infrastructure.Products;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Polly;
using Polly.Extensions.Http;
using StackExchange.Redis;

namespace HighPerformanceDotNetApi.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContextPool<AppDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("Postgres")));

        services.AddScoped<ProductSeeder>();
        services.AddScoped<IProductReadRepository, EfProductReadRepository>();

        var redisConnection = configuration.GetConnectionString("Redis");
        if (!string.IsNullOrWhiteSpace(redisConnection))
        {
            services.AddSingleton<IConnectionMultiplexer>(_ =>
            {
                var options = ConfigurationOptions.Parse(redisConnection);
                options.AbortOnConnectFail = false;
                options.ConnectRetry = 5;
                options.ConnectTimeout = 5_000;
                options.SyncTimeout = 5_000;
                return ConnectionMultiplexer.Connect(options);
            });
            services.AddSingleton<IProductCache, RedisProductCache>();
        }
        else
        {
            services.AddSingleton<IProductCache, NullProductCache>();
        }

        services.AddHttpClient<IProductPricingClient, ResilientPricingClient>(client =>
            {
                client.BaseAddress = new Uri(configuration["ExternalPricing:BaseUrl"] ?? "https://pricing.invalid");
                client.Timeout = TimeSpan.FromSeconds(2);
            })
            .AddPolicyHandler(HttpPolicyExtensions
                .HandleTransientHttpError()
                .Or<TaskCanceledException>()
                .WaitAndRetryAsync(3, attempt => TimeSpan.FromMilliseconds(100 * Math.Pow(2, attempt))))
            .AddPolicyHandler(HttpPolicyExtensions
                .HandleTransientHttpError()
                .CircuitBreakerAsync(5, TimeSpan.FromSeconds(30)));

        return services;
    }
}
