using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WMSPro.Domain.Entities;

namespace WMSPro.Infrastructure.Persistence.Configurations;

public class PickingWaveConfiguration : IEntityTypeConfiguration<PickingWave>
{
    public void Configure(EntityTypeBuilder<PickingWave> builder)
    {
        builder.HasKey(w => w.Id);

        builder.Property(w => w.WaveNumber)
            .IsRequired()
            .HasMaxLength(50);

        builder.HasIndex(w => w.WaveNumber)
            .IsUnique();

        builder.Property(w => w.Status)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(w => w.AssignedPicker)
            .HasMaxLength(150);

        builder.HasOne(w => w.Warehouse)
            .WithMany()
            .HasForeignKey(w => w.WarehouseId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(w => w.Items)
            .WithOne()
            .HasForeignKey(i => i.PickingWaveId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class PickItemConfiguration : IEntityTypeConfiguration<PickItem>
{
    public void Configure(EntityTypeBuilder<PickItem> builder)
    {
        builder.HasKey(pi => pi.Id);

        builder.Property(pi => pi.OrderNumber)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(pi => pi.ProductName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(pi => pi.ProductSku)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(pi => pi.LocationCode)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(pi => pi.Zone)
            .HasMaxLength(100);

        builder.Property(pi => pi.Aisle)
            .HasMaxLength(20);

        builder.Property(pi => pi.Rack)
            .HasMaxLength(20);

        builder.Property(pi => pi.BinNumber)
            .HasMaxLength(20);
    }
}
