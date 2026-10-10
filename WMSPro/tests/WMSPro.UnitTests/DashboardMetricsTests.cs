using Xunit;
using WMSPro.Application.Dashboard.Queries;

namespace WMSPro.UnitTests;

public class DashboardMetricsTests
{
    [Fact]
    public void AbcCategorySummaryDto_ShouldStoreCorrectValues()
    {
        // Arrange & Act
        var dto = new AbcCategorySummaryDto("Class A (High Value)", 5, 250000m, 76.5);

        // Assert
        Assert.Equal("Class A (High Value)", dto.Category);
        Assert.Equal(5, dto.ProductCount);
        Assert.Equal(250000m, dto.TotalValue);
        Assert.Equal(76.5, dto.ValuePercentage);
    }

    [Fact]
    public void DeadStockItemDto_ShouldCalculateTiedCapitalAccurately()
    {
        // Arrange
        var id = Guid.NewGuid();
        var sku = "LAP-DELL-01";
        var quantity = 25;
        var unitPrice = 1200m;
        var totalTied = quantity * unitPrice;

        // Act
        var deadStock = new DeadStockItemDto(id, "Dell Laptop", sku, quantity, unitPrice, totalTied, 65);

        // Assert
        Assert.Equal(30000m, deadStock.TotalTiedValue);
        Assert.Equal(65, deadStock.DaysInactive);
        Assert.Equal(sku, deadStock.SKU);
    }

    [Fact]
    public void WarehouseCapacityDto_ShouldReflectUtilizationCorrectly()
    {
        // Arrange
        var id = Guid.NewGuid();
        var capacity = 1000;
        var currentStock = 850;
        var utilization = Math.Round((double)currentStock / capacity * 100, 1);

        // Act
        var dto = new WarehouseCapacityDto(id, "Main Hub", "Cairo", capacity, currentStock, utilization);

        // Assert
        Assert.Equal(85.0, dto.UtilizationPercent);
        Assert.Equal(1000, dto.Capacity);
        Assert.Equal(850, dto.CurrentStock);
    }
}
