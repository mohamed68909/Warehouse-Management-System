using Microsoft.AspNetCore.Identity;
using WMSPro.Domain.Enums;

namespace WMSPro.Infrastructure.Persistence.Seeding;

public static class DatabaseSeeder
{
    public static async Task SeedRolesAndAdminAsync(
        RoleManager<IdentityRole> roleManager,
        UserManager<ApplicationUser> userManager)
    {
        // Seed Roles
        var roles = new[] { UserRole.Manager.ToString(), UserRole.WarehouseEmployee.ToString() };

        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
            }
        }

        // Seed Default Admin (Manager)
        var adminEmail = "admin@wms.com";
        if (await userManager.FindByEmailAsync(adminEmail) == null)
        {
            var adminUser = new ApplicationUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                FullName = "Admin User",
                EmailConfirmed = true,
                IsActive = true
            };

            var result = await userManager.CreateAsync(adminUser, "Admin123!");
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(adminUser, UserRole.Manager.ToString());
            }
        }
    }

    public static async Task SeedInitialDataAsync(ApplicationDbContext dbContext)
    {
        if (dbContext.Products.Any() || dbContext.Warehouses.Any())
            return; // Already seeded

        // Seed Warehouses
        var mainWarehouse = WMSPro.Domain.Entities.Warehouse.Create("Main Distribution Center", 10000, "123 Logistics Way", "John Doe", "555-0100", "john@wms.com").Value;
        var secondaryWarehouse = WMSPro.Domain.Entities.Warehouse.Create("East Coast Hub", 5000, "456 East Ave", "Jane Smith", "555-0200", "jane@wms.com").Value;

        dbContext.Warehouses.AddRange(mainWarehouse, secondaryWarehouse);

        // Seed Products (initial quantity reflects what we will stock in)
        var p1 = WMSPro.Domain.Entities.Product.Create("Dell XPS 15", "LAP-DELL-XPS", "Electronics", 1500m, 50).Value;
        var p2 = WMSPro.Domain.Entities.Product.Create("MacBook Pro M3", "LAP-MAC-M3", "Electronics", 2000m, 30).Value;
        var p3 = WMSPro.Domain.Entities.Product.Create("Logitech MX Master 3", "ACC-LOG-MX", "Accessories", 100m, 200).Value;
        var p4 = WMSPro.Domain.Entities.Product.Create("Ergonomic Office Chair", "FUR-CHAIR-01", "Furniture", 250m, 45).Value;

        dbContext.Products.AddRange(p1, p2, p3, p4);

        // Seed Transactions (Stock In)
        var tx1 = WMSPro.Domain.Entities.InventoryTransaction.CreateStockIn(p1.Id, mainWarehouse.Id, 50, "INIT-01", DateTime.UtcNow).Value;
        var tx2 = WMSPro.Domain.Entities.InventoryTransaction.CreateStockIn(p2.Id, mainWarehouse.Id, 30, "INIT-02", DateTime.UtcNow).Value;
        var tx3 = WMSPro.Domain.Entities.InventoryTransaction.CreateStockIn(p3.Id, secondaryWarehouse.Id, 200, "INIT-03", DateTime.UtcNow).Value;
        var tx4 = WMSPro.Domain.Entities.InventoryTransaction.CreateStockIn(p4.Id, secondaryWarehouse.Id, 45, "INIT-04", DateTime.UtcNow).Value;

        dbContext.InventoryTransactions.AddRange(tx1, tx2, tx3, tx4);

        // ✅ Seed initial WarehouseStock records aligning with the transactions
        var ws1 = WMSPro.Domain.Entities.WarehouseStock.Create(mainWarehouse.Id, p1.Id, 50).Value;
        var ws2 = WMSPro.Domain.Entities.WarehouseStock.Create(mainWarehouse.Id, p2.Id, 30).Value;
        var ws3 = WMSPro.Domain.Entities.WarehouseStock.Create(secondaryWarehouse.Id, p3.Id, 200).Value;
        var ws4 = WMSPro.Domain.Entities.WarehouseStock.Create(secondaryWarehouse.Id, p4.Id, 45).Value;
        dbContext.Set<WMSPro.Domain.Entities.WarehouseStock>().AddRange(ws1, ws2, ws3, ws4);

        // ✅ Synchronize warehouse CurrentStock with initial transactions
        // This ensures Warehouse.CurrentStock reflects the real stocked quantities
        mainWarehouse.AddStock(50);       // p1 → Main
        mainWarehouse.AddStock(30);       // p2 → Main
        secondaryWarehouse.AddStock(200); // p3 → Secondary
        secondaryWarehouse.AddStock(45);  // p4 → Secondary

        // Seed some orders (not completed, so no stock movement yet)
        var order = WMSPro.Domain.Entities.Order.Create("PO-1001", WMSPro.Domain.Enums.OrderType.Purchase, "Tech Suppliers Inc", mainWarehouse.Id).Value;
        order.AddOrderItem(p1.Id, p1.Name, 10, 1400m);
        order.AddOrderItem(p2.Id, p2.Name, 5, 1900m);
        dbContext.Orders.Add(order);

        var salesOrder = WMSPro.Domain.Entities.Order.Create("SO-2001", WMSPro.Domain.Enums.OrderType.Sales, "Global Tech Retailer", secondaryWarehouse.Id).Value;
        salesOrder.AddOrderItem(p3.Id, p3.Name, 20, 120m);
        dbContext.Orders.Add(salesOrder);

        await dbContext.SaveChangesAsync();
    }
}
