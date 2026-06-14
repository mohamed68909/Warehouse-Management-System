using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WMSPro.Domain.Entities;

namespace WMSPro.Infrastructure.Persistence.Configurations;

public class WarehouseStockConfiguration : IEntityTypeConfiguration<WarehouseStock>
{
    public void Configure(EntityTypeBuilder<WarehouseStock> builder)
    {
        builder.HasKey(ws => ws.Id);

        // Ensure compound uniqueness of WarehouseId and ProductId
        builder.HasIndex(ws => new { ws.WarehouseId, ws.ProductId })
            .IsUnique();

        builder.Property(ws => ws.Quantity)
            .IsRequired()
            .HasDefaultValue(0);

        builder.HasOne(ws => ws.Warehouse)
            .WithMany()
            .HasForeignKey(ws => ws.WarehouseId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(ws => ws.Product)
            .WithMany()
            .HasForeignKey(ws => ws.ProductId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
