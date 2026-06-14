using Microsoft.EntityFrameworkCore;
using WMSPro.Application.Abstractions.Repositories;
using WMSPro.Domain.Entities;
using WMSPro.Infrastructure.Persistence;

namespace WMSPro.Infrastructure.Repositories;

public class WarehouseRepository : IWarehouseRepository
{
    private readonly ApplicationDbContext _context;
    public WarehouseRepository(ApplicationDbContext context) => _context = context;

    public async Task<Warehouse?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        await _context.Warehouses.FirstOrDefaultAsync(w => w.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Warehouse>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await _context.Warehouses.OrderBy(w => w.Name).ToListAsync(cancellationToken);

    public async Task<bool> ExistsByNameAsync(string name, CancellationToken cancellationToken = default) =>
        await _context.Warehouses.AnyAsync(w => w.Name == name, cancellationToken);

    public async Task<bool> ExistsByNameExceptAsync(string name, Guid excludeId, CancellationToken cancellationToken = default) =>
        await _context.Warehouses.AnyAsync(w => w.Name == name && w.Id != excludeId, cancellationToken);

    public async Task AddAsync(Warehouse warehouse, CancellationToken cancellationToken = default) =>
        await _context.Warehouses.AddAsync(warehouse, cancellationToken);

    public void Update(Warehouse warehouse) => _context.Warehouses.Update(warehouse);

    public void Delete(Warehouse warehouse) => _context.Warehouses.Remove(warehouse);
}
