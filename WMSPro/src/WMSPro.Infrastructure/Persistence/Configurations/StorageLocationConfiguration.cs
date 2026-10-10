using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WMSPro.Domain.Entities;

namespace WMSPro.Infrastructure.Persistence.Configurations;

public class StorageLocationConfiguration : IEntityTypeConfiguration<StorageLocation>
{
    public void Configure(EntityTypeBuilder<StorageLocation> builder)
    {
        builder.HasKey(sl => sl.Id);

        builder.Property(sl => sl.LocationCode)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(sl => sl.Zone)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(sl => sl.Aisle)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(sl => sl.Rack)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(sl => sl.BinNumber)
            .IsRequired()
            .HasMaxLength(20);

        // Compound index for unique location code within a warehouse
        builder.HasIndex(sl => new { sl.WarehouseId, sl.LocationCode })
            .IsUnique();

        builder.HasOne(sl => sl.Warehouse)
            .WithMany()
            .HasForeignKey(sl => sl.WarehouseId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(sl => sl.Product)
            .WithMany()
            .HasForeignKey(sl => sl.ProductId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
