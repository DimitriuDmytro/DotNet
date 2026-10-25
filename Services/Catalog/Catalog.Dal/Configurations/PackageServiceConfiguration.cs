namespace Catalog.Dal.Configurations;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Catalog.Domain.Entities;

public class PackageServiceConfiguration : IEntityTypeConfiguration<PackageService>
{
    public void Configure(EntityTypeBuilder<PackageService> builder)
    {
        builder.ToTable("PackageServices", "dbo", t =>
        {
            t.HasCheckConstraint("CK_PackageServices_SortOrder", "[SortOrder] > 0");
        });

        // Складений первинний ключ M:N
        builder.HasKey(ps => new { ps.PackageId, ps.ServiceId });

        builder.Property(ps => ps.SortOrder)
            .HasDefaultValue(1)
            .IsRequired();

        builder.Property(ps => ps.AddedAt)
            .HasPrecision(3)
            .HasDefaultValueSql("SYSUTCDATETIME()")
            .IsRequired();

        builder.HasOne(ps => ps.Package)
            .WithMany(p => p.PackageServices)
            .HasForeignKey(ps => ps.PackageId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(ps => ps.Service)
            .WithMany(s => s.PackageServices)
            .HasForeignKey(ps => ps.ServiceId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
