using FixHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FixHub.Infrastructure.Persistence.Configurations;

public class ScoreSnapshotConfiguration : IEntityTypeConfiguration<ScoreSnapshot>
{
    public void Configure(EntityTypeBuilder<ScoreSnapshot> builder)
    {
        builder.ToTable("score_snapshots");

        builder.HasKey(ss => ss.Id);
        builder.Property(ss => ss.Id).HasColumnName("id");

        builder.Property(ss => ss.JobId).HasColumnName("job_id").IsRequired();
        builder.Property(ss => ss.TechnicianId).HasColumnName("technician_id").IsRequired();

        builder.Property(ss => ss.Score)
            .HasColumnName("score")
            .HasPrecision(5, 4)
            .IsRequired();

        builder.Property(ss => ss.FactorsJson)
            .HasColumnName("factors_json")
            .HasColumnType("jsonb")
            .HasDefaultValue("{}");

        builder.Property(ss => ss.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("NOW()");

        // Indexes para queries de scoring
        builder.HasIndex(ss => ss.JobId);
        builder.HasIndex(ss => ss.TechnicianId);
        builder.HasIndex(ss => new { ss.JobId, ss.TechnicianId });
    }
}
