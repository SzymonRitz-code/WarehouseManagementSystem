namespace WarehouseManagementSystem.API.Caching;

/// <summary>
/// Defines the cache invalidation matrix for different entity mutations in the Warehouse Management System.
/// </summary>
public static class CacheInvalidationMatrix
{
    /// <summary>
    /// Defines the cache regions that should be invalidated when a product is mutated (created, updated, or deleted).
    /// </summary>
    public static readonly string[] ProductMutation =
    [
        CacheRegions.Products,
        CacheRegions.Stocks,
        CacheRegions.ProductBatches,
        CacheRegions.AuditLogs
    ];

    /// <summary>
    /// Defines the cache regions that should be invalidated when a product batch is mutated (created, updated, or deleted).
    /// </summary>
    public static readonly string[] ProductBatchMutation =
    [
        CacheRegions.ProductBatches,
        CacheRegions.Stocks,
        CacheRegions.AuditLogs
    ];

    /// <summary>
    /// Defines the cache regions that should be invalidated when a warehouse is mutated (created, updated, or deleted).
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
    /// Defines the cache regions that should be invalidated when a warehouse zone is mutated (created, updated, or deleted).
    /// </summary>
    public static readonly string[] WarehouseZoneMutation =
    [
        CacheRegions.WarehouseZones,
        CacheRegions.Warehouses,
        CacheRegions.Stocks,
        CacheRegions.AuditLogs
    ];

    /// <summary>
    /// Defines the cache regions that should be invalidated when a document is created or updated.
    /// </summary>
    public static readonly string[] DocumentCreateOrUpdate =
    [
        CacheRegions.Documents,
        CacheRegions.AuditLogs
    ];

    /// <summary>
    /// Defines the cache regions that should be invalidated when a document is confirmed or canceled.
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
