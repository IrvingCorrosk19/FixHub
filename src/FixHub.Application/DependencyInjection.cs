using FixHub.Application.Common.Behaviors;
using FixHub.Application.Common.Models;
using FixHub.Application.Features.Admin;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace FixHub.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = typeof(DependencyInjection).Assembly;

        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(assembly);
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(AuditBehavior<,>));
            cfg.AddBehavior(typeof(IPipelineBehavior<GetOpsDashboardQuery, Result<OpsDashboardDto>>), typeof(DashboardCachingBehavior));
        });

        services.AddValidatorsFromAssembly(assembly);

        return services;
    }
}
