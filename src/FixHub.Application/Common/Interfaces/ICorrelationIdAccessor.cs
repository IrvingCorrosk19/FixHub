namespace FixHub.Application.Common.Interfaces;

/// <summary>
/// FASE 5.3: Provee el CorrelationId del request actual (inyectado por el host, ej. API).
/// </summary>
public interface ICorrelationIdAccessor
{
    string? GetCorrelationId();
}
