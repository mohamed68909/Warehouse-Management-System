using Microsoft.EntityFrameworkCore;
using WMSPro.Application.Abstractions.Repositories;
using WMSPro.Domain.Entities;
using WMSPro.Infrastructure.Persistence;

namespace WMSPro.Infrastructure.Repositories;

public class StorageLocationRepository : IStorageLocationRepository
{
    private readonly ApplicationDbContext _context;

    public StorageLocationRepository(ApplicationDbContext context) => _context = context;

    public async Task<StorageLocation?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        await _context.StorageLocations
            .Include(sl => sl.Warehouse)
            .Include(sl => sl.Product)
            .FirstOrDefaultAsync(sl => sl.Id == id, cancellationToken);

    public async Task<StorageLocation?> GetByCodeAsync(Guid warehouseId, string code, CancellationToken cancellationToken = default) =>
        await _context.StorageLocations
            .Include(sl => sl.Warehouse)
            .Include(sl => sl.Product)
            .FirstOrDefaultAsync(sl => sl.WarehouseId == warehouseId && sl.LocationCode == code.Trim().ToUpper(), cancellationToken);

    public async Task<IReadOnlyList<StorageLocation>> GetByWarehouseAsync(Guid warehouseId, string? zone = null, CancellationToken cancellationToken = default)
    {
        var query = _context.StorageLocations
            .Include(sl => sl.Product)
            .Where(sl => sl.WarehouseId == warehouseId);

        if (!string.IsNullOrWhiteSpace(zone))
        {
            query = query.Where(sl => sl.Zone == zone);
        }

        return await query
            .OrderBy(sl => sl.Aisle)
            .ThenBy(sl => sl.Rack)
            .ThenByDescending(sl => sl.ShelfLevel)
            .ThenBy(sl => sl.BinNumber)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(StorageLocation location, CancellationToken cancellationToken = default) =>
        await _context.StorageLocations.AddAsync(location, cancellationToken);

    public async Task AddRangeAsync(IEnumerable<StorageLocation> locations, CancellationToken cancellationToken = default) =>
        await _context.StorageLocations.AddRangeAsync(locations, cancellationToken);

    public void Update(StorageLocation location) => _context.StorageLocations.Update(location);

    public void Delete(StorageLocation location) => _context.StorageLocations.Remove(location);
}
