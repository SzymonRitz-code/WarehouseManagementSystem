using System.Linq.Expressions;
using WarehouseManagementSystem.API.DTO;
using WarehouseManagementSystem.Domain.Model.InventoryDomain;

namespace WarehouseManagementSystem.API.Services.Queries
{
    public interface IProductBatchQueryService
    {
        Task<IEnumerable<ProductBatchListDto>> GetProductBatchList(Expression<Func<ProductBatch, bool>>? predicate = null, CancellationToken ct = default);
        Task<ProductBatchDto?> GetProductBatchDetails(Guid batchId, CancellationToken ct = default);
    }
}
