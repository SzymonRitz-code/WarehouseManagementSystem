using WarehouseManagementSystem.Domain.Model.CatalogDomain;
using WarehouseManagementSystem.Domain.Model.DocumentsDomain;
using WarehouseManagementSystem.Domain.Model.InventoryDomain;
using WarehouseManagementSystem.Domain.Model.WarehouseDomain;

namespace WarehouseManagementSystem.API.Services.AuditLogs;

public static class AuditSnapshots
{
    public static object Product(Product product) => new
    {
        product.Id,
        Sku = product.SKU,
        product.Name,
        product.Description,
        product.Unit,
        product.RequiresBatch,
        product.IsActive,
        product.Weight,
        product.Volume,
        product.CreatedAt
    };

    public static object ProductBatch(ProductBatch batch) => new
    {
        batch.Id,
        batch.ProductId,
        batch.BatchNumber,
        batch.ManufacturedDate,
        batch.ExpirationDate,
        batch.CreatedAt
    };

    public static object Warehouse(Warehouse warehouse) => new
    {
        warehouse.Id,
        warehouse.Code,
        warehouse.Name,
        warehouse.Country,
        warehouse.City,
        warehouse.Address,
        warehouse.IsActive,
        warehouse.CreatedAt
    };

    public static object WarehouseZone(WarehouseZone zone) => new
    {
        zone.Id,
        zone.Code,
        zone.Name,
        zone.TemperatureType,
        zone.IsPickingZone,
        zone.WarehouseId,
        zone.CreatedAt
    };

    public static object Stock(Stock stock) => new
    {
        stock.Id,
        stock.ProductId,
        stock.WarehouseId,
        stock.WarehouseZoneId,
        stock.ProductBatchId,
        stock.QuantityTotal,
        stock.QuantityReserved,
        stock.Available,
        stock.LastUpdated
    };

    public static object StockReservation(StockReservation reservation) => new
    {
        reservation.Id,
        reservation.StockId,
        reservation.Quantity,
        reservation.Status,
        reservation.ReservationSource,
        reservation.CreatedByUser,
        reservation.CreatedAt,
        reservation.ExpiresAt
    };

    public static object Document(Document document) => new
    {
        document.Id,
        document.Number,
        document.DocumentDate,
        document.Type,
        document.Status,
        document.Notes,
        document.CreatedAt,
        document.ConfirmedAt,
        CreatedBy = document.CreatedByUser,
        ConfirmedBy = document.ConfirmedByUser,
        document.TransferStartedAt,
        document.SourceWarehouseId,
        document.TargetWarehouseId,
        Items = document.Items
            .OrderBy(i => i.Id)
            .Select(DocumentItem)
            .ToList()
    };

    public static object DocumentItem(DocumentItem item) => new
    {
        item.Id,
        item.ProductId,
        item.ProductBatchId,
        item.Quantity,
        item.SourceZoneId,
        item.TargetZoneId
    };
}
