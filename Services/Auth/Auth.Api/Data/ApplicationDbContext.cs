using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Services.Auth.Api.Models;

namespace Services.Auth.Api.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<RefreshToken>(b =>
        {
            b.HasKey(rt => rt.Id);
            b.HasIndex(rt => rt.TokenHash).IsUnique();
            b.HasIndex(rt => rt.UserId);
            b.Property(rt => rt.TokenHash).IsRequired().HasMaxLength(128);
            b.Property(rt => rt.UserId).IsRequired().HasMaxLength(450);
            b.Property(rt => rt.ReplacedByTokenHash).HasMaxLength(128);
            b.Property(rt => rt.CreatedByIp).HasMaxLength(64);
            b.Property(rt => rt.RevokedByIp).HasMaxLength(64);
        });
    }
}
