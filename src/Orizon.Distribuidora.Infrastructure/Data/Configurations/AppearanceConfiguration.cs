using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Orizon.Distribuidora.Domain.Entities;
using Orizon.Distribuidora.Infrastructure.Identity;

namespace Orizon.Distribuidora.Infrastructure.Data.Configurations;

public sealed class CompanyAppearanceSettingsConfiguration : IEntityTypeConfiguration<CompanyAppearanceSettings>
{
    public void Configure(EntityTypeBuilder<CompanyAppearanceSettings> builder)
    {
        builder.ToTable("CompanyAppearanceSettings"); builder.HasKey(x => x.Id);
        builder.Property(x => x.Theme).HasMaxLength(40).IsRequired(); builder.Property(x => x.PrimaryColor).HasMaxLength(20).IsRequired();
        builder.Property(x => x.LogoPath).HasMaxLength(400); builder.Property(x => x.FaviconPath).HasMaxLength(400);
        builder.Property(x => x.LoginBackgroundPath).HasMaxLength(400); builder.Property(x => x.LoginTitle).HasMaxLength(120);
        builder.HasOne<Company>().WithOne().HasForeignKey<CompanyAppearanceSettings>(x => x.CompanyId).OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(x => x.CompanyId).IsUnique().HasFilter("\"IsDeleted\" = FALSE"); builder.HasQueryFilter(x => !x.IsDeleted);
    }
}

public sealed class UserAppearanceSettingsConfiguration : IEntityTypeConfiguration<UserAppearanceSettings>
{
    public void Configure(EntityTypeBuilder<UserAppearanceSettings> builder)
    {
        builder.ToTable("UserAppearanceSettings"); builder.HasKey(x => x.Id);
        builder.Property(x => x.Theme).HasMaxLength(40); builder.Property(x => x.PrimaryColor).HasMaxLength(20);
        builder.Property(x => x.Mode).HasMaxLength(12).IsRequired(); builder.Property(x => x.Density).HasMaxLength(16).IsRequired();
        builder.Property(x => x.Sidebar).HasMaxLength(16).IsRequired(); builder.Property(x => x.FontSize).HasMaxLength(12).IsRequired();
        builder.Property(x => x.Radius).HasMaxLength(12).IsRequired(); builder.Property(x => x.Shadow).HasMaxLength(12).IsRequired();
        builder.Property(x => x.Motion).HasMaxLength(12).IsRequired(); builder.Property(x => x.Language).HasMaxLength(10).IsRequired();
        builder.HasOne<Company>().WithMany().HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<ApplicationUser>().WithOne().HasForeignKey<UserAppearanceSettings>(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(x => new { x.CompanyId, x.UserId }).IsUnique().HasFilter("\"IsDeleted\" = FALSE"); builder.HasQueryFilter(x => !x.IsDeleted);
    }
}
