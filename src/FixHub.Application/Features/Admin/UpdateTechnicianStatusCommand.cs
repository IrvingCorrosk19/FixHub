using FixHub.Application.Common.Interfaces;
using FixHub.Application.Common.Models;
using FixHub.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FixHub.Application.Features.Admin;

public record UpdateTechnicianStatusCommand(Guid TechnicianId, TechnicianStatus NewStatus)
    : IRequest<Result<Unit>>;

public class UpdateTechnicianStatusCommandHandler(IApplicationDbContext db)
    : IRequestHandler<UpdateTechnicianStatusCommand, Result<Unit>>
{
    public async Task<Result<Unit>> Handle(
        UpdateTechnicianStatusCommand req, CancellationToken ct)
    {
        var profile = await db.TechnicianProfiles
            .FirstOrDefaultAsync(tp => tp.UserId == req.TechnicianId, ct);

        if (profile is null)
            return Result<Unit>.Failure("Technician profile not found.", "NOT_FOUND");

        profile.Status = req.NewStatus;
        if (req.NewStatus == TechnicianStatus.Approved)
            profile.IsVerified = true;

        await db.SaveChangesAsync(ct);
        return Result<Unit>.Success(Unit.Value);
    }
}
