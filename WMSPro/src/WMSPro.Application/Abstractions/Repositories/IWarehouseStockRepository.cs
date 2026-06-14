using WMSPro.Domain.Entities;

namespace WMSPro.Application.Abstractions.Repositories;

public interface IWarehouseStockRepository
{
    Task<WarehouseStock?> GetByWarehouseAndProductAsync(Guid warehouseId, Guid productId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<WarehouseStock>> GetAllWithDetailsAsync(CancellationToken cancellationToken = default);
    Task AddAsync(WarehouseStock warehouseStock, CancellationToken cancellationToken = default);
    void Update(WarehouseStock warehouseStock);
}
