using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WMSPro.Domain.Entities;

namespace WMSPro.Infrastructure.Persistence.Configurations;

public class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Name)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(p => p.SKU)
            .IsRequired()
            .HasMaxLength(50);

        // Ensure SKU is unique
        builder.HasIndex(p => p.SKU)
            .IsUnique();

        builder.Property(p => p.Category)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(p => p.Price)
            .IsRequired()
            .HasColumnType("decimal(18,2)");

        builder.Property(p => p.Quantity)
            .IsRequired()
            .HasDefaultValue(0);

        // Ignore computed property
        builder.Ignore(p => p.IsLowStock);
    }
}
