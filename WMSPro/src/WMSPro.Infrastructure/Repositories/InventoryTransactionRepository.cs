using Microsoft.EntityFrameworkCore;
using WMSPro.Application.Abstractions.Repositories;
using WMSPro.Domain.Entities;
using WMSPro.Domain.Enums;
using WMSPro.Infrastructure.Persistence;

namespace WMSPro.Infrastructure.Repositories;

public class InventoryTransactionRepository : IInventoryTransactionRepository
{
    private readonly ApplicationDbContext _context;
    public InventoryTransactionRepository(ApplicationDbContext context) => _context = context;

    public async Task<IReadOnlyList<InventoryTransaction>> GetAllAsync(
        string? productFilter,
        TransactionType? typeFilter,
        CancellationToken cancellationToken = default)
    {
        var query = _context.InventoryTransactions
            .Include(t => t.Product)
            .Include(t => t.SourceWarehouse)
            .Include(t => t.DestinationWarehouse)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(productFilter))
            query = query.Where(t =>
                t.Product != null && (
                    t.Product.Name.Contains(productFilter) ||
                    t.Product.SKU.Contains(productFilter)));

        if (typeFilter.HasValue)
            query = query.Where(t => t.Type == typeFilter.Value);

        return await query
            .OrderByDescending(t => t.Date)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<InventoryTransaction>> GetByProductAsync(
        Guid productId, CancellationToken cancellationToken = default) =>
        await _context.InventoryTransactions
            .Where(t => t.ProductId == productId)
            .Include(t => t.SourceWarehouse)
            .Include(t => t.DestinationWarehouse)
            .OrderByDescending(t => t.Date)
            .ToListAsync(cancellationToken);

    public async Task AddAsync(InventoryTransaction transaction, CancellationToken cancellationToken = default) =>
        await _context.InventoryTransactions.AddAsync(transaction, cancellationToken);
}
