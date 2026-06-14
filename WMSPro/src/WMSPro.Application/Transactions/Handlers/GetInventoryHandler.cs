using MediatR;
using WMSPro.Application.Abstractions.Repositories;
using WMSPro.Application.Transactions.Commands;
using WMSPro.Domain.Common;

namespace WMSPro.Application.Transactions.Handlers;

public class GetInventoryHandler : IRequestHandler<GetInventoryQuery, Result<IReadOnlyList<InventoryItemDto>>>
{
    private readonly IWarehouseStockRepository _warehouseStockRepo;

    public GetInventoryHandler(IWarehouseStockRepository warehouseStockRepo)
    {
        _warehouseStockRepo = warehouseStockRepo;
    }

    public async Task<Result<IReadOnlyList<InventoryItemDto>>> Handle(GetInventoryQuery request, CancellationToken cancellationToken)
    {
        var stocks = await _warehouseStockRepo.GetAllWithDetailsAsync(cancellationToken);

        var result = stocks.Select(ws => new InventoryItemDto(
            ws.ProductId,
            ws.Product?.Name ?? string.Empty,
            ws.Product?.SKU ?? string.Empty,
            ws.WarehouseId,
            ws.Warehouse?.Name ?? string.Empty,
            ws.Quantity))
            .ToList()
            .AsReadOnly();

        return Result.Success<IReadOnlyList<InventoryItemDto>>(result);
    }
}
