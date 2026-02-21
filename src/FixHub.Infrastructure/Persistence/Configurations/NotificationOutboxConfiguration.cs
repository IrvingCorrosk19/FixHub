using FixHub.Domain.Entities;
using FixHub.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FixHub.Infrastructure.Persistence.Configurations;

public class NotificationOutboxConfiguration : IEntityTypeConfiguration<NotificationOutbox>
{
    public void Configure(EntityTypeBuilder<NotificationOutbox> builder)
    {
        builder.ToTable("notification_outbox");

        builder.HasKey(o => o.Id);
        builder.Property(o => o.Id).HasColumnName("id");

        builder.Property(o => o.Channel).HasColumnName("channel").HasMaxLength(50).IsRequired();
        builder.Property(o => o.ToEmail).HasColumnName("to_email").HasMaxLength(256).IsRequired();
        builder.Property(o => o.Subject).HasColumnName("subject").HasMaxLength(300).IsRequired();
        builder.Property(o => o.HtmlBody).HasColumnName("html_body").HasColumnType("text").IsRequired();
        builder.Property(o => o.Status).HasColumnName("status").HasConversion<int>().HasDefaultValue(OutboxStatus.Pending).IsRequired();
        builder.Property(o => o.Attempts).HasColumnName("attempts").HasDefaultValue(0).IsRequired();
        builder.Property(o => o.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("NOW()").IsRequired();
        builder.Property(o => o.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("NOW()").IsRequired();
        builder.Property(o => o.SentAt).HasColumnName("sent_at");
        builder.Property(o => o.NextRetryAt).HasColumnName("next_retry_at");
        builder.Property(o => o.JobId).HasColumnName("job_id");
        builder.Property(o => o.NotificationId).HasColumnName("notification_id");

        builder.HasIndex(o => new { o.Status, o.CreatedAt });
        builder.HasIndex(o => o.JobId);
        builder.HasIndex(o => new { o.NotificationId, o.Channel }).IsUnique();
    }
}
