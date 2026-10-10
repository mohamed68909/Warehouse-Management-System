using System.Text;
using WMSPro.Application.Dashboard.Queries;
using WMSPro.Application.Orders.Commands;
using WMSPro.Infrastructure.Services;
using Xunit;

namespace WMSPro.UnitTests;

public class PdfReportServiceTests
{
    private readonly PdfReportService _sut = new();

    [Fact]
    public void GenerateOrderInvoicePdf_ShouldReturnValidPdfBytes_WhenOrderProvided()
    {
        // Arrange
        var order = new OrderDto(
            Id: Guid.NewGuid(),
            OrderNumber: "PO-20261007-TEST",
            Type: "Purchase",
            Status: "Completed",
            SupplierCustomerName: "Global Tech Supplier LLC",
            WarehouseId: Guid.NewGuid(),
            WarehouseName: "Central Logistics Hub",
            TotalAmount: 12500.50m,
            Date: DateTime.UtcNow,
            Items: new List<OrderItemDto>
            {
                new(Guid.NewGuid(), Guid.NewGuid(), "Enterprise High-Speed Switch", 10, 1000.00m, 10000.00m),
                new(Guid.NewGuid(), Guid.NewGuid(), "Industrial Fiber Cable 50m", 50, 50.01m, 2500.50m)
            }
        );

        // Act
        var pdfBytes = _sut.GenerateOrderInvoicePdf(order, "Zone 4, Industrial Park", "John Warehouse Manager");

        // Assert
        Assert.NotNull(pdfBytes);
        Assert.True(pdfBytes.Length > 1000, "PDF byte array should contain generated document content.");

        // Check PDF magic header %PDF-
        var header = Encoding.ASCII.GetString(pdfBytes.Take(5).ToArray());
        Assert.Equal("%PDF-", header);
    }

    [Fact]
    public void GenerateExecutiveDashboardPdf_ShouldReturnValidPdfBytes_WhenStatsProvided()
    {
        // Arrange
        var stats = new DashboardStatsDto(
            TotalProducts: 120,
            TotalStock: 45000,
            LowStockAlertsCount: 3,
            TotalWarehouses: 4,
            TotalInventoryValue: 1250000.75m,
            InventoryTurnoverRatio: 4.8,
            DaysSalesOfInventory: 76,
            DeadStockValue: 15400.00m,
            DeadStockCount: 2,
            TotalOrders: 350,
            CompletedOrders: 320,
            OrderFulfillmentRate: 91.4,
            TotalSalesRevenue: 850000.00m,
            TotalPurchaseExpense: 620000.00m,
            WarehouseCapacities: new List<WarehouseCapacityDto>
            {
                new(Guid.NewGuid(), "Central Hub", "Building A", 50000, 32000, 64.0),
                new(Guid.NewGuid(), "Northern Depo", "Building B", 20000, 13000, 65.0)
            },
            AbcAnalysis: new List<AbcCategorySummaryDto>
            {
                new("Class A (High Value)", 20, 875000.00m, 70.0),
                new("Class B (Moderate)", 40, 250000.00m, 20.0),
                new("Class C (Low Value)", 60, 125000.75m, 10.0)
            },
            DeadStockItems: new List<DeadStockItemDto>
            {
                new(Guid.NewGuid(), "Discontinued Router", "SKU-RTR-001", 100, 120.00m, 12000.00m, 95),
                new(Guid.NewGuid(), "Legacy Cat5 Cable", "SKU-CBL-002", 340, 10.00m, 3400.00m, 120)
            },
            RecentActivities: new List<RecentActivityDto>
            {
                new("STOCK_IN", "Enterprise High-Speed Switch", 50, "Central Hub", DateTime.UtcNow.AddHours(-2)),
                new("STOCK_OUT", "Industrial Fiber Cable", 20, "Central Hub", DateTime.UtcNow.AddHours(-1))
            },
            StockMovementTrend: new List<StockMovementTrendDto>
            {
                new("Oct 2026", 1500, 1200)
            },
            CriticalAlerts: new List<CriticalAlertDto>()
        );

        // Act
        var pdfBytes = _sut.GenerateExecutiveDashboardPdf(stats);

        // Assert
        Assert.NotNull(pdfBytes);
        Assert.True(pdfBytes.Length > 1000, "Executive PDF byte array should contain generated document content.");

        // Check PDF magic header %PDF-
        var header = Encoding.ASCII.GetString(pdfBytes.Take(5).ToArray());
        Assert.Equal("%PDF-", header);
    }

    [Fact]
    public void GeneratePickListPdf_ShouldReturnValidPdfBytes_WhenWaveProvided()
    {
        // Arrange
        var wave = new WMSPro.Application.Picking.Commands.PickingWaveDto(
            Id: Guid.NewGuid(),
            WaveNumber: "WAVE-20261007-TEST",
            WarehouseId: Guid.NewGuid(),
            WarehouseName: "Distribution Center Alpha",
            Status: "InProgress",
            AssignedPicker: "Picker John",
            TotalItems: 2,
            PickedItems: 1,
            ProgressPercentage: 50.0,
            IsFullyPicked: false,
            CreatedAt: DateTime.UtcNow,
            CompletedAt: null,
            Items: new List<WMSPro.Application.Picking.Commands.PickItemDto>
            {
                new(Guid.NewGuid(), Guid.NewGuid(), "SO-101", Guid.NewGuid(), "Enterprise Router", "SKU-RTR-01", Guid.NewGuid(), "Z1-A01-R01-B01", "Zone 1", "A01", "R01", 1, "B01", 1, 5, 5, true, DateTime.UtcNow),
                new(Guid.NewGuid(), Guid.NewGuid(), "SO-102", Guid.NewGuid(), "Fiber Cable", "SKU-CBL-02", Guid.NewGuid(), "Z1-A01-R02-B02", "Zone 1", "A01", "R02", 2, "B02", 2, 10, 0, false, null)
            }
        );

        // Act
        var pdfBytes = _sut.GeneratePickListPdf(wave, "Distribution Center Alpha");

        // Assert
        Assert.NotNull(pdfBytes);
        Assert.True(pdfBytes.Length > 1000, "Pick list PDF should contain document bytes.");
        var header = Encoding.ASCII.GetString(pdfBytes.Take(5).ToArray());
        Assert.Equal("%PDF-", header);
    }
}
