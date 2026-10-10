using Xunit;
using WMSPro.Application.Abstractions.Repositories;
using WMSPro.Application.Products.Commands;
using WMSPro.Application.Products.Handlers;
using WMSPro.Domain.Entities;

namespace WMSPro.UnitTests;

public class FakeProductRepository : IProductRepository
{
    private readonly List<Product> _products = new();

    public FakeProductRepository(params Product[] products)
    {
        _products.AddRange(products);
    }

    public Task<Product?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        Task.FromResult(_products.FirstOrDefault(p => p.Id == id));

    public Task<Product?> GetBySkuAsync(string sku, CancellationToken cancellationToken = default) =>
        Task.FromResult(_products.FirstOrDefault(p => p.SKU == sku.Trim().ToUpper()));

    public Task<IReadOnlyList<Product>> GetAllAsync(string? search = null, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<Product>>(_products.AsReadOnly());

    public Task<bool> ExistsBySkuAsync(string sku, CancellationToken cancellationToken = default) =>
        Task.FromResult(_products.Any(p => p.SKU == sku.ToUpper()));

    public Task<bool> ExistsBySkuExceptAsync(string sku, Guid excludeId, CancellationToken cancellationToken = default) =>
        Task.FromResult(_products.Any(p => p.SKU == sku.ToUpper() && p.Id != excludeId));

    public Task AddAsync(Product product, CancellationToken cancellationToken = default)
    {
        _products.Add(product);
        return Task.CompletedTask;
    }

    public void Update(Product product) { }
    public void Delete(Product product) => _products.Remove(product);
}

public class ProductSkuTests
{
    [Fact]
    public async Task GetProductBySku_WhenNotFound_ShouldReturnFailure()
    {
        // Arrange
        var fakeRepo = new FakeProductRepository();
        var handler = new GetProductBySkuHandler(fakeRepo);

        // Act
        var result = await handler.Handle(new GetProductBySkuQuery("NON-EXISTENT"), CancellationToken.None);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("Product.NotFound", result.Error.Code);
    }

    [Fact]
    public async Task GetProductBySku_WhenFound_ShouldReturnProductDto()
    {
        // Arrange
        var product = Product.Create("Logitech Mouse", "ACC-LOG-01", "Accessories", 49.99m, 120).Value;
        var fakeRepo = new FakeProductRepository(product);
        var handler = new GetProductBySkuHandler(fakeRepo);

        // Act
        var result = await handler.Handle(new GetProductBySkuQuery("ACC-LOG-01"), CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal("ACC-LOG-01", result.Value.SKU);
        Assert.Equal("Logitech Mouse", result.Value.Name);
        Assert.Equal(120, result.Value.Quantity);
    }
}
