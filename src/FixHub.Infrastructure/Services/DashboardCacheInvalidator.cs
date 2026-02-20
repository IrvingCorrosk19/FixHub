using FixHub.Application.Common.Interfaces;
using Microsoft.Extensions.Caching.Memory;

namespace FixHub.Infrastructure.Services;

/// <summary>
/// FASE 9: Invalida el cache del dashboard operativo.
/// </summary>
public class DashboardCacheInvalidator : IDashboardCacheInvalidator
{
    private const string CacheKey = "ops-dashboard";
    private readonly IMemoryCache _cache;

    public DashboardCacheInvalidator(IMemoryCache cache)
    {
        _cache = cache;
    }

    public void Invalidate()
    {
        _cache.Remove(CacheKey);
    }
}
