namespace WMSPro.Application.Picking.Services;

public class PickPathOptimizer : IPickPathOptimizer
{
    public IReadOnlyList<OptimizedPickStop> OptimizeRoute(IEnumerable<PickLocationCandidate> candidates)
    {
        var candidateList = candidates.ToList();
        if (!candidateList.Any())
            return Array.Empty<OptimizedPickStop>();

        var assigned = candidateList.Where(c => c.StorageLocationId.HasValue).ToList();
        var unassigned = candidateList.Where(c => !c.StorageLocationId.HasValue).ToList();

        var optimizedStops = new List<OptimizedPickStop>();
        int sequenceCounter = 1;

        // Group by Zone
        var zoneGroups = assigned
            .GroupBy(c => c.Zone, StringComparer.OrdinalIgnoreCase)
            .OrderBy(g => g.Key);

        foreach (var zoneGroup in zoneGroups)
        {
            // Distinct aisles in natural alphanumeric order
            var distinctAisles = zoneGroup
                .Select(c => c.Aisle)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(a => a, StringComparer.OrdinalIgnoreCase)
                .ToList();

            for (int aisleIndex = 0; aisleIndex < distinctAisles.Count; aisleIndex++)
            {
                var aisleName = distinctAisles[aisleIndex];
                var aisleItems = zoneGroup.Where(c => string.Equals(c.Aisle, aisleName, StringComparison.OrdinalIgnoreCase));

                // S-Shape Snake Path logic:
                // Even aisle index -> ascending rack, shelf, bin
                // Odd aisle index -> descending rack, shelf, bin
                bool isEvenAisle = (aisleIndex % 2 == 0);

                IEnumerable<PickLocationCandidate> sortedAisleItems;
                if (isEvenAisle)
                {
                    sortedAisleItems = aisleItems
                        .OrderBy(c => c.Rack, StringComparer.OrdinalIgnoreCase)
                        .ThenBy(c => c.ShelfLevel)
                        .ThenBy(c => c.BinNumber, StringComparer.OrdinalIgnoreCase);
                }
                else
                {
                    sortedAisleItems = aisleItems
                        .OrderByDescending(c => c.Rack, StringComparer.OrdinalIgnoreCase)
                        .ThenByDescending(c => c.ShelfLevel)
                        .ThenByDescending(c => c.BinNumber, StringComparer.OrdinalIgnoreCase);
                }

                foreach (var item in sortedAisleItems)
                {
                    optimizedStops.Add(new OptimizedPickStop(
                        item.OrderId,
                        item.OrderNumber,
                        item.ProductId,
                        item.ProductName,
                        item.ProductSku,
                        item.StorageLocationId,
                        item.LocationCode,
                        item.Zone,
                        item.Aisle,
                        item.Rack,
                        item.ShelfLevel,
                        item.BinNumber,
                        sequenceCounter++,
                        item.QuantityRequested));
                }
            }
        }

        // Append unassigned items at the end with general floor location
        foreach (var item in unassigned)
        {
            optimizedStops.Add(new OptimizedPickStop(
                item.OrderId,
                item.OrderNumber,
                item.ProductId,
                item.ProductName,
                item.ProductSku,
                null,
                "FLOOR-RECEIVING",
                "General Floor",
                "FL0",
                "R0",
                0,
                "B0",
                sequenceCounter++,
                item.QuantityRequested));
        }

        return optimizedStops;
    }
}
