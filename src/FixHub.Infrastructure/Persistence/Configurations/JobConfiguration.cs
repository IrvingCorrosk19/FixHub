using FixHub.Domain.Entities;
using FixHub.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FixHub.Infrastructure.Persistence.Configurations;

public class JobConfiguration : IEntityTypeConfiguration<Job>
{
    public void Configure(EntityTypeBuilder<Job> builder)
    {
        builder.ToTable("jobs");

        builder.HasKey(j => j.Id);
        builder.Property(j => j.Id).HasColumnName("id");

        builder.Property(j => j.CustomerId).HasColumnName("customer_id").IsRequired();
        builder.Property(j => j.CategoryId).HasColumnName("category_id").IsRequired();

        builder.Property(j => j.Title)
            .HasColumnName("title")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(j => j.Description)
            .HasColumnName("description")
            .HasMaxLength(2000)
            .IsRequired();

        builder.Property(j => j.AddressText)
            .HasColumnName("address_text")
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(j => j.Lat)
            .HasColumnName("lat")
            .HasPrecision(9, 6);

        builder.Property(j => j.Lng)
            .HasColumnName("lng")
            .HasPrecision(9, 6);

        builder.Property(j => j.Status)
            .HasColumnName("status")
            .HasDefaultValue(JobStatus.Open)
            .IsRequired();

        builder.Property(j => j.BudgetMin)
            .HasColumnName("budget_min")
            .HasPrecision(10, 2);

        builder.Property(j => j.BudgetMax)
            .HasColumnName("budget_max")
            .HasPrecision(10, 2);

        builder.Property(j => j.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("NOW()");

        builder.Property(j => j.AssignedAt)
            .HasColumnName("assigned_at");

        builder.Property(j => j.CompletedAt)
            .HasColumnName("completed_at");

        builder.Property(j => j.CancelledAt)
            .HasColumnName("cancelled_at");

        // FASE 14: Concurrencia optimista via xmin (columna de sistema PostgreSQL).
        // No requiere DDL migration — xmin existe en toda tabla PostgreSQL.
#pragma warning disable CS0618 // UseXminAsConcurrencyToken obsoleto en Npgsql 8+; comportamiento idéntico, migrar cuando Npgsql lo elimine
        builder.UseXminAsConcurrencyToken();
#pragma warning restore CS0618

        // Indexes críticos para queries frecuentes
        builder.HasIndex(j => j.Status);
        builder.HasIndex(j => j.CustomerId);
        builder.HasIndex(j => j.CategoryId);
        builder.HasIndex(j => j.CreatedAt);
        builder.HasIndex(j => new { j.Status, j.CategoryId });

        // FK: Category (no cascade, conservar historial)
        builder.HasOne(j => j.Category)
            .WithMany(sc => sc.Jobs)
            .HasForeignKey(j => j.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        // One-to-one relations
        builder.HasOne(j => j.Assignment)
            .WithOne(a => a.Job)
            .HasForeignKey<JobAssignment>(a => a.JobId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(j => j.Review)
            .WithOne(r => r.Job)
            .HasForeignKey<Review>(r => r.JobId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(j => j.Payment)
            .WithOne(p => p.Job)
            .HasForeignKey<Payment>(p => p.JobId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
