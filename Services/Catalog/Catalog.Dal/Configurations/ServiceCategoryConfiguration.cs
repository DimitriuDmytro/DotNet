namespace Catalog.Dal.Configurations;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Catalog.Domain.Entities;

public class ServiceCategoryConfiguration : IEntityTypeConfiguration<ServiceCategory>
{
    public void Configure(EntityTypeBuilder<ServiceCategory> builder)
    {
        builder.ToTable("ServiceCategories", "dbo");

        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).UseIdentityColumn();

        builder.Property(c => c.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(c => c.Description)
            .HasMaxLength(500)
            .IsRequired(false);

        builder.HasIndex(c => c.Name)
            .IsUnique()
            .HasDatabaseName("UQ_ServiceCategories_Name");

        builder.HasMany(c => c.Services)
            .WithOne(s => s.Category)
            .HasForeignKey(s => s.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Navigation(c => c.Services)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        // Критерій 9(а): HasData у конфігурації довідників із фіксованими Id
        builder.HasData(
            new { Id = 1L, Name = "Мийка", Description = "Дбайлива багатофазна детейлінг-мийка кузова, підвіски та дисків." },
            new { Id = 2L, Name = "Полірування", Description = "Відновлювальне, коригувальне та глибоке абразивне полірування кузова і оптики." },
            new { Id = 3L, Name = "Захист", Description = "Нанесення кераміки 9H, рідкого скла, антидощу та захисних плівок PPF." },
            new { Id = 4L, Name = "Салон", Description = "Комплексна хімчистка інтер'єру, озонування та консервація шкіри." }
        );
    }
}
