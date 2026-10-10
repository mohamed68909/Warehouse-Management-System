using WMSPro.Domain.Entities;

namespace WMSPro.Application.Abstractions.Repositories;

public interface IStorageLocationRepository
{
    Task<StorageLocation?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<StorageLocation?> GetByCodeAsync(Guid warehouseId, string code, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<StorageLocation>> GetByWarehouseAsync(Guid warehouseId, string? zone = null, CancellationToken cancellationToken = default);
    Task AddAsync(StorageLocation location, CancellationToken cancellationToken = default);
    Task AddRangeAsync(IEnumerable<StorageLocation> locations, CancellationToken cancellationToken = default);
    void Update(StorageLocation location);
    void Delete(StorageLocation location);
}
