using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using WMSPro.Application.Abstractions;
using WMSPro.Application.Dashboard.Queries;
using WMSPro.Application.Orders.Commands;
using WMSPro.Application.Picking.Commands;

namespace WMSPro.Infrastructure.Services;

public class PdfReportService : IPdfReportService
{
    public PdfReportService()
    {
        QuestPDF.Settings.License = LicenseType.Community;
        QuestPDF.Settings.UseSystemFonts = true;
        QuestPDF.Settings.ThrowOnMissingFontFamilies = false;
    }

    public byte[] GenerateOrderInvoicePdf(OrderDto order, string? warehouseLocation = null, string? warehouseManager = null)
    {
        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(30);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontSize(10).FontColor(Colors.Grey.Darken3));

                // ── HEADER ──────────────────────────────────────────────────
                page.Header().Column(col =>
                {
                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Column(titleCol =>
                        {
                            titleCol.Item().Text("WMS PRO ENTERPRISE").Bold().FontSize(20).FontColor(Colors.Blue.Darken2);
                            titleCol.Item().Text("Advanced Warehouse Management & Logistics System").FontSize(9).FontColor(Colors.Grey.Medium);
                        });

                        row.RelativeItem().AlignRight().Column(metaCol =>
                        {
                            metaCol.Item().Text(order.Type.ToUpper() + " ORDER INVOICE").Bold().FontSize(14).FontColor(Colors.Grey.Darken4);
                            metaCol.Item().Text($"Order #: {order.OrderNumber}").Bold().FontSize(11).FontColor(Colors.Blue.Darken2);
                            metaCol.Item().Text($"Date: {order.Date.ToLocalTime():yyyy-MM-dd HH:mm}").FontSize(9);
                            metaCol.Item().Text($"Status: {order.Status}").Bold().FontSize(9).FontColor(order.Status == "Completed" ? Colors.Green.Darken2 : Colors.Orange.Darken2);
                        });
                    });

