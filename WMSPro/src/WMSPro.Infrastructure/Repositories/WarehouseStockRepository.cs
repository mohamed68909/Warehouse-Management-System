using Microsoft.EntityFrameworkCore;
using WMSPro.Application.Abstractions.Repositories;
using WMSPro.Domain.Entities;
using WMSPro.Infrastructure.Persistence;

namespace WMSPro.Infrastructure.Repositories;

public class WarehouseStockRepository : IWarehouseStockRepository
{
    private readonly ApplicationDbContext _context;

    public WarehouseStockRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<WarehouseStock?> GetByWarehouseAndProductAsync(
        Guid warehouseId,
        Guid productId,
        CancellationToken cancellationToken = default)
    {
        return await _context.WarehouseStocks
            .FirstOrDefaultAsync(ws => ws.WarehouseId == warehouseId && ws.ProductId == productId, cancellationToken);
    }

    public async Task<IReadOnlyList<WarehouseStock>> GetAllWithDetailsAsync(
        CancellationToken cancellationToken = default)
    {
        return await _context.WarehouseStocks
            .Include(ws => ws.Product)
            .Include(ws => ws.Warehouse)
            .OrderBy(ws => ws.Product!.Name)
            .ThenBy(ws => ws.Warehouse!.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(WarehouseStock warehouseStock, CancellationToken cancellationToken = default)
    {
        await _context.WarehouseStocks.AddAsync(warehouseStock, cancellationToken);
    }

    public void Update(WarehouseStock warehouseStock)
    {
        _context.WarehouseStocks.Update(warehouseStock);
    }
}
