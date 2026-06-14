using Microsoft.EntityFrameworkCore;
using WarehouseManagementSystem.API.DTO;
using WarehouseManagementSystem.Infrastructure.Persistence;

namespace WarehouseManagementSystem.API.Services.Queries;

public class WarehouseQueryService : IWarehouseQueryService
{
    private readonly WarehouseManagementSystemDbContext _context;

    public WarehouseQueryService(WarehouseManagementSystemDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<WarehouseListDto>> GetWarehousesAsync(CancellationToken ct = default)
    {
        return await _context.Warehouses
            .AsNoTracking()
            .Select(w => new WarehouseListDto(
                w.Id,
                w.Code,
                w.Name,
                w.Country,
                w.Address,
                _context.WarehouseZones.Count(z => z.WarehouseId == w.Id),
                _context.Stocks.Count(s => s.WarehouseId == w.Id),
                _context.Stocks
                    .Where(s => s.WarehouseId == w.Id)
                    .Select(s => (decimal?)s.QuantityTotal)
                    .Sum() ?? 0m,
                w.IsActive,
                w.CreatedAt))
            .ToListAsync(ct);
    }

    public async Task<WarehouseDetailsDto?> GetWarehouseAsync(Guid warehouseId, CancellationToken ct = default)
    {
        return await _context.Warehouses
            .AsNoTracking()
            .Where(w => w.Id == warehouseId)
            .Select(w => new WarehouseDetailsDto
            {
                Id = w.Id,
                Code = w.Code,
                Name = w.Name,
                Country = w.Country,
                City = w.City,
                Address = w.Address,
                IsActive = w.IsActive
            })
            .FirstOrDefaultAsync(ct);
    }

    public async Task<IReadOnlyList<WarehouseZoneListDto>> GetWarehouseZonesAsync(CancellationToken ct = default)
    {
        return await _context.WarehouseZones
            .AsNoTracking()
            .Select(z => new WarehouseZoneListDto(
                z.Id,
                z.Code,
                z.Name,
                z.TemperatureType,
                z.IsPickingZone,
                z.WarehouseId,
                z.Warehouse.Name,
                _context.Stocks
                    .Where(s => s.WarehouseZoneId == z.Id)
                    .Select(s => (decimal?)s.QuantityTotal)
                    .Sum() ?? 0m,
                z.CreatedAt))
            .ToListAsync(ct);
    }

    public async Task<WarehouseZoneDetailsDto?> GetWarehouseZoneAsync(Guid warehouseZoneId, CancellationToken ct = default)
    {
        return await _context.WarehouseZones
            .AsNoTracking()
            .Where(z => z.Id == warehouseZoneId)
            .Select(z => new WarehouseZoneDetailsDto
            {
                Id = z.Id,
                Code = z.Code,
                Name = z.Name,
                TemperatureType = z.TemperatureType,
                IsPickingZone = z.IsPickingZone,
                WarehouseId = z.WarehouseId,
                WarehouseName = z.Warehouse.Name
            })
            .FirstOrDefaultAsync(ct);
    }
}
