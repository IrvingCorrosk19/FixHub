using FixHub.Application.Common.Models;
using FixHub.Application.Features.Admin;
using MediatR;
using Microsoft.Extensions.Caching.Memory;

namespace FixHub.Application.Common.Behaviors;

/// <summary>
/// FASE 9: Cache en memoria del dashboard operativo (45 segundos).
/// </summary>
public sealed class DashboardCachingBehavior(IMemoryCache cache)
    : IPipelineBehavior<GetOpsDashboardQuery, Result<OpsDashboardDto>>
{
    private const string CacheKey = "ops-dashboard";
    private static readonly TimeSpan CacheDuration = TimeSpan.FromSeconds(45);

    public async Task<Result<OpsDashboardDto>> Handle(
        GetOpsDashboardQuery request,
        RequestHandlerDelegate<Result<OpsDashboardDto>> next,
        CancellationToken cancellationToken)
    {
        if (cache.TryGetValue(CacheKey, out Result<OpsDashboardDto>? cached) && cached is not null)
            return cached;

        var result = await next();
        if (result.IsSuccess && result.Value is not null)
            cache.Set(CacheKey, result, CacheDuration);

        return result;
    }
}
