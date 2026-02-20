using FixHub.Application.Common.Interfaces;
using FixHub.Infrastructure.Persistence;
using Microsoft.Extensions.Caching.Memory;
using FixHub.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FixHub.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // PostgreSQL via Npgsql
        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(
                configuration.GetConnectionString("DefaultConnection"),
                npgsql => npgsql.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName)
            )
        );

        services.AddScoped<IApplicationDbContext>(provider =>
            provider.GetRequiredService<AppDbContext>());

        // Services
        services.AddScoped<IJwtTokenService, JwtTokenService>();
        services.AddSingleton<IPasswordHasher, BcryptPasswordHasher>();
        services.AddScoped<IAuditService, AuditService>();
        services.AddScoped<INotificationService, NotificationService>();
        services.AddScoped<IEmailSender, SendGridEmailSender>();
        services.AddScoped<IEmailOutboxService, EmailOutboxService>();
        services.AddScoped<INotificationEmailComposer, NotificationEmailComposer>();
        services.AddScoped<IDatabaseHealthChecker, DatabaseHealthChecker>();
        services.AddHostedService<OutboxEmailSenderHostedService>();
        services.AddHostedService<JobSlaMonitor>();
        services.AddMemoryCache();
        services.AddSingleton<IDashboardCacheInvalidator, DashboardCacheInvalidator>();

        return services;
    }
}
