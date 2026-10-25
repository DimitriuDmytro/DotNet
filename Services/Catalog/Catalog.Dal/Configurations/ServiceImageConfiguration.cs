namespace Catalog.Dal.Configurations;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Catalog.Domain.Entities;

public class ServiceImageConfiguration : IEntityTypeConfiguration<ServiceImage>
{
    public void Configure(EntityTypeBuilder<ServiceImage> builder)
    {
        builder.ToTable("ServiceImages", "dbo");

        builder.HasKey(i => i.Id);
        builder.Property(i => i.Id).UseIdentityColumn();

        builder.Property(i => i.ServiceId)
            .IsRequired();

        builder.Property(i => i.Url)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(i => i.IsMain)
            .HasDefaultValue(false)
            .IsRequired();

        builder.HasIndex(i => new { i.ServiceId, i.IsMain })
            .HasDatabaseName("IX_ServiceImages_ServiceId_IsMain");

        // Унікальний фільтрований індекс: щонайбільше одне головне фото на послугу
        builder.HasIndex(i => i.ServiceId)
            .IsUnique()
            .HasFilter("[IsMain] = 1")
            .HasDatabaseName("UX_ServiceImages_ServiceId_MainOnly");

        builder.HasOne(i => i.Service)
            .WithMany(s => s.Images)
            .HasForeignKey(i => i.ServiceId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
