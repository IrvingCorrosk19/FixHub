using FixHub.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace FixHub.IntegrationTests;

/// <summary>
/// WebApplicationFactory que usa una connection string inyectada (ej. Testcontainers) y aplica migraciones al arrancar.
/// </summary>
public class FixHubApiFactory : WebApplicationFactory<Program>
{
    private readonly string _connectionString;

    public FixHubApiFactory(string connectionString)
    {
        _connectionString = connectionString;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:DefaultConnection", _connectionString);
        builder.UseSetting("JwtSettings:SecretKey", "integration-tests-secret-key-min-32-chars!!");
        builder.UseSetting("JwtSettings:Issuer", "FixHub.API");
        builder.UseSetting("JwtSettings:Audience", "FixHub.Clients");
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        var host = base.CreateHost(builder);
        using (var scope = host.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Database.Migrate();
        }
        return host;
    }
}
