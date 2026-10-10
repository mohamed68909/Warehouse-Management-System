using Microsoft.EntityFrameworkCore;
using WMSPro.Application.Abstractions.Repositories;
using WMSPro.Domain.Entities;
using WMSPro.Domain.Enums;
using WMSPro.Infrastructure.Persistence;

namespace WMSPro.Infrastructure.Repositories;

public class PickingWaveRepository : IPickingWaveRepository
{
    private readonly ApplicationDbContext _context;

    public PickingWaveRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<PickingWave?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.PickingWaves
            .FirstOrDefaultAsync(w => w.Id == id, cancellationToken);
    }

    public async Task<PickingWave?> GetByIdWithItemsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.PickingWaves
            .Include(w => w.Items)
            .FirstOrDefaultAsync(w => w.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<PickingWave>> GetAllAsync(
        Guid? warehouseId = null,
        string? status = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.PickingWaves
            .Include(w => w.Items)
            .AsNoTracking();

        if (warehouseId.HasValue)
            query = query.Where(w => w.WarehouseId == warehouseId.Value);

        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<PickingWaveStatus>(status, true, out var parsedStatus))
            query = query.Where(w => w.Status == parsedStatus);

        return await query
            .OrderByDescending(w => w.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(PickingWave wave, CancellationToken cancellationToken = default)
    {
        await _context.PickingWaves.AddAsync(wave, cancellationToken);
    }

    public void Update(PickingWave wave)
    {
        _context.PickingWaves.Update(wave);
    }
}
