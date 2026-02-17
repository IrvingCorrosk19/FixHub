using FixHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FixHub.Infrastructure.Persistence.Configurations;

public class JobAssignmentConfiguration : IEntityTypeConfiguration<JobAssignment>
{
    public void Configure(EntityTypeBuilder<JobAssignment> builder)
    {
        builder.ToTable("job_assignments");

        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id).HasColumnName("id");

        builder.Property(a => a.JobId).HasColumnName("job_id").IsRequired();
        builder.Property(a => a.ProposalId).HasColumnName("proposal_id").IsRequired();

        builder.Property(a => a.AcceptedAt)
            .HasColumnName("accepted_at")
            .HasDefaultValueSql("NOW()");

        builder.Property(a => a.StartedAt).HasColumnName("started_at");
        builder.Property(a => a.CompletedAt).HasColumnName("completed_at");

        // Un job solo puede tener UNA asignación activa
        builder.HasIndex(a => a.JobId).IsUnique();
        builder.HasIndex(a => a.ProposalId).IsUnique();

        // FK to Proposal
        builder.HasOne(a => a.Proposal)
            .WithOne(p => p.Assignment)
            .HasForeignKey<JobAssignment>(a => a.ProposalId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
