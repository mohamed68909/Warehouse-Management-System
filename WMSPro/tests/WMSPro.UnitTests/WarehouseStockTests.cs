using Xunit;
using WMSPro.Domain.Entities;

namespace WMSPro.UnitTests;

public class WarehouseStockTests
{
    [Fact]
    public void Create_WithValidParameters_ShouldReturnSuccess()
    {
        // Arrange
        var warehouseId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var initialQuantity = 50;

        // Act
        var result = WarehouseStock.Create(warehouseId, productId, initialQuantity);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(warehouseId, result.Value.WarehouseId);
        Assert.Equal(productId, result.Value.ProductId);
        Assert.Equal(initialQuantity, result.Value.Quantity);
    }

    [Fact]
    public void Create_WithNegativeQuantity_ShouldReturnFailure()
    {
        // Arrange
        var warehouseId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var initialQuantity = -5;

        // Act
        var result = WarehouseStock.Create(warehouseId, productId, initialQuantity);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("Validation.quantity", result.Error.Code);
    }

    [Fact]
    public void AddStock_WithPositiveQuantity_ShouldIncreaseStock()
    {
        // Arrange
        var stock = WarehouseStock.Create(Guid.NewGuid(), Guid.NewGuid(), 50).Value;

        // Act
        var result = stock.AddStock(30);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(80, stock.Quantity);
    }

    [Fact]
    public void RemoveStock_WithAvailableQuantity_ShouldDecreaseStock()
    {
        // Arrange
        var stock = WarehouseStock.Create(Guid.NewGuid(), Guid.NewGuid(), 50).Value;

        // Act
        var result = stock.RemoveStock(20);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(30, stock.Quantity);
    }

    [Fact]
    public void RemoveStock_WithInsufficientQuantity_ShouldReturnFailure()
    {
        // Arrange
        var stock = WarehouseStock.Create(Guid.NewGuid(), Guid.NewGuid(), 10).Value;

        // Act
        var result = stock.RemoveStock(20);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("Validation.quantity", result.Error.Code);
    }
}
