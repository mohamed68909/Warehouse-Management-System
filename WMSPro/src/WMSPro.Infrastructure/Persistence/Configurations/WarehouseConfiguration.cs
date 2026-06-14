using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WMSPro.Domain.Entities;

namespace WMSPro.Infrastructure.Persistence.Configurations;

public class WarehouseConfiguration : IEntityTypeConfiguration<Warehouse>
{
    public void Configure(EntityTypeBuilder<Warehouse> builder)
    {
        builder.HasKey(w => w.Id);

        builder.Property(w => w.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(w => w.Location)
            .IsRequired()
            .HasMaxLength(250);

        builder.Property(w => w.ManagerName)
            .HasMaxLength(100);

        builder.Property(w => w.Phone)
            .HasMaxLength(20);

        builder.Property(w => w.Email)
            .HasMaxLength(100);

        // CurrentStock is computed and managed through domain methods,
        // but needs to be persisted.
        builder.Property(w => w.CurrentStock)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(w => w.Capacity)
            .IsRequired();
            
        // Ignore computed property
        builder.Ignore(w => w.CapacityUsagePercent);
    }
}
