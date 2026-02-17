using FixHub.Domain.Entities;
using FixHub.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FixHub.Infrastructure.Persistence.Configurations;

public class ProposalConfiguration : IEntityTypeConfiguration<Proposal>
{
    public void Configure(EntityTypeBuilder<Proposal> builder)
    {
        builder.ToTable("proposals");

        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).HasColumnName("id");

        builder.Property(p => p.JobId).HasColumnName("job_id").IsRequired();
        builder.Property(p => p.TechnicianId).HasColumnName("technician_id").IsRequired();

        builder.Property(p => p.Price)
            .HasColumnName("price")
            .HasPrecision(10, 2)
            .IsRequired();

        builder.Property(p => p.Message)
            .HasColumnName("message")
            .HasMaxLength(1000);

        builder.Property(p => p.Status)
            .HasColumnName("status")
            .HasDefaultValue(ProposalStatus.Pending)
            .IsRequired();

        builder.Property(p => p.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("NOW()");

        // Un técnico solo puede enviar UNA propuesta por trabajo
        builder.HasIndex(p => new { p.JobId, p.TechnicianId }).IsUnique();

        // Indexes
        builder.HasIndex(p => p.JobId);
        builder.HasIndex(p => p.TechnicianId);
        builder.HasIndex(p => p.Status);
    }
}
