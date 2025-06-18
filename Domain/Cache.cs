namespace Void.Platform.Domain;

using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;
using System.Text.Json;
using System.Reflection;
using ZiggyCreatures.Caching.Fusion.Backplane;
using ZiggyCreatures.Caching.Fusion.Backplane.StackExchangeRedis;
using ZiggyCreatures.Caching.Fusion.Serialization.SystemTextJson;
using ZiggyCreatures.Caching.Fusion;

//=================================================================================================
// CONFIGURATION
//=================================================================================================

public record CacheOptions
{
  public string? RedisUrl { get; init; }
  public JsonSerializerOptions? JsonSerializerOptions { get; init; }
}

public record CacheEntryOptions
{
  public Duration? Duration;
  public CacheEntryOptions(Duration? duration = null)
  {
    Duration = duration;
  }
}

//=================================================================================================
// INTERFACE
//=================================================================================================

public interface ICache
{
  bool Contains(string key);
  ValueTask<bool> ContainsAsync(string key);

  TValue? GetOrDefault<TValue>(
    string key,
    TValue? defaultValue = default);

  TValue GetOrSet<TValue>(
    string key,
    Func<FusionCacheFactoryExecutionContext<TValue>, CancellationToken, TValue> factory,
    CacheEntryOptions? options = null);

  ValueTask<TValue> GetOrSetAsync<TValue>(
    string key,
    Func<FusionCacheFactoryExecutionContext<TValue>, CancellationToken, Task<TValue>> factory,
    CacheEntryOptions? options = null,
    CancellationToken token = default);

  ValueTask SetAsync<TValue>(
    string key,
    TValue value,
    CacheEntryOptions? options = null,
    CancellationToken token = default);

  ValueTask RemoveAsync(
    string key,
    CancellationToken token = default);

  ValueTask<MaybeValue<TValue>> TryGetAsync<TValue>(
    string key);

  Task<List<string>> ListKeysAsync(
    string prefix,
    int max = 100,
    CancellationToken token = default);
}

//=================================================================================================
// CACHE KEY GENERATOR
//=================================================================================================

public static class CacheKey
{
  //-----------------------------------------------------------------------------------------------

  public static string ForDownloads(string repo) =>
    For("download", "releases", repo);

  public static string ForGameServe(string orgSlug, string gameSlug, string deploySlug) =>
    For("game", "serve", orgSlug, gameSlug, deploySlug);

  public static string ForGameServePassword(Share.Branch branch) =>
    For("game", "serve", "password", branch);

  public static string ForSysAdminDatabaseStats() =>
    For("sysadmin", "stats", "database");

  public static string ForSysAdminFileStats() =>
    For("sysadmin", "stats", "files");

  public static string FirewallBlocked(string ip) =>
    For("firewall", "blocked", ip);

  public static string SafeTrash(string path) =>
    For("trash", "safe", path);

  public static string UnsafeTrash(string path) =>
    For("trash", "unsafe", path);

  //-----------------------------------------------------------------------------------------------

  public static string For(params dynamic[] parts)
  {
    return String.Join(":", parts.Select(p => Part(p)));
  }

  //-----------------------------------------------------------------------------------------------

  private static string Part(dynamic value)
  {
    if (value is string)
      return value;
    else if (value is long)
      return value.ToString();
    else if (HasId(value, out long result))
      return result.ToString();
    else
      return value.ToString();
  }

  private static bool HasId(dynamic obj, out long value)
  {
    value = default;

    if (obj is null)
      return false;

    Type type = obj.GetType();
    if (!type.IsClass && !type.IsValueType)
      return false; // Ensures it's a class, struct, or record

    PropertyInfo? idProperty = type.GetProperty("Id", BindingFlags.Public | BindingFlags.Instance);
    if (idProperty is null || idProperty.PropertyType != typeof(long))
      return false;

    value = (long) idProperty.GetValue(obj);
    return true;
  }
}

//=================================================================================================
// DI SERVICE PROVIDER
//=================================================================================================

public static class CacheExtensions
{
  public static IServiceCollection AddVoidCache(this IServiceCollection sp, CacheOptions options)
  {
    if (options.RedisUrl is not null)
    {
      sp.AddSingleton<IConnectionMultiplexer>(sp =>
      {
        return ConnectionMultiplexer.Connect(options.RedisUrl);
      });

      sp.AddStackExchangeRedisCache(o =>
      {
        o.Configuration = options.RedisUrl;
      });

      sp.AddSingleton<IFusionCacheBackplane>(sp => new RedisBackplane(new RedisBackplaneOptions
      {
        Configuration = options.RedisUrl
      }));
    }
    sp.AddSingleton<CacheOptions>(options);
    sp.AddSingleton<ICache, Cache>();
    return sp;
  }
}

