using FixHub.API.Middleware;
using FixHub.Application.Common.Interfaces;

namespace FixHub.API.Services;

/// <summary>
/// FASE 5.3: Provee el CorrelationId desde el HttpContext (establecido por CorrelationIdMiddleware).
/// </summary>
public class CorrelationIdAccessor(IHttpContextAccessor httpContextAccessor) : ICorrelationIdAccessor
{
    public string? GetCorrelationId() =>
        httpContextAccessor.HttpContext?.Items[CorrelationIdMiddleware.CorrelationIdItemKey]?.ToString();
}
