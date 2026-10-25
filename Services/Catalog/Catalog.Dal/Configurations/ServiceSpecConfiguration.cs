namespace Catalog.Dal.Configurations;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Catalog.Domain.Entities;

public class ServiceSpecConfiguration : IEntityTypeConfiguration<ServiceSpec>
{
    public void Configure(EntityTypeBuilder<ServiceSpec> builder)
    {
        builder.ToTable("ServiceSpecs", "dbo", t =>
        {
            t.HasCheckConstraint("CK_ServiceSpecs_WarrantyMonths", "[WarrantyMonths] >= 0");
        });

        // 1:1 спільний первинний ключ (Shared Primary Key)
        builder.HasKey(sp => sp.ServiceId);
        builder.Property(sp => sp.ServiceId).ValueGeneratedNever();

        builder.Property(sp => sp.Materials)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(sp => sp.WarrantyMonths)
            .HasDefaultValue(0)
            .IsRequired();

        builder.Property(sp => sp.Description)
            .IsRequired()
            .HasMaxLength(2000);

        builder.HasOne(sp => sp.Service)
            .WithOne(s => s.Spec)
            .HasForeignKey<ServiceSpec>(sp => sp.ServiceId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
