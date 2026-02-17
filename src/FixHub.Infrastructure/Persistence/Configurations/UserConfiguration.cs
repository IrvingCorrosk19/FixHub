using FixHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FixHub.Infrastructure.Persistence.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users");

        builder.HasKey(u => u.Id);
        builder.Property(u => u.Id).HasColumnName("id");

        builder.Property(u => u.Email)
            .HasColumnName("email")
            .HasMaxLength(256)
            .IsRequired();

        builder.HasIndex(u => u.Email).IsUnique();

        builder.Property(u => u.PasswordHash)
            .HasColumnName("password_hash")
            .HasMaxLength(512)
            .IsRequired();

        builder.Property(u => u.FullName)
            .HasColumnName("full_name")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(u => u.Role)
            .HasColumnName("role")
            .IsRequired();

        builder.Property(u => u.Phone)
            .HasColumnName("phone")
            .HasMaxLength(30);

        builder.Property(u => u.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("NOW()");

        builder.Property(u => u.IsActive)
            .HasColumnName("is_active")
            .HasDefaultValue(true);

        // Relations
        builder.HasOne(u => u.TechnicianProfile)
            .WithOne(tp => tp.User)
            .HasForeignKey<TechnicianProfile>(tp => tp.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(u => u.JobsAsCustomer)
            .WithOne(j => j.Customer)
            .HasForeignKey(j => j.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(u => u.Proposals)
            .WithOne(p => p.Technician)
            .HasForeignKey(p => p.TechnicianId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(u => u.ReviewsGiven)
            .WithOne(r => r.Customer)
            .HasForeignKey(r => r.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(u => u.ReviewsReceived)
            .WithOne(r => r.Technician)
            .HasForeignKey(r => r.TechnicianId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
