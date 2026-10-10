using WMSPro.Application.Picking.Services;
using Xunit;

namespace WMSPro.UnitTests;

public class PickPathOptimizerTests
{
    private readonly PickPathOptimizer _sut = new();

    [Fact]
    public void OptimizeRoute_ShouldSortEvenAisleAscendingAndOddAisleDescending_WhenMultipleAislesProvided()
    {
        // Arrange
        // Aisle A01 (Even index 0 in sorted order): should be R01 -> R02
        // Aisle A02 (Odd index 1 in sorted order): should be R02 -> R01 (reverse traversal)
        var candidates = new List<PickLocationCandidate>
        {
            new(Guid.NewGuid(), "SO-001", Guid.NewGuid(), "Prod A", "SKU-A", Guid.NewGuid(), "Z1-A01-R02-B01", "Zone 1", "A01", "R02", 1, "B01", 5),
            new(Guid.NewGuid(), "SO-001", Guid.NewGuid(), "Prod B", "SKU-B", Guid.NewGuid(), "Z1-A01-R01-B01", "Zone 1", "A01", "R01", 1, "B01", 2),
            new(Guid.NewGuid(), "SO-002", Guid.NewGuid(), "Prod C", "SKU-C", Guid.NewGuid(), "Z1-A02-R01-B01", "Zone 1", "A02", "R01", 1, "B01", 4),
            new(Guid.NewGuid(), "SO-002", Guid.NewGuid(), "Prod D", "SKU-D", Guid.NewGuid(), "Z1-A02-R02-B01", "Zone 1", "A02", "R02", 1, "B01", 3),
        };

        // Act
        var result = _sut.OptimizeRoute(candidates);

        // Assert
        Assert.Equal(4, result.Count);

        // Sequence 1 and 2 belong to Aisle A01 (index 0 - ascending rack)
        Assert.Equal("A01", result[0].Aisle);
        Assert.Equal("R01", result[0].Rack);
        Assert.Equal(1, result[0].SequenceOrder);

        Assert.Equal("A01", result[1].Aisle);
        Assert.Equal("R02", result[1].Rack);
        Assert.Equal(2, result[1].SequenceOrder);

        // Sequence 3 and 4 belong to Aisle A02 (index 1 - descending rack for snake return)
        Assert.Equal("A02", result[2].Aisle);
        Assert.Equal("R02", result[2].Rack);
        Assert.Equal(3, result[2].SequenceOrder);

        Assert.Equal("A02", result[3].Aisle);
        Assert.Equal("R01", result[3].Rack);
        Assert.Equal(4, result[3].SequenceOrder);
    }

    [Fact]
    public void OptimizeRoute_ShouldPlaceUnassignedItemsAtTheEnd()
    {
        // Arrange
        var candidates = new List<PickLocationCandidate>
        {
            new(Guid.NewGuid(), "SO-001", Guid.NewGuid(), "Floor Item", "SKU-FL", null, "UNASSIGNED", "Floor", "FL0", "R0", 0, "B0", 10),
            new(Guid.NewGuid(), "SO-001", Guid.NewGuid(), "Assigned Item", "SKU-AS", Guid.NewGuid(), "Z1-A01-R01-B01", "Zone 1", "A01", "R01", 1, "B01", 2),
        };

        // Act
        var result = _sut.OptimizeRoute(candidates);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.True(result[0].StorageLocationId.HasValue);
        Assert.Equal("Assigned Item", result[0].ProductName);
        Assert.False(result[1].StorageLocationId.HasValue);
        Assert.Equal("Floor Item", result[1].ProductName);
    }
}
