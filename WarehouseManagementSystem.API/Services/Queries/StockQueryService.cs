using Microsoft.EntityFrameworkCore;
using WarehouseManagementSystem.API.DTO;
using WarehouseManagementSystem.Domain.Enums;
using WarehouseManagementSystem.Domain.Model.InventoryDomain;
using WarehouseManagementSystem.Infrastructure.Persistence;

namespace WarehouseManagementSystem.API.Services.Queries;

public class StockQueryService : IStockQueryService
{
    #region Fields and Constructor

    private readonly WarehouseManagementSystemDbContext _context;

    public StockQueryService(WarehouseManagementSystemDbContext context)
    {
        _context = context;
    }

    #endregion

    #region Stock DTO Query Operations

    public async Task<PagedResult<StockDto>> GetStocksAsync(StockListQuery query, CancellationToken ct = default)
    {
        var stocks = BuildStockListQuery();

        stocks = ApplyStockListSearch(stocks, query);

        var totalItems = await stocks.CountAsync(ct);
        var orderedStocks = ApplyStockListSorting(stocks, query.SortBy, query.SortDirection);

        var pagedStocks = orderedStocks
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize);

        var items = await (
            from stock in pagedStocks
            join product in _context.Products.AsNoTracking() on stock.ProductId equals product.Id
            join warehouse in _context.Warehouses.AsNoTracking() on stock.WarehouseId equals warehouse.Id
            join zone in _context.WarehouseZones.AsNoTracking() on stock.WarehouseZoneId equals zone.Id
            join batch in _context.ProductBatches.AsNoTracking() on stock.ProductBatchId equals batch.Id into batches
            from batch in batches.DefaultIfEmpty()
            select new StockDto
            {
                Id = stock.Id,
                ProductBatchNumber = batch != null ? batch.BatchNumber : null,
                QuantityTotal = stock.QuantityTotal,
                QuantityReserved = stock.QuantityReserved,
                QuantityAvailable = stock.QuantityTotal - stock.QuantityReserved,
                LastUpdated = stock.LastUpdated,
                ProductId = stock.ProductId,
                ProductSku = product.SKU,
                ProductName = product.Name,
                WarehouseId = stock.WarehouseId,
                WarehouseName = warehouse.Name,
                ZoneId = stock.WarehouseZoneId,
                ZoneName = zone.Name,
                Unit = product.Unit.ToString()
            })
            .ToListAsync(ct);

