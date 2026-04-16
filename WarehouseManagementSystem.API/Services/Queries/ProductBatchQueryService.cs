using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using WarehouseManagementSystem.API.DTO;
using WarehouseManagementSystem.Domain.Model.InventoryDomain;
using WarehouseManagementSystem.Infrastructure.Persistence;

namespace WarehouseManagementSystem.API.Services.Queries
{
    public class ProductBatchQueryService : IProductBatchQueryService
    {
        private readonly WarehouseManagementSystemDbContext _context;

        public ProductBatchQueryService(WarehouseManagementSystemDbContext context)
        {
            _context = context;
        }
        public async Task<IEnumerable<ProductBatchListDto>> GetProductBatchList(Expression<Func<ProductBatch,bool>>? predicate = null, CancellationToken ct = default)
        {
            var query = _context.ProductBatches.AsNoTracking();
            
            if(predicate != null)
            {
                query = query.Where(predicate);
            }

            return await (
                from pb in query
                join s in _context.Stocks.AsNoTracking() on pb.Id equals s.ProductBatchId into stockGroup
                from s in stockGroup.DefaultIfEmpty()
                group s by new { pb.Id, pb.BatchNumber, pb.Product.Name, pb.ExpirationDate, pb.CreatedAt } into g
                select new ProductBatchListDto(
                    g.Key.Id,
                    g.Key.BatchNumber,
                    g.Key.Name,
                    g.Key.ExpirationDate,
                    (int)g.Sum(s => s != null ? s.QuantityTotal : 0),
                    (int)g.Sum(s => s != null ? s.QuantityTotal - s.QuantityReserved : 0),
                    (int)g.Sum(s => s != null ? s.QuantityReserved : 0),
                    g.Key.CreatedAt
                )
            ).ToListAsync(ct);
        }
    }
}
