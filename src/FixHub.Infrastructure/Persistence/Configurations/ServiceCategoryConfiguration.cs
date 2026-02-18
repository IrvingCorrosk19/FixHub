using FixHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FixHub.Infrastructure.Persistence.Configurations;

public class ServiceCategoryConfiguration : IEntityTypeConfiguration<ServiceCategory>
{
    public void Configure(EntityTypeBuilder<ServiceCategory> builder)
    {
        builder.ToTable("service_categories");

        builder.HasKey(sc => sc.Id);
        builder.Property(sc => sc.Id).HasColumnName("id").UseIdentityByDefaultColumn();

        builder.Property(sc => sc.Name)
            .HasColumnName("name")
            .HasMaxLength(100)
            .IsRequired();

        builder.HasIndex(sc => sc.Name).IsUnique();

        builder.Property(sc => sc.Icon)
            .HasColumnName("icon")
            .HasMaxLength(100);

        builder.Property(sc => sc.IsActive)
            .HasColumnName("is_active")
            .HasDefaultValue(true);

        // Seed data — categorías MVP
        builder.HasData(
            new ServiceCategory { Id = 1, Name = "Plomería", Icon = "wrench", IsActive = true },
            new ServiceCategory { Id = 2, Name = "Electricidad", Icon = "zap", IsActive = true },
            new ServiceCategory { Id = 3, Name = "Handyman", Icon = "tool", IsActive = true },
            new ServiceCategory { Id = 4, Name = "Aire Acondicionado", Icon = "wind", IsActive = true },
            new ServiceCategory { Id = 5, Name = "Pintura", Icon = "paint-roller", IsActive = true },
            new ServiceCategory { Id = 6, Name = "Cerrajería", Icon = "key", IsActive = true }
        );
    }
}
