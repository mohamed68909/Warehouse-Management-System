using WMSPro.Domain.Entities;
using WMSPro.Domain.Enums;
using Xunit;

namespace WMSPro.UnitTests;

public class PickingWaveTests
{
    [Fact]
    public void Create_ShouldInitializeWaveCorrectly_WhenValidDataProvided()
    {
        // Arrange
        var warehouseId = Guid.NewGuid();
        var waveNumber = "WAVE-20261007-TEST";

        // Act
        var result = PickingWave.Create(waveNumber, warehouseId, "Operator 1");

        // Assert
        Assert.True(result.IsSuccess);
        var wave = result.Value;
        Assert.Equal(waveNumber, wave.WaveNumber);
        Assert.Equal(warehouseId, wave.WarehouseId);
        Assert.Equal("Operator 1", wave.AssignedPicker);
        Assert.Equal(PickingWaveStatus.Generated, wave.Status);
        Assert.Equal(0, wave.TotalItems);
        Assert.Equal(0, wave.ProgressPercentage);
    }

    [Fact]
    public void ConfirmItemPick_ShouldUpdateStatusToInProgressThenCompleted()
    {
        // Arrange
        var wave = PickingWave.Create("WAVE-TEST", Guid.NewGuid()).Value;
        wave.AddPickItem(
            Guid.NewGuid(), "SO-001", Guid.NewGuid(), "Switch", "SKU-SW",
            Guid.NewGuid(), "Z1-A01-R01-B01", "Zone 1", "A01", "R01", 1, "B01", 1, 5);

        var itemId = wave.Items[0].Id;

        // Act - Confirm pick
        var confirmResult = wave.ConfirmItemPick(itemId, 5);

        // Assert
        Assert.True(confirmResult.IsSuccess);
        Assert.Equal(PickingWaveStatus.Completed, wave.Status);
        Assert.Equal(1, wave.PickedItems);
        Assert.Equal(100.0, wave.ProgressPercentage);
        Assert.True(wave.IsFullyPicked);
        Assert.NotNull(wave.CompletedAt);
    }

    [Fact]
    public void ConfirmItemPick_ShouldFail_WhenQuantityExceedsRequested()
    {
        // Arrange
        var wave = PickingWave.Create("WAVE-TEST", Guid.NewGuid()).Value;
        wave.AddPickItem(
            Guid.NewGuid(), "SO-001", Guid.NewGuid(), "Switch", "SKU-SW",
            Guid.NewGuid(), "Z1-A01-R01-B01", "Zone 1", "A01", "R01", 1, "B01", 1, 5);

        var itemId = wave.Items[0].Id;

        // Act
        var result = wave.ConfirmItemPick(itemId, 10);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("Error.Conflict", result.Error.Code);
    }
}