                    col.Item().PaddingVertical(10).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);

                    // ── PARTY DETAILS ───────────────────────────────────────
                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(10).Column(c =>
                        {
                            c.Item().Text(order.Type == "Purchase" ? "SUPPLIER INFORMATION" : "CUSTOMER INFORMATION").Bold().FontSize(9).FontColor(Colors.Blue.Darken2);
                            c.Item().Text(order.SupplierCustomerName).Bold().FontSize(11);
                            c.Item().Text("Account: Commercial Enterprise").FontSize(9);
                            c.Item().Text("Authorized Billing Entity").FontSize(9);
                        });

                        row.ConstantItem(15);

                        row.RelativeItem().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(10).Column(c =>
                        {
                            c.Item().Text("FULFILLMENT FACILITY").Bold().FontSize(9).FontColor(Colors.Blue.Darken2);
                            c.Item().Text(order.WarehouseName ?? "Primary Distribution Hub").Bold().FontSize(11);
                            c.Item().Text($"Location: {warehouseLocation ?? "123 Logistics Way"}").FontSize(9);
                            c.Item().Text($"Facility Manager: {warehouseManager ?? "Warehouse Operations"}").FontSize(9);
                        });
                    });

                    col.Item().Height(15);
                });

                // ── CONTENT (ITEMS TABLE) ───────────────────────────────────
                page.Content().Column(col =>
                {
                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.ConstantColumn(30);
                            columns.RelativeColumn(3);
                            columns.RelativeColumn(2);
                            columns.RelativeColumn(1);
                            columns.RelativeColumn(1.5f);
                            columns.RelativeColumn(1.5f);
                        });

                        table.Header(header =>
                        {
                            header.Cell().Background(Colors.Grey.Lighten3).Padding(5).Text("#").Bold();
                            header.Cell().Background(Colors.Grey.Lighten3).Padding(5).Text("Product Description").Bold();
                            header.Cell().Background(Colors.Grey.Lighten3).Padding(5).Text("SKU / Code").Bold();
                            header.Cell().Background(Colors.Grey.Lighten3).Padding(5).AlignRight().Text("Qty").Bold();
                            header.Cell().Background(Colors.Grey.Lighten3).Padding(5).AlignRight().Text("Unit Price").Bold();
                            header.Cell().Background(Colors.Grey.Lighten3).Padding(5).AlignRight().Text("Total").Bold();
                        });

                        int idx = 1;
                        foreach (var item in order.Items)
                        {
                            var bg = idx % 2 == 0 ? Colors.Grey.Lighten4 : Colors.White;

                            table.Cell().Background(bg).Padding(5).Text(idx.ToString());
                            table.Cell().Background(bg).Padding(5).Text(item.ProductName).SemiBold();
                            table.Cell().Background(bg).Padding(5).Text(item.Id.ToString().Substring(0, 8).ToUpper());
                            table.Cell().Background(bg).Padding(5).AlignRight().Text(item.Quantity.ToString());
                            table.Cell().Background(bg).Padding(5).AlignRight().Text($"${item.UnitPrice:N2}");
                            table.Cell().Background(bg).Padding(5).AlignRight().Text($"${item.TotalPrice:N2}").Bold();
                            idx++;
                        }
                    });

                    col.Item().Height(15);

                    // ── TOTALS ──────────────────────────────────────────────
                    col.Item().Row(row =>
                    {
                        row.RelativeItem(2).Column(noteCol =>
                        {
                            noteCol.Item().Text("NOTES & DISPATCH TERMS:").Bold().FontSize(8);
                            noteCol.Item().Text("• Goods received/dispatched under official WMS Pro warehouse verification.")
                                .FontSize(8).FontColor(Colors.Grey.Medium);
                            noteCol.Item().Text("• All inventory balances and journal ledgers updated upon order completion.")
                                .FontSize(8).FontColor(Colors.Grey.Medium);
                        });

                        row.RelativeItem(1).Border(1).BorderColor(Colors.Grey.Lighten2).Padding(8).Column(totCol =>
                        {
                            totCol.Item().Row(r =>
                            {
                                r.RelativeItem().Text("Subtotal:");
                                r.RelativeItem().AlignRight().Text($"${order.TotalAmount:N2}");
                            });

                            totCol.Item().Row(r =>
                            {
                                r.RelativeItem().Text("Tax / VAT (0%):");
                                r.RelativeItem().AlignRight().Text("$0.00");
                            });

                            totCol.Item().PaddingVertical(3).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);

                            totCol.Item().Row(r =>
                            {
                                r.RelativeItem().Text("TOTAL:").Bold().FontSize(12).FontColor(Colors.Blue.Darken2);
                                r.RelativeItem().AlignRight().Text($"${order.TotalAmount:N2}").Bold().FontSize(12).FontColor(Colors.Blue.Darken2);
                            });
                        });
                    });

                    col.Item().Height(25);

                    // ── SIGNATURES ──────────────────────────────────────────
                    col.Item().Row(sigRow =>
                    {
                        sigRow.RelativeItem().Column(c =>
                        {
                            c.Item().Text("Prepared By:").FontSize(8).FontColor(Colors.Grey.Medium);
                            c.Item().PaddingTop(25).LineHorizontal(1).LineColor(Colors.Grey.Medium);
                            c.Item().PaddingTop(2).Text("Warehouse Officer").FontSize(8).AlignCenter();
                        });

                        sigRow.ConstantItem(40);

                        sigRow.RelativeItem().Column(c =>
                        {
                            c.Item().Text("Verified & Stamped By:").FontSize(8).FontColor(Colors.Grey.Medium);
                            c.Item().PaddingTop(25).LineHorizontal(1).LineColor(Colors.Grey.Medium);
                            c.Item().PaddingTop(2).Text("Logistics Operations Lead").FontSize(8).AlignCenter();
                        });

                        sigRow.ConstantItem(40);

                        sigRow.RelativeItem().Column(c =>
                        {
                            c.Item().Text("Received / Approved By:").FontSize(8).FontColor(Colors.Grey.Medium);
                            c.Item().PaddingTop(25).LineHorizontal(1).LineColor(Colors.Grey.Medium);
                            c.Item().PaddingTop(2).Text("Carrier / Recipient Signature").FontSize(8).AlignCenter();
                        });
                    });
                });

                // ── FOOTER ──────────────────────────────────────────────────
                page.Footer().AlignCenter().Text(x =>
                {
                    x.Span("Generated via WMS Pro Executive Engine • Page ");
                    x.CurrentPageNumber();
                    x.Span(" of ");
                    x.TotalPages();
                });
            });
        });

        return document.GeneratePdf();
    }

    public byte[] GenerateExecutiveDashboardPdf(DashboardStatsDto stats)
    {
        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(30);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontSize(10).FontColor(Colors.Grey.Darken3));

                // ── HEADER ──────────────────────────────────────────────────
                page.Header().Column(col =>
                {
                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().Text("WMS PRO ENTERPRISE").Bold().FontSize(22).FontColor(Colors.Blue.Darken2);
                            c.Item().Text("EXECUTIVE INVENTORY & CAPITAL DIAGNOSTICS REPORT").Bold().FontSize(12).FontColor(Colors.Grey.Darken4);
                        });

                        row.RelativeItem().AlignRight().Column(c =>
                        {
                            c.Item().Text($"Generated: {DateTime.UtcNow:yyyy-MM-dd HH:mm} UTC").FontSize(9);
                            c.Item().Text("Status: Official Audit Snapshot").FontSize(9).FontColor(Colors.Green.Darken2);
                        });
                    });

                    col.Item().PaddingVertical(8).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);
                });

                // ── CONTENT ─────────────────────────────────────────────────
                page.Content().Column(col =>
                {
                    // Hero KPI Grid (4 Boxes)
                    col.Item().Row(r =>
                    {
                        r.RelativeItem().Border(1).BorderColor(Colors.Grey.Lighten2).Background(Colors.Grey.Lighten4).Padding(8).Column(c =>
                        {
                            c.Item().Text("TOTAL VALUATION").FontSize(8).FontColor(Colors.Grey.Darken1);
                            c.Item().Text($"${stats.TotalInventoryValue:N2}").Bold().FontSize(14).FontColor(Colors.Blue.Darken2);
                            c.Item().Text($"{stats.TotalProducts} Catalog SKUs").FontSize(8).FontColor(Colors.Grey.Medium);
                        });

                        r.ConstantItem(8);

                        r.RelativeItem().Border(1).BorderColor(Colors.Grey.Lighten2).Background(Colors.Grey.Lighten4).Padding(8).Column(c =>
                        {
                            c.Item().Text("STOCK VOLUME").FontSize(8).FontColor(Colors.Grey.Darken1);
                            c.Item().Text($"{stats.TotalStock:N0} Units").Bold().FontSize(14).FontColor(Colors.Green.Darken2);
                            c.Item().Text($"Across {stats.TotalWarehouses} Hubs").FontSize(8).FontColor(Colors.Grey.Medium);
                        });

                        r.ConstantItem(8);

                        r.RelativeItem().Border(1).BorderColor(Colors.Grey.Lighten2).Background(Colors.Grey.Lighten4).Padding(8).Column(c =>
                        {
                            c.Item().Text("TURNOVER VELOCITY").FontSize(8).FontColor(Colors.Grey.Darken1);
                            c.Item().Text($"{stats.InventoryTurnoverRatio:N2}x").Bold().FontSize(14).FontColor(Colors.Teal.Darken2);
                            c.Item().Text($"DSI: {stats.DaysSalesOfInventory} Days").FontSize(8).FontColor(Colors.Grey.Medium);
                        });

                        r.ConstantItem(8);

                        r.RelativeItem().Border(1).BorderColor(Colors.Grey.Lighten2).Background(Colors.Grey.Lighten4).Padding(8).Column(c =>
                        {
                            c.Item().Text("DEAD STOCK RISK").FontSize(8).FontColor(Colors.Grey.Darken1);
                            c.Item().Text($"${stats.DeadStockValue:N2}").Bold().FontSize(14).FontColor(Colors.Red.Darken2);
                            c.Item().Text($"{stats.DeadStockCount} Items Inactive").FontSize(8).FontColor(Colors.Grey.Medium);
                        });
                    });

                    col.Item().Height(15);

                    // Facility Utilization Table
                    col.Item().Text("WAREHOUSE FACILITY CAPACITIES & UTILIZATION").Bold().FontSize(11).FontColor(Colors.Blue.Darken2);
                    col.Item().Height(4);

                    col.Item().Table(t =>
                    {
                        t.ColumnsDefinition(cd =>
                        {
                            cd.RelativeColumn(3);
                            cd.RelativeColumn(2);
                            cd.RelativeColumn(1.5f);
                            cd.RelativeColumn(1.5f);
                            cd.RelativeColumn(1.5f);
                        });

                        t.Header(h =>
                        {
                            h.Cell().Background(Colors.Grey.Lighten3).Padding(4).Text("Facility Name").Bold();
                            h.Cell().Background(Colors.Grey.Lighten3).Padding(4).Text("Location").Bold();
                            h.Cell().Background(Colors.Grey.Lighten3).Padding(4).AlignRight().Text("Capacity").Bold();
                            h.Cell().Background(Colors.Grey.Lighten3).Padding(4).AlignRight().Text("Current Stock").Bold();
                            h.Cell().Background(Colors.Grey.Lighten3).Padding(4).AlignRight().Text("Utilization").Bold();
                        });

                        foreach (var wh in stats.WarehouseCapacities)
                        {
                            t.Cell().Padding(4).Text(wh.Name);
                            t.Cell().Padding(4).Text(wh.Location);
                            t.Cell().Padding(4).AlignRight().Text($"{wh.Capacity:N0} u");
                            t.Cell().Padding(4).AlignRight().Text($"{wh.CurrentStock:N0} u");
                            t.Cell().Padding(4).AlignRight().Text($"{wh.UtilizationPercent}%").Bold();
                        }
                    });

                    col.Item().Height(15);

                    // ABC Classification Summary
                    col.Item().Text("ABC INVENTORY PARETO CLASSIFICATION").Bold().FontSize(11).FontColor(Colors.Blue.Darken2);
                    col.Item().Height(4);

                    col.Item().Table(t =>
                    {
                        t.ColumnsDefinition(cd =>
                        {
                            cd.RelativeColumn(2);
                            cd.RelativeColumn(1);
                            cd.RelativeColumn(1.5f);
                            cd.RelativeColumn(1.5f);
                        });

                        t.Header(h =>
                        {
                            h.Cell().Background(Colors.Grey.Lighten3).Padding(4).Text("Category Class").Bold();
                            h.Cell().Background(Colors.Grey.Lighten3).Padding(4).AlignRight().Text("Products").Bold();
                            h.Cell().Background(Colors.Grey.Lighten3).Padding(4).AlignRight().Text("Tied Valuation").Bold();
                            h.Cell().Background(Colors.Grey.Lighten3).Padding(4).AlignRight().Text("Capital Share").Bold();
                        });

                        foreach (var abc in stats.AbcAnalysis)
                        {
                            t.Cell().Padding(4).Text(abc.Category);
                            t.Cell().Padding(4).AlignRight().Text(abc.ProductCount.ToString());
                            t.Cell().Padding(4).AlignRight().Text($"${abc.TotalValue:N2}");
                            t.Cell().Padding(4).AlignRight().Text($"{abc.ValuePercentage}%").Bold();
                        }
                    });

                    col.Item().Height(15);

                    // Stagnant Stock Watchlist
                    if (stats.DeadStockItems.Any())
                    {
                        col.Item().Text("STAGNANT / DEAD STOCK AT RISK (60+ DAYS)").Bold().FontSize(11).FontColor(Colors.Red.Darken2);
                        col.Item().Height(4);

                        col.Item().Table(t =>
                        {
                            t.ColumnsDefinition(cd =>
                            {
                                cd.RelativeColumn(3);
                                cd.RelativeColumn(2);
                                cd.RelativeColumn(1);
                                cd.RelativeColumn(1.5f);
                                cd.RelativeColumn(1.5f);
                            });

                            t.Header(h =>
                            {
                                h.Cell().Background(Colors.Grey.Lighten3).Padding(4).Text("Product").Bold();
                                h.Cell().Background(Colors.Grey.Lighten3).Padding(4).Text("SKU").Bold();
                                h.Cell().Background(Colors.Grey.Lighten3).Padding(4).AlignRight().Text("Units").Bold();
                                h.Cell().Background(Colors.Grey.Lighten3).Padding(4).AlignRight().Text("Unit Price").Bold();
                                h.Cell().Background(Colors.Grey.Lighten3).Padding(4).AlignRight().Text("Frozen Capital").Bold();
                            });

                            foreach (var ds in stats.DeadStockItems.Take(5))
                            {
                                t.Cell().Padding(4).Text(ds.ProductName);
                                t.Cell().Padding(4).Text(ds.SKU);
                                t.Cell().Padding(4).AlignRight().Text(ds.Quantity.ToString());
                                t.Cell().Padding(4).AlignRight().Text($"${ds.Price:N2}");
                                t.Cell().Padding(4).AlignRight().Text($"${ds.TotalTiedValue:N2}").Bold().FontColor(Colors.Red.Darken2);
                            }
                        });
                    }
                });

                // ── FOOTER ──────────────────────────────────────────────────
                page.Footer().AlignCenter().Text(x =>
                {
                    x.Span("WMS Pro Enterprise Logistics Engine • Official Audit PDF • Page ");
                    x.CurrentPageNumber();
                    x.Span(" of ");
                    x.TotalPages();
                });
            });
        });

        return document.GeneratePdf();
    }

    public byte[] GeneratePickListPdf(PickingWaveDto wave, string? warehouseName = null)
    {
        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(30);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontSize(10).FontColor(Colors.Grey.Darken3));

                // ── HEADER ──────────────────────────────────────────────────
                page.Header().Column(col =>
                {
                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Column(titleCol =>
                        {
                            titleCol.Item().Text("WMS PRO LOGISTICS").Bold().FontSize(18).FontColor(Colors.Blue.Darken3);
                            titleCol.Item().Text("WAREHOUSE S-SHAPE PICK LIST & DISPATCH TICKET").Bold().FontSize(12).FontColor(Colors.Grey.Darken4);
                        });

                        row.RelativeItem().AlignRight().Column(metaCol =>
                        {
                            metaCol.Item().Text(wave.WaveNumber).Bold().FontSize(15).FontColor(Colors.Blue.Darken2);
                            metaCol.Item().Text($"Created: {wave.CreatedAt.ToLocalTime():yyyy-MM-dd HH:mm}").FontSize(9);
                            metaCol.Item().Text($"Status: {wave.Status}").Bold().FontSize(9).FontColor(wave.Status == "Completed" ? Colors.Green.Darken2 : Colors.Orange.Darken2);
                        });
                    });

                    col.Item().PaddingVertical(8).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);

                    // Operational Summary Cards
                    col.Item().Row(r =>
                    {
                        r.RelativeItem().Border(1).BorderColor(Colors.Grey.Lighten2).Background(Colors.Grey.Lighten4).Padding(6).Column(c =>
                        {
                            c.Item().Text("FULFILLMENT HUB").FontSize(8).FontColor(Colors.Grey.Darken1);
                            c.Item().Text(warehouseName ?? wave.WarehouseName ?? "Primary Hub").Bold().FontSize(10);
                        });

                        r.ConstantItem(8);

                        r.RelativeItem().Border(1).BorderColor(Colors.Grey.Lighten2).Background(Colors.Grey.Lighten4).Padding(6).Column(c =>
                        {
                            c.Item().Text("ASSIGNED PICKER").FontSize(8).FontColor(Colors.Grey.Darken1);
                            c.Item().Text(wave.AssignedPicker ?? "Floor Operations Team").Bold().FontSize(10);
                        });

                        r.ConstantItem(8);

                        r.RelativeItem().Border(1).BorderColor(Colors.Grey.Lighten2).Background(Colors.Grey.Lighten4).Padding(6).Column(c =>
                        {
                            c.Item().Text("PICK STOPS COUNT").FontSize(8).FontColor(Colors.Grey.Darken1);
                            c.Item().Text($"{wave.TotalItems} Stops").Bold().FontSize(10).FontColor(Colors.Blue.Darken2);
                        });

                        r.ConstantItem(8);

                        r.RelativeItem().Border(1).BorderColor(Colors.Grey.Lighten2).Background(Colors.Grey.Lighten4).Padding(6).Column(c =>
                        {
                            c.Item().Text("TOTAL UNITS TO PICK").FontSize(8).FontColor(Colors.Grey.Darken1);
                            c.Item().Text($"{wave.Items.Sum(i => i.QuantityRequested)} Units").Bold().FontSize(10).FontColor(Colors.Green.Darken2);
                        });
                    });

                    col.Item().Height(12);
                });

                // ── CONTENT (OPTIMIZED ROUTE TABLE) ─────────────────────────
                page.Content().Column(col =>
                {
                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(cd =>
                        {
                            cd.ConstantColumn(35);      // Stop #
                            cd.RelativeColumn(2.5f);    // Location (Zone/Aisle/Rack/Bin)
                            cd.RelativeColumn(3);       // Product & SKU
                            cd.RelativeColumn(1.8f);    // Source Order #
                            cd.RelativeColumn(1);       // Qty Req
                            cd.RelativeColumn(1.2f);    // Checked / Qty Picked
                        });

                        table.Header(h =>
                        {
                            h.Cell().Background(Colors.Grey.Lighten3).Padding(5).Text("Stop").Bold();
                            h.Cell().Background(Colors.Grey.Lighten3).Padding(5).Text("Location Coordinate").Bold();
                            h.Cell().Background(Colors.Grey.Lighten3).Padding(5).Text("Item / SKU").Bold();
                            h.Cell().Background(Colors.Grey.Lighten3).Padding(5).Text("Order #").Bold();
                            h.Cell().Background(Colors.Grey.Lighten3).Padding(5).AlignRight().Text("Qty").Bold();
                            h.Cell().Background(Colors.Grey.Lighten3).Padding(5).AlignCenter().Text("Verified").Bold();
                        });

                        int rowIdx = 1;
                        foreach (var item in wave.Items.OrderBy(i => i.SequenceOrder))
                        {
                            var bg = rowIdx % 2 == 0 ? Colors.Grey.Lighten4 : Colors.White;

                            // Stop #
                            table.Cell().Background(bg).Padding(5).AlignCenter().Text($"#{item.SequenceOrder}").Bold().FontColor(Colors.Blue.Darken2);

                            // Location coordinate with badge look
                            table.Cell().Background(bg).Padding(5).Column(locCol =>
                            {
                                locCol.Item().Text(item.LocationCode).Bold().FontSize(10).FontColor(Colors.Blue.Darken3);
                                locCol.Item().Text($"{item.Zone} • Aisle {item.Aisle} • R{item.Rack}-S{item.ShelfLevel}").FontSize(8).FontColor(Colors.Grey.Darken1);
                            });

                            // Product & SKU
                            table.Cell().Background(bg).Padding(5).Column(pCol =>
                            {
                                pCol.Item().Text(item.ProductName).SemiBold().FontSize(9);
                                pCol.Item().Text($"SKU: {item.ProductSku}").FontSize(8).FontColor(Colors.Grey.Darken2);
                            });

                            // Order Number
                            table.Cell().Background(bg).Padding(5).Text(item.OrderNumber).FontSize(9);

                            // Quantity Required
                            table.Cell().Background(bg).Padding(5).AlignRight().Text(item.QuantityRequested.ToString()).Bold().FontSize(11).FontColor(Colors.Green.Darken3);

                            // Checkbox / Picked slot
                            table.Cell().Background(bg).Padding(5).AlignCenter().Column(chkCol =>
                            {
                                if (item.IsPicked)
                                {
                                    chkCol.Item().Text($"✓ ({item.QuantityPicked})").Bold().FontColor(Colors.Green.Darken2);
                                }
                                else
                                {
                                    chkCol.Item().Text("[  ] _____").FontColor(Colors.Grey.Medium);
                                }
                            });

                            rowIdx++;
                        }
                    });

                    col.Item().Height(25);

                    // Signatures block
                    col.Item().Row(sigRow =>
                    {
                        sigRow.RelativeItem().Column(c =>
                        {
                            c.Item().Text("Picker Signature & Date:").FontSize(8).FontColor(Colors.Grey.Medium);
                            c.Item().PaddingTop(25).LineHorizontal(1).LineColor(Colors.Grey.Medium);
                            c.Item().PaddingTop(2).Text(wave.AssignedPicker ?? "Floor Order Picker").FontSize(8).AlignCenter();
                        });

                        sigRow.ConstantItem(40);

                        sigRow.RelativeItem().Column(c =>
                        {
                            c.Item().Text("Supervisor Audit & Release:").FontSize(8).FontColor(Colors.Grey.Medium);
                            c.Item().PaddingTop(25).LineHorizontal(1).LineColor(Colors.Grey.Medium);
                            c.Item().PaddingTop(2).Text("Logistics Dispatch Lead").FontSize(8).AlignCenter();
                        });

                        sigRow.ConstantItem(40);

                        sigRow.RelativeItem().Column(c =>
                        {
                            sigRow.RelativeItem().Column(c2 =>
                            {
                                c2.Item().Text("Pallet Staging Bay:").FontSize(8).FontColor(Colors.Grey.Medium);
                                c2.Item().PaddingTop(25).LineHorizontal(1).LineColor(Colors.Grey.Medium);
                                c2.Item().PaddingTop(2).Text("Outbound Dock Area #1").FontSize(8).AlignCenter();
                            });
                        });
                    });
                });

                // ── FOOTER ──────────────────────────────────────────────────
                page.Footer().AlignCenter().Text(x =>
                {
                    x.Span("WMS Pro S-Shape Path Optimization Engine • Wave Pick Ticket • Page ");
                    x.CurrentPageNumber();
                    x.Span(" of ");
                    x.TotalPages();
                });
            });
        });

        return document.GeneratePdf();
    }
}
