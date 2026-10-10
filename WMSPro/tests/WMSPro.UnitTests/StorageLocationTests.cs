using Xunit;
using WMSPro.Domain.Entities;

namespace WMSPro.UnitTests;

public class StorageLocationTests
{
    [Fact]
    public void Create_WithValidParameters_ShouldReturnSuccess()
    {
        // Arrange
        var warehouseId = Guid.NewGuid();
        var code = "A01-R01-L1-B01";
        var zone = "Zone A (Tech)";
        var capacity = 50;

        // Act
        var result = StorageLocation.Create(
            warehouseId, code, zone, "A01", "R01", 1, "B01", capacity);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(code, result.Value.LocationCode);
        Assert.Equal(zone, result.Value.Zone);
        Assert.Equal(capacity, result.Value.CapacityUnits);
        Assert.Equal(0, result.Value.CurrentStock);
        Assert.True(result.Value.IsEmpty);
        Assert.False(result.Value.IsFull);
    }

    [Fact]
    public void Create_WhenCurrentStockExceedsCapacity_ShouldReturnFailure()
    {
        // Arrange
        var warehouseId = Guid.NewGuid();

        // Act
        var result = StorageLocation.Create(
            warehouseId, "A01-R01-L1-B01", "Zone A", "A01", "R01", 1, "B01", capacityUnits: 40, currentStock: 50);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("Error.Conflict", result.Error.Code);
    }

    [Fact]
    public void AssignProduct_ShouldUpdateStockAndProduct()
    {
        // Arrange
        var loc = StorageLocation.Create(
            Guid.NewGuid(), "A01-R01-L1-B01", "Zone A", "A01", "R01", 1, "B01", capacityUnits: 100).Value;
        var productId = Guid.NewGuid();

        // Act
        var result = loc.AssignProduct(productId, 75);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(productId, loc.ProductId);
        Assert.Equal(75, loc.CurrentStock);
        Assert.Equal(75.0, loc.CapacityUsagePercent);
        Assert.False(loc.IsEmpty);
    }

    [Fact]
    public void AdjustStock_ToZero_ShouldClearProduct()
    {
        // Arrange
        var loc = StorageLocation.Create(
            Guid.NewGuid(), "A01-R01-L1-B01", "Zone A", "A01", "R01", 1, "B01", capacityUnits: 100).Value;
        loc.AssignProduct(Guid.NewGuid(), 50);

        // Act
        var result = loc.AdjustStock(0);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(0, loc.CurrentStock);
        Assert.Null(loc.ProductId);
        Assert.True(loc.IsEmpty);
    }

    [Fact]
    public void EmptyBin_ShouldResetStockAndProduct()
    {
        // Arrange
        var loc = StorageLocation.Create(
            Guid.NewGuid(), "A01-R01-L1-B01", "Zone A", "A01", "R01", 1, "B01", capacityUnits: 100).Value;
        loc.AssignProduct(Guid.NewGuid(), 40);

        // Act
        loc.EmptyBin();

        // Assert
        Assert.Equal(0, loc.CurrentStock);
        Assert.Null(loc.ProductId);
        Assert.True(loc.IsEmpty);
    }
}
