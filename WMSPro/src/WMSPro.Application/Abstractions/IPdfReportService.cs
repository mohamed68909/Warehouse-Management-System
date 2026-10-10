using WMSPro.Application.Dashboard.Queries;
using WMSPro.Application.Orders.Commands;
using WMSPro.Application.Picking.Commands;

namespace WMSPro.Application.Abstractions;

public interface IPdfReportService
{
    byte[] GenerateOrderInvoicePdf(OrderDto order, string? warehouseLocation = null, string? warehouseManager = null);
    byte[] GenerateExecutiveDashboardPdf(DashboardStatsDto stats);
    byte[] GeneratePickListPdf(PickingWaveDto wave, string? warehouseName = null);
}
