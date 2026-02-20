using FixHub.Domain.Entities;
using FixHub.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FixHub.Infrastructure.Persistence.Configurations;

public class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.ToTable("notifications");

        builder.HasKey(n => n.Id);
        builder.Property(n => n.Id).HasColumnName("id");

        builder.Property(n => n.UserId).HasColumnName("user_id").IsRequired();
        builder.Property(n => n.JobId).HasColumnName("job_id");
        builder.Property(n => n.Type).HasColumnName("type").IsRequired();
        builder.Property(n => n.Message).HasColumnName("message").HasMaxLength(500).IsRequired();
        builder.Property(n => n.IsRead).HasColumnName("is_read").HasDefaultValue(false).IsRequired();
        builder.Property(n => n.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("NOW()").IsRequired();

        builder.HasIndex(n => new { n.UserId, n.IsRead, n.CreatedAt });

        builder.HasOne(n => n.User)
            .WithMany()
            .HasForeignKey(n => n.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(n => n.Job)
            .WithMany()
            .HasForeignKey(n => n.JobId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
