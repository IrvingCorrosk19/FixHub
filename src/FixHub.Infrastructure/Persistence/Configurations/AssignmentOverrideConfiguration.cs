using FixHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FixHub.Infrastructure.Persistence.Configurations;

public class AssignmentOverrideConfiguration : IEntityTypeConfiguration<AssignmentOverride>
{
    public void Configure(EntityTypeBuilder<AssignmentOverride> builder)
    {
        builder.ToTable("assignment_overrides");

        builder.HasKey(o => o.Id);
        builder.Property(o => o.Id).HasColumnName("id");

        builder.Property(o => o.JobId).HasColumnName("job_id").IsRequired();
        builder.Property(o => o.FromTechnicianId).HasColumnName("from_technician_id");
        builder.Property(o => o.ToTechnicianId).HasColumnName("to_technician_id").IsRequired();
        builder.Property(o => o.Reason).HasColumnName("reason").HasMaxLength(200).IsRequired();
        builder.Property(o => o.ReasonDetail).HasColumnName("reason_detail").HasMaxLength(1000);
        builder.Property(o => o.AdminUserId).HasColumnName("admin_user_id").IsRequired();
        builder.Property(o => o.CreatedAtUtc).HasColumnName("created_at_utc").HasDefaultValueSql("NOW()");

        builder.HasIndex(o => o.JobId);
        builder.HasIndex(o => o.CreatedAtUtc);

        builder.HasOne(o => o.Job)
            .WithMany()
            .HasForeignKey(o => o.JobId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
