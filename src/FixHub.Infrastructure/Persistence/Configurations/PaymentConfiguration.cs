using FixHub.Domain.Entities;
using FixHub.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FixHub.Infrastructure.Persistence.Configurations;

public class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> builder)
    {
        builder.ToTable("payments");

        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).HasColumnName("id");

        builder.Property(p => p.JobId).HasColumnName("job_id").IsRequired();

        builder.Property(p => p.Amount)
            .HasColumnName("amount")
            .HasPrecision(10, 2)
            .IsRequired();

        builder.Property(p => p.Currency)
            .HasColumnName("currency")
            .HasMaxLength(3)
            .HasDefaultValue("USD")
            .IsRequired();

        builder.Property(p => p.Status)
            .HasColumnName("status")
            .HasDefaultValue(PaymentStatus.Pending)
            .IsRequired();

        builder.Property(p => p.Provider)
            .HasColumnName("provider")
            .HasMaxLength(50);

        builder.Property(p => p.ProviderRef)
            .HasColumnName("provider_ref")
            .HasMaxLength(200);

        builder.Property(p => p.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("NOW()");

        // 1 payment por job
        builder.HasIndex(p => p.JobId).IsUnique();
        builder.HasIndex(p => p.Status);
    }
}
