using FixHub.Domain.Entities;
using FixHub.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FixHub.Infrastructure.Persistence.Configurations;

public class TechnicianProfileConfiguration : IEntityTypeConfiguration<TechnicianProfile>
{
    public void Configure(EntityTypeBuilder<TechnicianProfile> builder)
    {
        builder.ToTable("technician_profiles");

        builder.HasKey(tp => tp.UserId);
        builder.Property(tp => tp.UserId).HasColumnName("user_id");

        builder.Property(tp => tp.Status)
            .HasColumnName("status")
            .HasDefaultValue(TechnicianStatus.Pending)
            .HasConversion<int>();

        builder.Property(tp => tp.Bio)
            .HasColumnName("bio")
            .HasMaxLength(1000);

        builder.Property(tp => tp.ServiceRadiusKm)
            .HasColumnName("service_radius_km")
            .HasDefaultValue(10);

        builder.Property(tp => tp.IsVerified)
            .HasColumnName("is_verified")
            .HasDefaultValue(false);

        builder.Property(tp => tp.DocumentsJson)
            .HasColumnName("documents_json")
            .HasColumnType("jsonb");

        builder.Property(tp => tp.AvgRating)
            .HasColumnName("avg_rating")
            .HasPrecision(3, 2)
            .HasDefaultValue(0m);

        builder.Property(tp => tp.CompletedJobs)
            .HasColumnName("completed_jobs")
            .HasDefaultValue(0);

        builder.Property(tp => tp.CancelRate)
            .HasColumnName("cancel_rate")
            .HasPrecision(5, 4)
            .HasDefaultValue(0m);

        // Indexes
        builder.HasIndex(tp => tp.IsVerified);
        builder.HasIndex(tp => tp.AvgRating);
        builder.HasIndex(tp => tp.Status);

        // Relations with ScoreSnapshots
        builder.HasMany(tp => tp.ScoreSnapshots)
            .WithOne(ss => ss.Technician)
            .HasForeignKey(ss => ss.TechnicianId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
