using FixHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FixHub.Infrastructure.Persistence.Configurations;

public class JobIssueConfiguration : IEntityTypeConfiguration<JobIssue>
{
    public void Configure(EntityTypeBuilder<JobIssue> builder)
    {
        builder.ToTable("job_issues");

        builder.HasKey(i => i.Id);
        builder.Property(i => i.Id).HasColumnName("id");

        builder.Property(i => i.JobId)
            .HasColumnName("job_id")
            .IsRequired();

        builder.Property(i => i.ReportedByUserId)
            .HasColumnName("reported_by_user_id")
            .IsRequired();

        builder.Property(i => i.Reason)
            .HasColumnName("reason")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(i => i.Detail)
            .HasColumnName("detail")
            .HasMaxLength(500);

        builder.Property(i => i.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("NOW()");

        builder.Property(i => i.ResolvedAt)
            .HasColumnName("resolved_at");

        builder.Property(i => i.ResolvedByUserId)
            .HasColumnName("resolved_by_user_id");

        builder.Property(i => i.ResolutionNote)
            .HasColumnName("resolution_note")
            .HasMaxLength(1000);

        // Índices para consultas frecuentes del admin
        builder.HasIndex(i => i.JobId);
        builder.HasIndex(i => i.CreatedAt);
        builder.HasIndex(i => new { i.JobId, i.CreatedAt });

        // FK: Job (cascade — si se borra el job se borran sus incidencias)
        builder.HasOne(i => i.Job)
            .WithMany(j => j.Issues)
            .HasForeignKey(i => i.JobId)
            .OnDelete(DeleteBehavior.Cascade);

        // FK: User (restrict — no borrar user si tiene incidencias)
        builder.HasOne(i => i.ReportedBy)
            .WithMany()
            .HasForeignKey(i => i.ReportedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