//=================================================================================================
// IMPLEMENTATION
//  - We implement a hybrid L1 (memory) + L2 (redis) cache
//  - As a wrapper around the Fusion Cache library - https://github.com/ZiggyCreatures/FusionCache
//=================================================================================================

public class Cache : ICache
{
  //-----------------------------------------------------------------------------------------------

  private FusionCache fusion;
  private IConnectionMultiplexer? redis;

  public Cache(CacheOptions? options = null, IDistributedCache? distributedCache = null, IFusionCacheBackplane? backplane = null, IConnectionMultiplexer? redis = null)
  {
    var fusionOptions = new FusionCacheOptions
    {
      DefaultEntryOptions = new FusionCacheEntryOptions
      {
        IsFailSafeEnabled = true,
        FailSafeMaxDuration = TimeSpan.FromHours(1),
        FailSafeThrottleDuration = TimeSpan.FromSeconds(30),
        FactorySoftTimeout = TimeSpan.FromMilliseconds(100),
        FactoryHardTimeout = TimeSpan.FromMilliseconds(5000),
        DistributedCacheSoftTimeout = TimeSpan.FromSeconds(1),
        DistributedCacheHardTimeout = TimeSpan.FromSeconds(2),
        AllowBackgroundDistributedCacheOperations = true
      },
    };

    this.redis = redis; // only used to peek behind the curtain in ListKeys

    this.fusion = new FusionCache(fusionOptions, new MemoryCache(new MemoryCacheOptions
    {
    }));

    if (options?.JsonSerializerOptions is not null)
      fusion.SetupSerializer(new FusionCacheSystemTextJsonSerializer(options.JsonSerializerOptions));

    if (distributedCache is not null)
      fusion.SetupDistributedCache(distributedCache);

    if (backplane is not null)
      fusion.SetupBackplane(backplane);
  }

  //-----------------------------------------------------------------------------------------------

  public bool Contains(string key)
  {
    return fusion.TryGet<object>(key).HasValue;
  }

  public async ValueTask<bool> ContainsAsync(string key)
  {
    return (await fusion.TryGetAsync<object>(key)).HasValue;
  }

  public TValue? GetOrDefault<TValue>(string key, TValue? defaultValue = default)
  {
    return fusion.GetOrDefault<TValue>(key, defaultValue);
  }

  public TValue GetOrSet<TValue>(string key, Func<FusionCacheFactoryExecutionContext<TValue>, CancellationToken, TValue> fn, CacheEntryOptions? options = null)
  {
    return fusion.GetOrSet(key, factory: fn, options: ToFusion(options));
  }

  public ValueTask<TValue> GetOrSetAsync<TValue>(string key, Func<FusionCacheFactoryExecutionContext<TValue>, CancellationToken, Task<TValue>> fn, CacheEntryOptions? options = null, CancellationToken token = default)
  {
    return fusion.GetOrSetAsync<TValue>(key, factory: fn, options: ToFusion(options), token: token);
  }

  public ValueTask SetAsync<TValue>(string key, TValue value, CacheEntryOptions? options = null, CancellationToken token = default)
  {
    return fusion.SetAsync<TValue>(key, value, options: ToFusion(options), token: token);
  }

  public ValueTask RemoveAsync(string key, CancellationToken token = default)
  {
    return fusion.RemoveAsync(key, token: token);
  }

  public ValueTask<MaybeValue<TValue>> TryGetAsync<TValue>(string key)
  {
    return fusion.TryGetAsync<TValue>(key);
  }

  //-----------------------------------------------------------------------------------------------

  public async Task<List<string>> ListKeysAsync(string prefix, int max = 100, CancellationToken token = default)
  {
    if (redis is null)
    {
      return new List<string> { "keys cannot be listed unless redis is enabled" };
    }

    var db = redis.GetDatabase();
    var endpoints = redis.GetEndPoints();
    var server = redis.GetServer(endpoints[0]);
    var keys = server.Keys(pattern: $"v2:{prefix}*", pageSize: max);

    await Task.CompletedTask;
    return keys.Select(k => k.ToString().Replace("v2:", "")).ToList();
  }

  //-----------------------------------------------------------------------------------------------

  private FusionCacheEntryOptions ToFusion(CacheEntryOptions? options)
  {
    return new FusionCacheEntryOptions(options?.Duration?.ToTimeSpan())
    {
    };
  }
}