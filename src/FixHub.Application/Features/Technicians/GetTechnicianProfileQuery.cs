using FixHub.Application.Common.Interfaces;
using FixHub.Application.Common.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FixHub.Application.Features.Technicians;

// ─── Query ────────────────────────────────────────────────────────────────────
public record GetTechnicianProfileQuery(Guid TechnicianId) : IRequest<Result<TechnicianProfileDto>>;

// ─── Response ─────────────────────────────────────────────────────────────────
public record TechnicianProfileDto(
    Guid UserId,
    string FullName,
    string Email,
    string? Phone,
    string? Bio,
    int ServiceRadiusKm,
    bool IsVerified,
    decimal AvgRating,
    int CompletedJobs,
    decimal CancelRate,
    string Status
);

// ─── Handler ──────────────────────────────────────────────────────────────────
public class GetTechnicianProfileQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetTechnicianProfileQuery, Result<TechnicianProfileDto>>
{
    public async Task<Result<TechnicianProfileDto>> Handle(
        GetTechnicianProfileQuery req, CancellationToken ct)
    {
        var profile = await db.TechnicianProfiles
            .Include(tp => tp.User)
            .FirstOrDefaultAsync(tp => tp.UserId == req.TechnicianId, ct);

        if (profile is null)
            return Result<TechnicianProfileDto>.Failure(
                "Technician profile not found.", "PROFILE_NOT_FOUND");

        return Result<TechnicianProfileDto>.Success(new TechnicianProfileDto(
            profile.UserId,
            profile.User.FullName,
            profile.User.Email,
            profile.User.Phone,
            profile.Bio,
            profile.ServiceRadiusKm,
            profile.IsVerified,
            profile.AvgRating,
            profile.CompletedJobs,
            profile.CancelRate,
            profile.Status.ToString()
        ));
    }
}
