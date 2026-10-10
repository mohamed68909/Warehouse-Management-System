using Microsoft.EntityFrameworkCore;
using WMSPro.Application.Abstractions.Repositories;
using WMSPro.Domain.Entities;
using WMSPro.Infrastructure.Persistence;

namespace WMSPro.Infrastructure.Repositories;

public class ProductRepository : IProductRepository
{
    private readonly ApplicationDbContext _context;
    public ProductRepository(ApplicationDbContext context) => _context = context;

    public async Task<Product?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        await _context.Products.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

    public async Task<Product?> GetBySkuAsync(string sku, CancellationToken cancellationToken = default) =>
        await _context.Products.FirstOrDefaultAsync(p => p.SKU == sku.Trim().ToUpper(), cancellationToken);

    public async Task<IReadOnlyList<Product>> GetAllAsync(string? search = null, CancellationToken cancellationToken = default)
    {
        var query = _context.Products.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(p => p.Name.Contains(search) || p.SKU.Contains(search));
        }

        return await query.OrderBy(p => p.Name).ToListAsync(cancellationToken);
    }

    public async Task<bool> ExistsBySkuAsync(string sku, CancellationToken cancellationToken = default) =>
        await _context.Products.AnyAsync(p => p.SKU == sku.ToUpper(), cancellationToken);

    public async Task<bool> ExistsBySkuExceptAsync(string sku, Guid excludeId, CancellationToken cancellationToken = default) =>
        await _context.Products.AnyAsync(p => p.SKU == sku.ToUpper() && p.Id != excludeId, cancellationToken);

    public async Task AddAsync(Product product, CancellationToken cancellationToken = default) =>
        await _context.Products.AddAsync(product, cancellationToken);

    public void Update(Product product) => _context.Products.Update(product);

    public void Delete(Product product) => _context.Products.Remove(product);
}
