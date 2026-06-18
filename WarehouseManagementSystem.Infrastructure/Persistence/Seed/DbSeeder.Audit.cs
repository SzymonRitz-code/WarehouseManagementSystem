using System.Globalization;
using WarehouseManagementSystem.Domain.Model.AuditDomain;
using WarehouseManagementSystem.Domain.Model.CatalogDomain;
using WarehouseManagementSystem.Domain.Model.DocumentsDomain;
using WarehouseManagementSystem.Domain.Model.InventoryDomain;
using WarehouseManagementSystem.Domain.Model.WarehouseDomain;
using WarehouseManagementSystem.Domain.ValueObjects;

namespace WarehouseManagementSystem.Infrastructure.Persistence.Seed;

public static partial class DbSeeder
{
    private static AuditLog CreateCreateAuditLog(Warehouse warehouse)
    {
        return CreateAuditLog(
            nameof(Warehouse),
            warehouse.Id,
            "Create",
            warehouse.CreatedByUser,
            $"{{\"code\":\"{warehouse.Code}\",\"name\":\"{warehouse.Name}\"}}");
    }

    private static AuditLog CreateCreateAuditLog(WarehouseZone zone)
    {
        return CreateAuditLog(
            nameof(WarehouseZone),
            zone.Id,
            "Create",
            zone.CreatedByUser,
            $"{{\"warehouseId\":\"{zone.WarehouseId}\",\"code\":\"{zone.Code}\",\"name\":\"{zone.Name}\"}}");
    }

    private static AuditLog CreateCreateAuditLog(Product product)
    {
        return CreateAuditLog(
            nameof(Product),
            product.Id,
            "Create",
            product.CreatedByUser,
            $"{{\"sku\":\"{product.SKU}\",\"name\":\"{product.Name}\",\"unit\":\"{product.Unit}\",\"requiresBatch\":{product.RequiresBatch.ToString().ToLowerInvariant()}}}");
    }

    private static AuditLog CreateCreateAuditLog(ProductBatch batch)
    {
        return CreateAuditLog(
            nameof(ProductBatch),
            batch.Id,
            "Create",
            batch.CreatedByUser,
            $"{{\"productId\":\"{batch.ProductId}\",\"batchNumber\":\"{batch.BatchNumber}\"}}");
    }

    private static AuditLog CreateCreateAuditLog(Stock stock, UserSnapshot performedBy)
    {
        return CreateAuditLog(
            nameof(Stock),
            stock.Id,
            "Create",
            performedBy,
            $"{{\"productId\":\"{stock.ProductId}\",\"warehouseId\":\"{stock.WarehouseId}\",\"warehouseZoneId\":\"{stock.WarehouseZoneId}\",\"productBatchId\":\"{stock.ProductBatchId}\",\"quantityTotal\":{stock.QuantityTotal.ToString(CultureInfo.InvariantCulture)}}}");
    }

    private static AuditLog CreateCreateAuditLog(Document document)
    {
        return CreateAuditLog(
            nameof(Document),
            document.Id,
            "Create",
            document.CreatedByUser,
            $"{{\"type\":\"{document.Type}\",\"status\":\"{document.Status}\",\"number\":\"{document.Number}\",\"itemCount\":{document.Items.Count}}}");
    }

    private static AuditLog CreateAuditLog(
        string entityName,
        Guid entityId,
        string operation,
        UserSnapshot performedBy,
        string newValues)
    {
        return new AuditLog
        {
            Id = Guid.NewGuid(),
            EntityName = entityName,
            EntityId = entityId,
            Operation = operation,
            OldValues = string.Empty,
            NewValues = newValues,
            PerformedAt = DateTimeOffset.UtcNow,
            IpAddress = "seed",
            PerformedById = performedBy.Id,
            PerformedBy = new UserSnapshot(performedBy.Id, performedBy.Email, performedBy.Name)
        };
    }
}
