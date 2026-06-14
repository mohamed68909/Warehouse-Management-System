using WMSPro.Domain.Entities;
using WMSPro.Domain.Enums;

namespace WMSPro.Application.Abstractions.Repositories;

public interface IInventoryTransactionRepository
{
    Task<IReadOnlyList<InventoryTransaction>> GetAllAsync(
        string? productFilter,
        TransactionType? typeFilter,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<InventoryTransaction>> GetByProductAsync(
        Guid productId,
        CancellationToken cancellationToken = default);

    Task AddAsync(InventoryTransaction transaction, CancellationToken cancellationToken = default);
}
