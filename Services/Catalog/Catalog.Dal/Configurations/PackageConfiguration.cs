namespace Catalog.Dal.Configurations;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Catalog.Domain.Entities;

public class PackageConfiguration : IEntityTypeConfiguration<Package>
{
    public void Configure(EntityTypeBuilder<Package> builder)
    {
        builder.ToTable("Packages", "dbo", t =>
        {
            t.HasCheckConstraint("CK_Packages_Price", "[Price] > 0");
        });

        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).UseIdentityColumn();

        builder.Property(p => p.Name)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(p => p.Description)
            .HasMaxLength(1000)
            .IsRequired(false);

        builder.Property(p => p.Price)
            .HasColumnType("decimal(18,2)")
            .IsRequired();

        builder.Property(p => p.CreatedAt)
            .HasPrecision(3)
            .HasDefaultValueSql("SYSUTCDATETIME()")
            .IsRequired();

        builder.Property(p => p.CreatedBy)
            .HasMaxLength(100)
            .HasDefaultValue("system")
            .IsRequired();

        builder.Property(p => p.UpdatedAt)
            .HasPrecision(3)
            .IsRequired(false);

        builder.Property(p => p.UpdatedBy)
            .HasMaxLength(100)
            .IsRequired(false);

        builder.Property(p => p.IsDeleted)
            .HasDefaultValue(false)
            .IsRequired();

        builder.Property(p => p.RowVersion)
            .IsRowVersion();

        builder.HasIndex(p => p.Name)
            .IsUnique()
            .HasDatabaseName("UQ_Packages_Name");

        builder.HasQueryFilter(p => !p.IsDeleted);

        builder.HasMany(p => p.PackageServices)
            .WithOne(ps => ps.Package)
            .HasForeignKey(ps => ps.PackageId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(p => p.PackageServices)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
