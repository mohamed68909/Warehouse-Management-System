namespace WMSPro.Application.Picking.Services;

public record PickLocationCandidate(
    Guid OrderId,
    string OrderNumber,
    Guid ProductId,
    string ProductName,
    string ProductSku,
    Guid? StorageLocationId,
    string LocationCode,
    string Zone,
    string Aisle,
    string Rack,
    int ShelfLevel,
    string BinNumber,
    int QuantityRequested);

public record OptimizedPickStop(
    Guid OrderId,
    string OrderNumber,
    Guid ProductId,
    string ProductName,
    string ProductSku,
    Guid? StorageLocationId,
    string LocationCode,
    string Zone,
    string Aisle,
    string Rack,
    int ShelfLevel,
    string BinNumber,
    int SequenceOrder,
    int QuantityRequested);

public interface IPickPathOptimizer
{
    IReadOnlyList<OptimizedPickStop> OptimizeRoute(IEnumerable<PickLocationCandidate> candidates);
}
