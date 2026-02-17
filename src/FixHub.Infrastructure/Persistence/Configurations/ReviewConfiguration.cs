using FixHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FixHub.Infrastructure.Persistence.Configurations;

public class ReviewConfiguration : IEntityTypeConfiguration<Review>
{
    public void Configure(EntityTypeBuilder<Review> builder)
    {
        builder.ToTable("reviews");

        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).HasColumnName("id");

        builder.Property(r => r.JobId).HasColumnName("job_id").IsRequired();
        builder.Property(r => r.CustomerId).HasColumnName("customer_id").IsRequired();
        builder.Property(r => r.TechnicianId).HasColumnName("technician_id").IsRequired();

        builder.Property(r => r.Stars)
            .HasColumnName("stars")
            .IsRequired();

        // CHECK constraint: 1 <= stars <= 5
        builder.ToTable(t => t.HasCheckConstraint("ck_reviews_stars", "stars >= 1 AND stars <= 5"));

        builder.Property(r => r.Comment)
            .HasColumnName("comment")
            .HasMaxLength(1000);

        builder.Property(r => r.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("NOW()");

        // 1 review por job (unique)
        builder.HasIndex(r => r.JobId).IsUnique();
        builder.HasIndex(r => r.TechnicianId);
    }
}
