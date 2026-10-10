using WMSPro.Domain.Entities;

namespace WMSPro.Application.Abstractions.Repositories;

public interface IPickingWaveRepository
{
    Task<PickingWave?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<PickingWave?> GetByIdWithItemsAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PickingWave>> GetAllAsync(Guid? warehouseId = null, string? status = null, CancellationToken cancellationToken = default);
    Task AddAsync(PickingWave wave, CancellationToken cancellationToken = default);
    void Update(PickingWave wave);
}