        return new PagedResult<StockDto>
        {
            Items = items,
            Page = query.Page,
            PageSize = query.PageSize,
            TotalItems = totalItems
        };
    }

    public async Task<List<StockDto>> GetStockAvailabilityAsync(CancellationToken ct = default)
    {
        return await (
            from stock in _context.Stocks.AsNoTracking()
            join product in _context.Products.AsNoTracking() on stock.ProductId equals product.Id
            join warehouse in _context.Warehouses.AsNoTracking() on stock.WarehouseId equals warehouse.Id
            join zone in _context.WarehouseZones.AsNoTracking() on stock.WarehouseZoneId equals zone.Id
            select new StockDto
            {
                Id = stock.Id,
                ProductBatchNumber = null,
                QuantityTotal = stock.QuantityTotal,
                QuantityReserved = stock.QuantityReserved,
                QuantityAvailable = stock.QuantityTotal - stock.QuantityReserved,
                LastUpdated = stock.LastUpdated,
                ProductId = stock.ProductId,
                ProductSku = product.SKU,
                ProductName = product.Name,
                WarehouseId = stock.WarehouseId,
                WarehouseName = warehouse.Name,
                ZoneId = stock.WarehouseZoneId,
                ZoneName = zone.Name,
                Unit = product.Unit.ToString()
            })
            .ToListAsync(ct);
    }

    public async Task<StockDto?> GetStockDetailsAsync(Guid stockId, CancellationToken ct = default)
    {
        return await (
            from stock in _context.Stocks.AsNoTracking()
            join product in _context.Products.AsNoTracking() on stock.ProductId equals product.Id
            join warehouse in _context.Warehouses.AsNoTracking() on stock.WarehouseId equals warehouse.Id
            join zone in _context.WarehouseZones.AsNoTracking() on stock.WarehouseZoneId equals zone.Id
            join batch in _context.ProductBatches.AsNoTracking() on stock.ProductBatchId equals batch.Id into batches
            from batch in batches.DefaultIfEmpty()
            where stock.Id == stockId
            select new StockDto
            {
                Id = stock.Id,
                ProductBatchNumber = batch != null ? batch.BatchNumber : null,
                QuantityTotal = stock.QuantityTotal,
                QuantityReserved = stock.QuantityReserved,
                QuantityAvailable = stock.QuantityTotal - stock.QuantityReserved,
                LastUpdated = stock.LastUpdated,
                ProductId = stock.ProductId,
                ProductSku = product.SKU,
                ProductName = product.Name,
                WarehouseId = stock.WarehouseId,
                WarehouseName = warehouse.Name,
                ZoneId = stock.WarehouseZoneId,
                ZoneName = zone.Name,
                Unit = product.Unit.ToString()
            })
            .FirstOrDefaultAsync(ct);
    }

    public async Task<IReadOnlyList<StockDto>> GetProductStocksAsync(Guid productId, CancellationToken ct = default)
    {
        return await (
            from stock in _context.Stocks.AsNoTracking()
            join product in _context.Products.AsNoTracking() on stock.ProductId equals product.Id
            join warehouse in _context.Warehouses.AsNoTracking() on stock.WarehouseId equals warehouse.Id
            join zone in _context.WarehouseZones.AsNoTracking() on stock.WarehouseZoneId equals zone.Id
            join batch in _context.ProductBatches.AsNoTracking() on stock.ProductBatchId equals batch.Id into batches
            from batch in batches.DefaultIfEmpty()
            where stock.ProductId == productId
            orderby warehouse.Name, zone.Name, batch.BatchNumber
            select new StockDto
            {
                Id = stock.Id,
                ProductBatchNumber = batch != null ? batch.BatchNumber : null,
                QuantityTotal = stock.QuantityTotal,
                QuantityReserved = stock.QuantityReserved,
                QuantityAvailable = stock.QuantityTotal - stock.QuantityReserved,
                LastUpdated = stock.LastUpdated,
                ProductId = stock.ProductId,
                ProductSku = product.SKU,
                ProductName = product.Name,
                WarehouseId = stock.WarehouseId,
                WarehouseName = warehouse.Name,
                ZoneId = stock.WarehouseZoneId,
                ZoneName = zone.Name,
                Unit = product.Unit.ToString()
            })
            .ToListAsync(ct);
    }

    #endregion

    #region Stock Lookup Operations

    public async Task<Stock?> GetByIdAsync(Guid stockId, CancellationToken ct = default)
    {
        return await _context.Stocks
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == stockId, ct);
    }

    public async Task<Stock?> GetStockAsync(
        Guid productId,
        Guid? batchId,
        Guid warehouseId,
        Guid? warehouseZoneId,
        CancellationToken ct = default)
    {
        var query = _context.Stocks
            .AsNoTracking()
            .Where(s =>
                s.ProductId == productId &&
                s.WarehouseId == warehouseId);

        if (batchId.HasValue)
        {
            query = query.Where(s => s.ProductBatchId == batchId.Value);
        }

        if (warehouseZoneId.HasValue)
        {
            query = query.Where(s => s.WarehouseZoneId == warehouseZoneId.Value);
        }

        return await query.FirstOrDefaultAsync(ct);
    }

    public async Task<IReadOnlyList<Stock>> GetByProductAsync(
        Guid productId,
        CancellationToken ct = default)
    {
        return await _context.Stocks
            .AsNoTracking()
            .Where(s => s.ProductId == productId)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<Stock>> GetByWarehouseAsync(
        Guid warehouseId,
        CancellationToken ct = default)
    {
        return await _context.Stocks
            .AsNoTracking()
            .Where(s => s.WarehouseId == warehouseId)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<Stock>> GetByWarehouseZoneAsync(
        Guid warehouseZoneId,
        CancellationToken ct = default)
    {
        return await _context.Stocks
            .AsNoTracking()
            .Where(s => s.WarehouseZoneId == warehouseZoneId)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<Stock>> GetByProductAndWarehouseAsync(
        Guid productId,
        Guid warehouseId,
        CancellationToken ct = default)
    {
        return await _context.Stocks
            .AsNoTracking()
            .Where(s =>
                s.ProductId == productId &&
                s.WarehouseId == warehouseId)
            .ToListAsync(ct);
    }

    #endregion

    #region Quantity and Availability Operations

    public async Task<decimal> GetAvailableQuantityAsync(
        Guid productId,
        Guid? batchId,
        Guid warehouseId,
        Guid? warehouseZoneId,
        CancellationToken ct = default)
    {
        var query = _context.Stocks
            .AsNoTracking()
            .Where(s =>
                s.ProductId == productId &&
                s.WarehouseId == warehouseId);

        if (batchId.HasValue)
        {
            query = query.Where(s => s.ProductBatchId == batchId.Value);
        }

        if (warehouseZoneId.HasValue)
        {
            query = query.Where(s => s.WarehouseZoneId == warehouseZoneId.Value);
        }

        return await query
            .Select(s => s.QuantityTotal - s.QuantityReserved)
            .DefaultIfEmpty(0m)
            .SumAsync(ct);
    }

    public async Task<decimal> GetTotalQuantityAsync(
        Guid productId,
        Guid warehouseId,
        CancellationToken ct = default)
    {
        return await _context.Stocks
            .AsNoTracking()
            .Where(s =>
                s.ProductId == productId &&
                s.WarehouseId == warehouseId)
            .Select(s => s.QuantityTotal)
            .DefaultIfEmpty(0m)
            .SumAsync(ct);
    }

    public async Task<bool> IsAvailableAsync(
        Guid productId,
        Guid warehouseId,
        Guid warehouseZoneId,
        decimal requiredQuantity,
        Guid? batchId,
        CancellationToken ct = default)
    {
        if (requiredQuantity <= 0)
        {
            throw new ArgumentException("Required quantity must be greater than zero.", nameof(requiredQuantity));
        }

        var available = await _context.Stocks
            .AsNoTracking()
            .Where(s =>
                s.ProductId == productId &&
                s.WarehouseId == warehouseId &&
                s.WarehouseZoneId == warehouseZoneId &&
                s.ProductBatchId == batchId)
            .Select(s => s.QuantityTotal - s.QuantityReserved)
            .FirstOrDefaultAsync(ct);

        return available >= requiredQuantity;
    }

    #endregion

    #region Reservation and Picking Query Operations

    public async Task<IReadOnlyList<Stock>> GetStocksWithActiveReservationsAsync(
        CancellationToken ct = default)
    {
        return await _context.Stocks
            .AsNoTracking()
            .Where(s => s.Reservations.Any(r => r.Status == ReservationStatus.Active))
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<Stock>> GetByTemperatureAsync(
        TemperatureType temperatureType,
        CancellationToken ct = default)
    {
        return await _context.Stocks
            .AsNoTracking()
            .Include(s => s.WarehouseZone)
            .Where(s => s.WarehouseZone.TemperatureType == temperatureType)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<Stock>> GetAvailableForPickingAsync(
        Guid warehouseId,
        CancellationToken ct = default)
    {
        return await _context.Stocks
            .AsNoTracking()
            .Where(s =>
                s.WarehouseId == warehouseId &&
                (s.QuantityTotal - s.QuantityReserved) > 0)
            .OrderByDescending(s => s.QuantityTotal - s.QuantityReserved)
            .ToListAsync(ct);
    }

    #endregion

    #region Query Helpers

    private IQueryable<Stock> BuildStockListQuery()
    {
        return _context.Stocks.AsNoTracking();
    }

    private IQueryable<Stock> ApplyStockListSearch(
        IQueryable<Stock> stocks,
        StockListQuery query)
    {
        if (query.WarehouseId.HasValue)
        {
            stocks = stocks.Where(s => s.WarehouseId == query.WarehouseId.Value);
        }

        if (query.ZoneId.HasValue)
        {
            stocks = stocks.Where(s => s.WarehouseZoneId == query.ZoneId.Value);
        }

        if (query.AvailableOnly == true)
        {
            stocks = stocks.Where(s => s.QuantityTotal - s.QuantityReserved > 0);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();

            stocks =
                from stock in stocks
                join product in _context.Products.AsNoTracking() on stock.ProductId equals product.Id
                join batch in _context.ProductBatches.AsNoTracking() on stock.ProductBatchId equals batch.Id into batches
                from batch in batches.DefaultIfEmpty()
                where product.SKU.Contains(search)
                      || product.Name.Contains(search)
                      || (batch != null && batch.BatchNumber.Contains(search))
                select stock;
        }

        return stocks;
    }

    private static IQueryable<Stock> ApplyStockListSorting(
        IQueryable<Stock> stocks,
        string? sortBy,
        string? sortDirection)
    {
        var descending = string.Equals(sortDirection, "desc", StringComparison.OrdinalIgnoreCase);
        var sortKey = sortBy?.Trim().ToLowerInvariant();

        return sortKey switch
        {
            "quantityavailable" => descending
                ? stocks.OrderByDescending(s => s.QuantityTotal - s.QuantityReserved).ThenByDescending(s => s.LastUpdated)
                : stocks.OrderBy(s => s.QuantityTotal - s.QuantityReserved).ThenByDescending(s => s.LastUpdated),
            "quantityreserved" => descending
                ? stocks.OrderByDescending(s => s.QuantityReserved).ThenByDescending(s => s.LastUpdated)
                : stocks.OrderBy(s => s.QuantityReserved).ThenByDescending(s => s.LastUpdated),
            "quantitytotal" => descending
                ? stocks.OrderByDescending(s => s.QuantityTotal).ThenByDescending(s => s.LastUpdated)
                : stocks.OrderBy(s => s.QuantityTotal).ThenByDescending(s => s.LastUpdated),
            _ => descending
                ? stocks.OrderByDescending(s => s.LastUpdated)
                : stocks.OrderBy(s => s.LastUpdated)
        };
    }

    #endregion

}
