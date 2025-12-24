using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;
using Catalog.Application.Interfaces;

namespace Catalog.Infrastructure.Services;

public class RedisCacheService : ICacheService
{
    private readonly IDistributedCache _cache;
    private readonly JsonSerializerOptions _jsonOptions;
    
    public RedisCacheService(IDistributedCache cache)
    {
        _cache = cache;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };
    }
    
    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        var value = await _cache.GetStringAsync(key, cancellationToken);
        return value == null ? default : JsonSerializer.Deserialize<T>(value, _jsonOptions);
    }
    
    public async Task SetAsync<T>(string key, T value, DistributedCacheEntryOptions options, 
        CancellationToken cancellationToken = default)
    {
        var json = JsonSerializer.Serialize(value, _jsonOptions);
        await _cache.SetStringAsync(key, json, options, cancellationToken);
    }
    
    public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        await _cache.RemoveAsync(key, cancellationToken);
    }
}