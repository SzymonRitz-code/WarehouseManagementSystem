using Microsoft.EntityFrameworkCore;
using WarehouseManagementSystem.API.Caching;
using WarehouseManagementSystem.API.DTO;
using WarehouseManagementSystem.Domain.Enums;
using WarehouseManagementSystem.Domain.Model.DocumentsDomain;
using WarehouseManagementSystem.Domain.Model.InventoryDomain;
using WarehouseManagementSystem.Infrastructure.Persistence;

namespace WarehouseManagementSystem.API.Services.Documents.Query;

public class DocumentQueryService : IDocumentQueryService
{
    #region Fields and Constructor

    private const string ContractVersion = "v1";

    private readonly WarehouseManagementSystemDbContext _context;
    private readonly IQueryCacheService _queryCache;

    public DocumentQueryService(WarehouseManagementSystemDbContext context, IQueryCacheService queryCache)
    {
        _context = context;
        _queryCache = queryCache;
    }

    public DocumentQueryService(WarehouseManagementSystemDbContext context)
        : this(context, new NoOpQueryCacheService())
    {
    }

    #endregion

    #region Paged Query Operations

    public async Task<PagedResult<DocumentListDto>> GetDocumentsPageAsync(DocumentListQuery query, CancellationToken ct = default)
    {
        var parameters = new Dictionary<string, string>
        {
            ["page"] = CacheKeyNormalizer.NormalizeInt(query.Page),
            ["pageSize"] = CacheKeyNormalizer.NormalizeInt(query.PageSize),
            ["search"] = CacheKeyNormalizer.NormalizeString(query.Search),
            ["type"] = CacheKeyNormalizer.NormalizeEnum(query.Type),
            ["status"] = CacheKeyNormalizer.NormalizeEnum(query.Status),
            ["warehouseId"] = CacheKeyNormalizer.NormalizeGuid(query.WarehouseId),
            ["createdFrom"] = CacheKeyNormalizer.NormalizeDate(query.CreatedFrom),
            ["createdTo"] = CacheKeyNormalizer.NormalizeDate(query.CreatedTo),
            ["sortBy"] = CacheKeyNormalizer.NormalizeSort(query.SortBy),
            ["sortDirection"] = CacheKeyNormalizer.NormalizeSort(query.SortDirection)
        };

        var result = await _queryCache.GetOrCreateAsync(
            CacheRegions.Documents,
            ContractVersion,
            parameters,
            async token =>
            {
                var documents = BuildDocumentListQuery();

                documents = ApplyDocumentListSearch(documents, query);

                var totalItems = await documents.CountAsync(token);
                var orderedDocuments = ApplyDocumentListSorting(documents, query.SortBy, query.SortDirection);

                var pagedDocuments = orderedDocuments
                    .Skip((query.Page - 1) * query.PageSize)
                    .Take(query.PageSize);

                var items = await (
                    from document in pagedDocuments
                    join sourceWarehouse in _context.Warehouses.AsNoTracking()
                        on document.SourceWarehouseId equals sourceWarehouse.Id into sourceJoin
                    from sourceWarehouse in sourceJoin.DefaultIfEmpty()
                    join targetWarehouse in _context.Warehouses.AsNoTracking()
                        on document.TargetWarehouseId equals targetWarehouse.Id into targetJoin
                    from targetWarehouse in targetJoin.DefaultIfEmpty()
                    join item in _context.DocumentItems.AsNoTracking()
                        on document.Id equals item.DocumentId into itemJoin
                    select new DocumentListDto
                    {
                        Id = document.Id,
                        DocumentNumber = document.DocumentNumber,
                        Type = document.Type,
                        Status = document.Status,
                        SourceWarehouse = sourceWarehouse != null ? sourceWarehouse.Name : string.Empty,
                        DestinationWarehouse = targetWarehouse != null ? targetWarehouse.Name : null,
                        CreatedBy = document.CreatedBy,
                        ApprovedBy = document.ApprovedBy,
                        CreatedAt = document.CreatedAt,
                        ApprovedAt = document.ApprovedAt,
                        ItemCount = itemJoin.Count(),
                        TotalQuantity = itemJoin.Sum(item => (decimal?)item.Quantity) ?? 0
                    }).ToListAsync(token);

                return new PagedResult<DocumentListDto>
                {
                    Items = items,
                    Page = query.Page,
                    PageSize = query.PageSize,
                    TotalItems = totalItems
                };
            },
            ct);

        return result ?? new PagedResult<DocumentListDto>
        {
            Items = Array.Empty<DocumentListDto>(),
            Page = query.Page,
            PageSize = query.PageSize,
            TotalItems = 0
        };
    }

