namespace FixHub.Application.Common.Interfaces;

/// <summary>
/// FASE 9: Invalida el cache del dashboard operativo cuando cambian jobs/issues.
/// </summary>
public interface IDashboardCacheInvalidator
{
    void Invalidate();
}
