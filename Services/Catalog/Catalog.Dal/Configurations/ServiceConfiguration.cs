namespace Catalog.Dal.Configurations;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Catalog.Domain.Entities;

public class ServiceConfiguration : IEntityTypeConfiguration<Service>
{
    public void Configure(EntityTypeBuilder<Service> builder)
    {
        builder.ToTable("Services", "dbo", t =>
        {
            t.HasCheckConstraint("CK_Services_Price", "[Price] > 0");
            t.HasCheckConstraint("CK_Services_DurationMin", "[DurationMin] > 0");
        });

        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).UseIdentityColumn();

        builder.Property(s => s.CategoryId)
            .IsRequired();

        builder.Property(s => s.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(s => s.Price)
            .HasColumnType("decimal(18,2)")
            .IsRequired();

        builder.Property(s => s.DurationMin)
            .IsRequired();

        builder.Property(s => s.CreatedAt)
            .HasPrecision(3)
            .HasDefaultValueSql("SYSUTCDATETIME()")
            .IsRequired();

        builder.Property(s => s.CreatedBy)
            .HasMaxLength(100)
            .HasDefaultValue("system")
            .IsRequired();

        builder.Property(s => s.UpdatedAt)
            .HasPrecision(3)
            .IsRequired(false);

        builder.Property(s => s.UpdatedBy)
            .HasMaxLength(100)
            .IsRequired(false);

        builder.Property(s => s.IsDeleted)
            .HasDefaultValue(false)
            .IsRequired();

        builder.Property(s => s.RowVersion)
            .IsRowVersion();

        // Унікальний індекс за назвою послуги
        builder.HasIndex(s => s.Name)
            .IsUnique()
            .HasDatabaseName("UQ_Services_Name");

        // Індекс під запит фільтрації за категорією та сортування за ціною
        builder.HasIndex(s => new { s.CategoryId, s.Price })
            .HasDatabaseName("IX_Services_CategoryId_Price");

        // Індекс під швидкий пошук за назвою
        builder.HasIndex(s => s.Name)
            .HasDatabaseName("IX_Services_Name");

        // Глобальний фільтр м'якого видалення (Soft Delete)
        builder.HasQueryFilter(s => !s.IsDeleted);

        // Зв'язок 1:1 зі специфікацією
        builder.HasOne(s => s.Spec)
            .WithOne(sp => sp.Service)
            .HasForeignKey<ServiceSpec>(sp => sp.ServiceId)
            .OnDelete(DeleteBehavior.Cascade);

        // Зв'язок 1:N із зображеннями
        builder.HasMany(s => s.Images)
            .WithOne(i => i.Service)
            .HasForeignKey(i => i.ServiceId)
            .OnDelete(DeleteBehavior.Cascade);

        // Зв'язок M:N із пакетами через PackageService
        builder.HasMany(s => s.PackageServices)
            .WithOne(ps => ps.Service)
            .HasForeignKey(ps => ps.ServiceId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Navigation(s => s.Images)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.Navigation(s => s.PackageServices)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