    #endregion

    #region Document Lookup Operations

    public async Task<Document?> GetByIdAsync(Guid documentId, CancellationToken ct = default)
    {
        return await _context.Documents
            .Include(d => d.SourceWarehouse)
            .Include(d => d.TargetWarehouse)
            .Include(d => d.Items)
                .ThenInclude(i => i.Product)
            .Include(d => d.Items)
                .ThenInclude(i => i.ProductBatch)
            .Include(d => d.Items)
                .ThenInclude(i => i.SourceZone)
            .Include(d => d.Items)
                .ThenInclude(i => i.TargetZone)
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.Id == documentId, ct);
    }

    public async Task<Document?> GetByNumberAsync(string number, CancellationToken ct = default)
    {
        return await _context.Documents
            .Include(d => d.Items)
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.Number == number, ct);
    }

    public async Task<IReadOnlyList<Document>> GetByStatusAsync(
        DocumentStatus status,
        CancellationToken ct = default)
    {
        return await _context.Documents
            .Where(d => d.Status == status)
            .OrderByDescending(d => d.CreatedAt)
            .AsNoTracking()
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<Document>> GetByTypeAsync(
        DocumentType type,
        CancellationToken ct = default)
    {
        return await _context.Documents
            .Where(d => d.Type == type)
            .OrderByDescending(d => d.CreatedAt)
            .AsNoTracking()
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<Document>> GetByWarehouseAsync(
        Guid warehouseId,
        CancellationToken ct = default)
    {
        return await _context.Documents
            .Where(d =>
                d.SourceWarehouseId == warehouseId ||
                d.TargetWarehouseId == warehouseId)
            .OrderByDescending(d => d.CreatedAt)
            .AsNoTracking()
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<Document>> GetBetweenDatesAsync(
        DateTimeOffset from,
        DateTimeOffset to,
        CancellationToken ct = default)
    {
        return await _context.Documents
            .Where(d => d.CreatedAt >= from && d.CreatedAt <= to)
            .OrderByDescending(d => d.CreatedAt)
            .AsNoTracking()
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<Document>> GetPagedAsync(
        int page,
        int pageSize,
        CancellationToken ct = default)
    {
        return await _context.Documents
            .OrderByDescending(d => d.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .AsNoTracking()
            .ToListAsync(ct);
    }

    public async Task<bool> ExistsAsync(Guid documentId, CancellationToken ct = default)
    {
        return await _context.Documents.AsNoTracking()
            .AnyAsync(d => d.Id == documentId, ct);
    }

    #endregion

    #region Status and Workflow Query Operations

    public async Task<IReadOnlyList<Document>> GetByTypeAndStatusAsync(
        DocumentType type,
        DocumentStatus status,
        CancellationToken ct = default)
    {
        return await _context.Documents
            .Where(d => d.Type == type && d.Status == status)
            .OrderByDescending(d => d.CreatedAt)
            .AsNoTracking()
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<Document>> GetDraftsAsync(CancellationToken ct = default)
    {
        return await _context.Documents
            .Where(d => d.Status == DocumentStatus.Draft)
            .OrderByDescending(d => d.CreatedAt)
            .AsNoTracking()
            .ToListAsync(ct);
    }
    [Obsolete]
    public async Task<IReadOnlyList<Document>> GetPendingConfirmationAsync(CancellationToken ct = default)
    {
        // Dokumenty w trakcie procesu operacyjnego
        return await _context.Documents
            .Where(d => d.Status == DocumentStatus.Draft)
            .OrderBy(d => d.CreatedAt)
            .AsNoTracking()
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<DocumentListDto>> GetPendingDocumentsAsync(CancellationToken ct = default)
    {
        var parameters = new Dictionary<string, string>
        {
            ["scope"] = "pending"
        };

        return await _queryCache.GetOrCreateAsync(
                   CacheRegions.Documents,
                   ContractVersion,
                   parameters,
                   async token => await (
                       from document in _context.Documents.AsNoTracking()
                       join item in _context.DocumentItems.AsNoTracking() on document.Id equals item.DocumentId into itemJoin
                       from item in itemJoin.DefaultIfEmpty()
                       join sourceWarehouse in _context.Warehouses.AsNoTracking() on document.SourceWarehouseId equals sourceWarehouse.Id
                       join targetWarehouse in _context.Warehouses.AsNoTracking() on document.TargetWarehouseId equals targetWarehouse.Id into targetJoin
                       from targetWarehouse in targetJoin.DefaultIfEmpty()
                       where document.Status == DocumentStatus.Draft
                       group new { document, item, sourceWarehouse, targetWarehouse }
                       by new
                       {
                           document.Id,
                           document.Number,
                           document.Type,
                           document.Status,
                           SourceWarehouseName = sourceWarehouse.Name,
                           TargetWarehouseName = targetWarehouse != null ? targetWarehouse.Name : null,
                           CreatedByName = document.CreatedByUser != null ? document.CreatedByUser.Name : null,
                           ConfirmedByName = document.ConfirmedByUser != null ? document.ConfirmedByUser.Name : null,
                           document.CreatedAt,
                           document.ConfirmedAt
                       }
                       into g
                       select new DocumentListDto
                       {
                           Id = g.Key.Id,
                           DocumentNumber = g.Key.Number,
                           Type = g.Key.Type,
                           Status = g.Key.Status,
                           SourceWarehouse = g.Key.SourceWarehouseName,
                           DestinationWarehouse = g.Key.TargetWarehouseName,
                           CreatedBy = g.Key.CreatedByName,
                           ApprovedBy = g.Key.ConfirmedByName,
                           CreatedAt = g.Key.CreatedAt,
                           ApprovedAt = g.Key.ConfirmedAt,
                           ItemCount = g.Count(x => x.item != null),
                           TotalQuantity = g.Sum(x => (decimal?)x.item.Quantity) ?? 0
                       }
                       ).OrderBy(d => d.CreatedAt).AsNoTracking().ToListAsync(token),
                   ct)
               ?? new List<DocumentListDto>();
    }

    public async Task<IReadOnlyList<StockReservation>> GetActiveReservationsAsync(
        Guid documentId,
        CancellationToken ct = default)
    {
        return await (
            from item in _context.DocumentItems.AsNoTracking()
            join stock in _context.Stocks.AsNoTracking()
                on new { item.ProductId, item.ProductBatchId }
                equals new { stock.ProductId, stock.ProductBatchId }
            join reservation in _context.StockReservations.AsNoTracking()
                on stock.Id equals reservation.StockId
            join document in _context.Documents.AsNoTracking()
                on item.DocumentId equals document.Id
            where item.DocumentId == documentId
                  && reservation.Status == ReservationStatus.Active
                  && (
                        (document.SourceWarehouseId.HasValue &&
                         stock.WarehouseId == document.SourceWarehouseId &&
                         stock.WarehouseZoneId == item.SourceZoneId)
                     ||
                        (document.TargetWarehouseId.HasValue &&
                         stock.WarehouseId == document.TargetWarehouseId &&
                         stock.WarehouseZoneId == item.TargetZoneId)
                     )
            select reservation
        ).AsNoTracking().ToListAsync(ct);
    }

    public async Task<bool> HasActiveReservationsAsync(
        Guid documentId,
        CancellationToken ct = default)
    {
        return await (
            from item in _context.DocumentItems.AsNoTracking()
            join stock in _context.Stocks.AsNoTracking()
                on new { item.ProductId, item.ProductBatchId }
                equals new { stock.ProductId, stock.ProductBatchId }
            join reservation in _context.StockReservations.AsNoTracking()
                on stock.Id equals reservation.StockId
            join document in _context.Documents.AsNoTracking()
                on item.DocumentId equals document.Id
            where item.DocumentId == documentId
                  && reservation.Status == ReservationStatus.Active
                  && (
                        (document.SourceWarehouseId.HasValue &&
                         stock.WarehouseId == document.SourceWarehouseId &&
                         stock.WarehouseZoneId == item.SourceZoneId)
                     ||
                        (document.TargetWarehouseId.HasValue &&
                         stock.WarehouseId == document.TargetWarehouseId &&
                         stock.WarehouseZoneId == item.TargetZoneId)
                     )
            select reservation.Id
        ).AnyAsync(ct);
    }

    #endregion

    #region Recent Query Operations

    public async Task<IReadOnlyList<Document>> GetRecentAsync(
        int take,
        CancellationToken ct = default)
    {
        return await _context.Documents
            .OrderByDescending(d => d.CreatedAt)
            .Take(take)
            .AsNoTracking()
            .ToListAsync(ct);
    }

    #endregion

    #region Query Helpers

    private IQueryable<DocumentListQueryRow> BuildDocumentListQuery()
    {
        return _context.Documents
            .AsNoTracking()
            .Select(document => new DocumentListQueryRow
            {
                Id = document.Id,
                DocumentNumber = document.Number,
                Type = document.Type,
                Status = document.Status,
                SourceWarehouseId = document.SourceWarehouseId,
                TargetWarehouseId = document.TargetWarehouseId,
                CreatedBy = document.CreatedByUser.Name,
                ApprovedBy = document.ConfirmedByUser != null ? document.ConfirmedByUser.Name : null,
                CreatedAt = document.CreatedAt,
                ApprovedAt = document.ConfirmedAt
            });
    }

    private static IQueryable<DocumentListQueryRow> ApplyDocumentListSearch(
        IQueryable<DocumentListQueryRow> documents,
        DocumentListQuery query)
    {
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();

            documents = documents.Where(d =>
                (d.DocumentNumber != null && d.DocumentNumber.Contains(search)) ||
                (d.CreatedBy != null && d.CreatedBy.Contains(search)) ||
                (d.ApprovedBy != null && d.ApprovedBy.Contains(search)));
        }

        if (query.Type.HasValue)
        {
            documents = documents.Where(d => d.Type == query.Type.Value);
        }

        if (query.Status.HasValue)
        {
            documents = documents.Where(d => d.Status == query.Status.Value);
        }

        if (query.WarehouseId.HasValue)
        {
            var warehouseId = query.WarehouseId.Value;
            documents = documents.Where(d => d.SourceWarehouseId == warehouseId || d.TargetWarehouseId == warehouseId);
        }

        if (query.CreatedFrom.HasValue)
        {
            documents = documents.Where(d => d.CreatedAt >= query.CreatedFrom.Value);
        }

        if (query.CreatedTo.HasValue)
        {
            documents = documents.Where(d => d.CreatedAt < query.CreatedTo.Value.AddDays(1));
        }

        return documents;
    }

    private static IQueryable<DocumentListQueryRow> ApplyDocumentListSorting(
        IQueryable<DocumentListQueryRow> documents,
        string? sortBy,
        string? sortDirection)
    {
        var descending = string.Equals(sortDirection, "desc", StringComparison.OrdinalIgnoreCase);
        var sortKey = sortBy?.Trim().ToLowerInvariant();

        return sortKey switch
        {
            "documentnumber" => descending
                ? documents.OrderByDescending(d => d.DocumentNumber).ThenByDescending(d => d.CreatedAt)
                : documents.OrderBy(d => d.DocumentNumber).ThenByDescending(d => d.CreatedAt),
            "type" => descending
                ? documents.OrderByDescending(d => d.Type).ThenByDescending(d => d.CreatedAt)
                : documents.OrderBy(d => d.Type).ThenByDescending(d => d.CreatedAt),
            "status" => descending
                ? documents.OrderByDescending(d => d.Status).ThenByDescending(d => d.CreatedAt)
                : documents.OrderBy(d => d.Status).ThenByDescending(d => d.CreatedAt),
            "createdby" => descending
                ? documents.OrderByDescending(d => d.CreatedBy).ThenByDescending(d => d.CreatedAt)
                : documents.OrderBy(d => d.CreatedBy).ThenByDescending(d => d.CreatedAt),
            "approvedby" => descending
                ? documents.OrderByDescending(d => d.ApprovedBy).ThenByDescending(d => d.CreatedAt)
                : documents.OrderBy(d => d.ApprovedBy).ThenByDescending(d => d.CreatedAt),
            "approvedat" => descending
                ? documents.OrderByDescending(d => d.ApprovedAt).ThenByDescending(d => d.CreatedAt)
                : documents.OrderBy(d => d.ApprovedAt).ThenByDescending(d => d.CreatedAt),
            _ => descending
                ? documents.OrderByDescending(d => d.CreatedAt)
                : documents.OrderBy(d => d.CreatedAt)
        };
    }

    private sealed class DocumentListQueryRow
    {
        public Guid Id { get; init; }
        public string? DocumentNumber { get; init; }
        public DocumentType Type { get; init; }
        public DocumentStatus Status { get; init; }
        public Guid? SourceWarehouseId { get; init; }
        public Guid? TargetWarehouseId { get; init; }
        public string? CreatedBy { get; init; }
        public string? ApprovedBy { get; init; }
        public DateTimeOffset CreatedAt { get; init; }
        public DateTimeOffset? ApprovedAt { get; init; }
    }

    #endregion

    private sealed class NoOpQueryCacheService : IQueryCacheService
    {
        public Task<T?> GetOrCreateAsync<T>(
            string region,
            string contractVersion,
            IReadOnlyDictionary<string, string> parameters,
            Func<CancellationToken, Task<T?>> factory,
            CancellationToken ct = default)
        {
            return factory(ct);
        }
    }

}
