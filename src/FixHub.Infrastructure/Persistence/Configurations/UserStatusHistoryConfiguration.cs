using FixHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FixHub.Infrastructure.Persistence.Configurations;

public class UserStatusHistoryConfiguration : IEntityTypeConfiguration<UserStatusHistory>
{
    public void Configure(EntityTypeBuilder<UserStatusHistory> builder)
    {
        builder.ToTable("user_status_history");

        builder.HasKey(h => h.Id);
        builder.Property(h => h.Id).HasColumnName("id");

        builder.Property(h => h.UserId).HasColumnName("user_id").IsRequired();
        builder.Property(h => h.PreviousIsActive).HasColumnName("previous_is_active").IsRequired();
        builder.Property(h => h.PreviousIsSuspended).HasColumnName("previous_is_suspended").IsRequired();
        builder.Property(h => h.NewIsActive).HasColumnName("new_is_active").IsRequired();
        builder.Property(h => h.NewIsSuspended).HasColumnName("new_is_suspended").IsRequired();
        builder.Property(h => h.Reason).HasColumnName("reason").HasMaxLength(500);
        builder.Property(h => h.ActorUserId).HasColumnName("actor_user_id").IsRequired();
        builder.Property(h => h.CreatedAtUtc).HasColumnName("created_at_utc").HasDefaultValueSql("NOW()");

        builder.HasIndex(h => h.UserId);
        builder.HasIndex(h => h.CreatedAtUtc);

        builder.HasOne(h => h.User)
            .WithMany()
            .HasForeignKey(h => h.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(h => h.ActorUser)
            .WithMany()
            .HasForeignKey(h => h.ActorUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
