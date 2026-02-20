using FixHub.Domain.Entities;
using FixHub.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FixHub.Infrastructure.Persistence.Configurations;

public class JobAlertConfiguration : IEntityTypeConfiguration<JobAlert>
{
    public void Configure(EntityTypeBuilder<JobAlert> builder)
    {
        builder.ToTable("job_alerts");

        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id).HasColumnName("id");

        builder.Property(a => a.JobId).HasColumnName("job_id").IsRequired();
        builder.Property(a => a.Type).HasColumnName("type").HasConversion<int>().IsRequired();
        builder.Property(a => a.Message).HasColumnName("message").HasMaxLength(500).IsRequired();
        builder.Property(a => a.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("NOW()").IsRequired();
        builder.Property(a => a.IsResolved).HasColumnName("is_resolved").HasDefaultValue(false).IsRequired();

        builder.HasIndex(a => a.JobId);
        builder.HasIndex(a => new { a.JobId, a.Type, a.IsResolved });

        builder.HasOne(a => a.Job)
            .WithMany()
            .HasForeignKey(a => a.JobId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
