using System.Collections.Concurrent;
using ERP.Application.Common.Interfaces;
using Microsoft.Extensions.Caching.Memory;

namespace ERP.Infrastructure.Caching;

/// <summary>
/// Bộ nhớ đệm trung tâm trên nền IMemoryCache có theo dõi danh sách khóa để hủy cache tức thời theo prefix
/// khi cập nhật Quyền (Role/Permission/UserPermission), SiteSettings hoặc DynamicMenu/Banner.
/// </summary>
public sealed class MemoryAppCacheService : IAppCacheService
{
    private static readonly TimeSpan DefaultTtl = TimeSpan.FromMinutes(30);

    private readonly IMemoryCache _cache;
    private readonly ConcurrentDictionary<string, byte> _keys = new(StringComparer.Ordinal);

    public MemoryAppCacheService(IMemoryCache cache)
    {
        _cache = cache;
    }

    public T GetOrCreate<T>(string key, Func<T> factory, TimeSpan? absoluteExpiration = null)
    {
        if (_cache.TryGetValue(key, out T? existing) && existing is not null)
        {
            return existing;
        }

        T created = factory();
        SetEntry(key, created, absoluteExpiration ?? DefaultTtl);
        return created;
    }

    public async Task<T> GetOrCreateAsync<T>(
        string key,
        Func<CancellationToken, Task<T>> factory,
        TimeSpan? absoluteExpiration = null,
        CancellationToken cancellationToken = default)
    {
        if (_cache.TryGetValue(key, out T? existing) && existing is not null)
        {
            return existing;
        }

        T created = await factory(cancellationToken);
        SetEntry(key, created, absoluteExpiration ?? DefaultTtl);
        return created;
    }

    public void Remove(string key)
    {
        _cache.Remove(key);
        _keys.TryRemove(key, out _);
    }

    public void RemoveByPrefix(string prefix)
    {
        if (string.IsNullOrEmpty(prefix)) return;

        foreach (var key in _keys.Keys)
        {
            if (key.StartsWith(prefix, StringComparison.Ordinal))
            {
                _cache.Remove(key);
                _keys.TryRemove(key, out _);
            }
        }
    }

    public void InvalidatePermissions(int? userId = null)
    {
        if (userId.HasValue)
        {
            Remove(AppCacheKeys.UserPermissions(userId.Value));
            return;
        }

        RemoveByPrefix(AppCacheKeys.PermissionsPrefix);
    }

    public void InvalidateSiteSettings()
    {
        Remove(AppCacheKeys.SiteSettingsKey);
    }

    public void InvalidateMenus()
    {
        RemoveByPrefix(AppCacheKeys.DynamicMenuPrefix);
        RemoveByPrefix(AppCacheKeys.DynamicBannerPrefix);
    }

    private void SetEntry<T>(string key, T value, TimeSpan ttl)
    {
        var options = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = ttl
        };
        options.RegisterPostEvictionCallback((evictedKey, _, _, _) =>
        {
            if (evictedKey is string k)
            {
                _keys.TryRemove(k, out _);
            }
        });

        _keys[key] = 0;
        _cache.Set(key, value, options);
    }
}

