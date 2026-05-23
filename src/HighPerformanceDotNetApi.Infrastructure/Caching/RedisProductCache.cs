using System.Text.Json;
using HighPerformanceDotNetApi.Application.Products;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace HighPerformanceDotNetApi.Infrastructure.Caching;

public sealed class RedisProductCache(IConnectionMultiplexer redis, ILogger<RedisProductCache> logger) : IProductCache
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    public async Task<IReadOnlyList<ProductSummaryDto>?> GetTopRatedAsync(int count, CancellationToken cancellationToken)
    {
        RedisValue payload;
        try
        {
            payload = await redis.GetDatabase().StringGetAsync(CacheKey(count));
            if (payload.IsNullOrEmpty)
            {
                return null;
            }
        }
        catch (RedisException ex)
        {
            logger.LogWarning(ex, "Redis read failed. Falling back to database.");
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<IReadOnlyList<ProductSummaryDto>>(payload!, SerializerOptions);
        }
        catch (JsonException ex)
        {
            logger.LogWarning(ex, "Redis cache payload for top rated products was invalid.");
            return null;
        }
    }

    public Task SetTopRatedAsync(int count, IReadOnlyList<ProductSummaryDto> products, TimeSpan ttl, CancellationToken cancellationToken)
    {
        var payload = JsonSerializer.Serialize(products, SerializerOptions);
        return SetAsync(count, payload, ttl);
    }

    private async Task SetAsync(int count, string payload, TimeSpan ttl)
    {
        try
        {
            await redis.GetDatabase().StringSetAsync(CacheKey(count), payload, ttl);
        }
        catch (RedisException ex)
        {
            logger.LogWarning(ex, "Redis write failed. Response will be served without cache persistence.");
        }
    }

    private static string CacheKey(int count) => $"products:top-rated:{count}";
}
