using System.Text.Json;
using FixHub.Application.Common.Interfaces;
using FixHub.Application.Common.Models;
using FixHub.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FixHub.Application.Features.Scoring;

// ─── Command ──────────────────────────────────────────────────────────────────
public record RankTechniciansCommand(Guid JobId) : IRequest<Result<List<TechnicianRankDto>>>;

// ─── Response ─────────────────────────────────────────────────────────────────
public record TechnicianRankDto(
    Guid TechnicianId,
    string FullName,
    decimal Score,
    decimal AvgRating,
    int CompletedJobs,
    decimal CancelRate,
    bool IsVerified,
    ScoringFactorsDto Factors
);

public record ScoringFactorsDto(
    decimal RatingComponent,
    decimal ExperienceComponent,
    decimal CancelPenalty,
    decimal VerifiedBonus
);

// ─── Handler ──────────────────────────────────────────────────────────────────
public class RankTechniciansCommandHandler(IApplicationDbContext db)
    : IRequestHandler<RankTechniciansCommand, Result<List<TechnicianRankDto>>>
{
    // Fórmula v1 (rule-based, sin ML)
    // Score = (AvgRating * 2) + (CompletedJobs * 0.1) - (CancelRate * 5) + (IsVerified ? 5 : 0)
    private static readonly decimal RatingWeight = 2m;
    private static readonly decimal ExperienceWeight = 0.1m;
    private static readonly decimal CancelPenaltyWeight = 5m;
    private static readonly decimal VerifiedBonus = 5m;

    public async Task<Result<List<TechnicianRankDto>>> Handle(
        RankTechniciansCommand req, CancellationToken ct)
    {
        var job = await db.Jobs.FirstOrDefaultAsync(j => j.Id == req.JobId, ct);

        if (job is null)
            return Result<List<TechnicianRankDto>>.Failure("Job not found.", "JOB_NOT_FOUND");

        // Obtener técnicos con propuestas Pending en este job
        var proposals = await db.Proposals
            .Include(p => p.Technician)
            .Where(p => p.JobId == req.JobId)
            .ToListAsync(ct);

        if (proposals.Count == 0)
            return Result<List<TechnicianRankDto>>.Failure(
                "No proposals found for this job.", "NO_PROPOSALS");

        var technicianIds = proposals.Select(p => p.TechnicianId).Distinct().ToList();

        var profiles = await db.TechnicianProfiles
            .Where(tp => technicianIds.Contains(tp.UserId))
            .ToDictionaryAsync(tp => tp.UserId, ct);

        var ranked = new List<TechnicianRankDto>();
        var snapshots = new List<ScoreSnapshot>();

        foreach (var proposal in proposals)
        {
            profiles.TryGetValue(proposal.TechnicianId, out var profile);

            var avgRating = profile?.AvgRating ?? 0m;
            var completedJobs = profile?.CompletedJobs ?? 0;
            var cancelRate = profile?.CancelRate ?? 0m;
            var isVerified = profile?.IsVerified ?? false;

            var ratingComp = avgRating * RatingWeight;
            var expComp = completedJobs * ExperienceWeight;
            var cancelPenalty = cancelRate * CancelPenaltyWeight;
            var verifiedBonus = isVerified ? VerifiedBonus : 0m;

            var score = ratingComp + expComp - cancelPenalty + verifiedBonus;
            score = Math.Max(0m, score); // Mínimo 0

            var factors = new ScoringFactorsDto(ratingComp, expComp, cancelPenalty, verifiedBonus);
            var factorsJson = JsonSerializer.Serialize(factors);

            ranked.Add(new TechnicianRankDto(
                proposal.TechnicianId,
                proposal.Technician.FullName,
                Math.Round(score, 4),
                avgRating,
                completedJobs,
                cancelRate,
                isVerified,
                factors
            ));

            // Guardar ScoreSnapshot para auditoría
            snapshots.Add(new ScoreSnapshot
            {
                Id = Guid.NewGuid(),
                JobId = req.JobId,
                TechnicianId = proposal.TechnicianId,
                Score = Math.Round(score, 4),
                FactorsJson = factorsJson,
                CreatedAt = DateTime.UtcNow
            });
        }

        db.ScoreSnapshots.AddRange(snapshots);
        await db.SaveChangesAsync(ct);

        // Ordenar por score descendente
        var sorted = ranked.OrderByDescending(r => r.Score).ToList();

        return Result<List<TechnicianRankDto>>.Success(sorted);
    }
}
