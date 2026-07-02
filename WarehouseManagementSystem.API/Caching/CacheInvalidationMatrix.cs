namespace WarehouseManagementSystem.API.Caching;

/// <summary>
/// Maps domain mutations to the read regions whose cached projections may become stale.
/// </summary>
public static class CacheInvalidationMatrix
{
    /// <summary>
    /// Regions affected by product mutations.
    /// </summary>
    public static readonly string[] ProductMutation =
    [
        CacheRegions.Products,
        CacheRegions.Stocks,
        CacheRegions.ProductBatches,
        CacheRegions.AuditLogs
    ];

    /// <summary>
    /// Regions affected by product batch mutations.
    /// </summary>
    public static readonly string[] ProductBatchMutation =
    [
        CacheRegions.ProductBatches,
        CacheRegions.Stocks,
        CacheRegions.AuditLogs
    ];

    /// <summary>
    /// Regions affected by warehouse mutations.
    /// </summary>
    public static readonly string[] WarehouseMutation =
    [
        CacheRegions.Warehouses,
        CacheRegions.WarehouseZones,
        CacheRegions.Stocks,
        CacheRegions.Documents,
        CacheRegions.AuditLogs
    ];

    /// <summary>
    /// Regions affected by warehouse zone mutations.
    /// </summary>
    public static readonly string[] WarehouseZoneMutation =
    [
        CacheRegions.WarehouseZones,
        CacheRegions.Warehouses,
        CacheRegions.Stocks,
        CacheRegions.AuditLogs
    ];

    /// <summary>
    /// Regions affected by document create and update operations.
    /// </summary>
    public static readonly string[] DocumentCreateOrUpdate =
    [
        CacheRegions.Documents,
        CacheRegions.AuditLogs
    ];

    /// <summary>
    /// Regions affected by document confirm and cancel operations.
    /// </summary>
    public static readonly string[] DocumentConfirmOrCancel =
    [
        CacheRegions.Documents,
        CacheRegions.Stocks,
        CacheRegions.Warehouses,
        CacheRegions.WarehouseZones,
        CacheRegions.ProductBatches,
        CacheRegions.AuditLogs
    ];
}
