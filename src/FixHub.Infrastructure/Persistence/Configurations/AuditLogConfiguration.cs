using FixHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FixHub.Infrastructure.Persistence.Configurations;

public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("audit_logs");

        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id).HasColumnName("id");

        builder.Property(a => a.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .HasDefaultValueSql("NOW()")
            .IsRequired();

        builder.Property(a => a.ActorUserId).HasColumnName("actor_user_id");
        builder.Property(a => a.Action).HasColumnName("action").HasMaxLength(200).IsRequired();
        builder.Property(a => a.EntityType).HasColumnName("entity_type").HasMaxLength(100);
        builder.Property(a => a.EntityId).HasColumnName("entity_id");
        builder.Property(a => a.MetadataJson).HasColumnName("metadata_json").HasColumnType("jsonb");
        builder.Property(a => a.CorrelationId).HasColumnName("correlation_id").HasMaxLength(64);

        builder.HasIndex(a => a.CreatedAtUtc);
        builder.HasIndex(a => a.Action);
        builder.HasIndex(a => a.CorrelationId);
    }
}
