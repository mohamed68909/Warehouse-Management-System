using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using WMSPro.Domain.Entities;

namespace WMSPro.Infrastructure.Persistence;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options) { }

    // Domain Entities
    public DbSet<Warehouse> Warehouses => Set<Warehouse>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<InventoryTransaction> InventoryTransactions => Set<InventoryTransaction>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<WarehouseStock> WarehouseStocks => Set<WarehouseStock>();
    public DbSet<StorageLocation> StorageLocations => Set<StorageLocation>();
    public DbSet<PickingWave> PickingWaves => Set<PickingWave>();
    public DbSet<PickItem> PickItems => Set<PickItem>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Apply all entity configurations from this assembly
        builder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
    }
}
