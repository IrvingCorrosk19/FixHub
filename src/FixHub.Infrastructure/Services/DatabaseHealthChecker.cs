using FixHub.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FixHub.Infrastructure.Services;

/// <summary>
/// FASE 9: Verifica conectividad a la base de datos para health checks.
/// </summary>
public interface IDatabaseHealthChecker
{
    Task<bool> CanConnectAsync(CancellationToken ct = default);
}

public class DatabaseHealthChecker : IDatabaseHealthChecker
{
    private readonly AppDbContext _db;

    public DatabaseHealthChecker(AppDbContext db)
    {
        _db = db;
    }

    public async Task<bool> CanConnectAsync(CancellationToken ct = default)
    {
        try
        {
            return await _db.Database.CanConnectAsync(ct);
        }
        catch
        {
            return false;
        }
    }
}
