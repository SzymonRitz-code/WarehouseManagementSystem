using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WarehouseManagementSystem.API.DTO;
using WarehouseManagementSystem.API.Services.Queries;
using WarehouseManagementSystem.Domain.Model.WarehouseDomain;
using WarehouseManagementSystem.Infrastructure.Persistence;

namespace WarehouseManagementSystem.API.Controllers;

[Route("api/[controller]")]
[ApiController]
public class WarehousesController : ControllerBase
{
    private readonly IStockQueryService _stockQueryService;
    private readonly WarehouseManagementSystemDbContext _unitOfWork;
    private readonly IMapper _mapper;

    public WarehousesController(IStockQueryService stockQueryService, WarehouseManagementSystemDbContext unitOfWork, IMapper mapper)
    {
        this._stockQueryService = stockQueryService;
        _unitOfWork = unitOfWork;
        this._mapper = mapper;
    }
    [HttpGet("{warehouseId}/stocks")]
    public async Task<ActionResult<IEnumerable<StockDto>>> GetStocksInWarehouse(Guid warehouseId)
    {
        var stocks = await _stockQueryService.GetByWarehouseAsync(warehouseId);
        return Ok(_mapper.Map<IEnumerable<StockDto>>(stocks));
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Warehouse>>> GetWarehouses()
    {
        return await _unitOfWork.Warehouses.ToListAsync();
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Warehouse>> GetWarehouse(Guid id)
    {
        var warehouse = await _unitOfWork.Warehouses.FindAsync(id);

        if (warehouse == null) { return NotFound(); }

        return warehouse;
    }
    [HttpGet("{warehouseId}/stocks")]
    public async Task<ActionResult<IEnumerable<StockDto>>> GetAvailableStocksInWarehouse(Guid warehouseId)
    {
        var stocks = await _stockQueryService.GetByWarehouseAsync(warehouseId);
        return Ok(_mapper.Map<IEnumerable<StockDto>>(stocks));
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> PutWarehouse(Guid id, Warehouse warehouse)
    {
        if (id != warehouse.Id) { return BadRequest(); }

        _unitOfWork.Entry(warehouse).State = EntityState.Modified;

        try
        {
            await _unitOfWork.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!WarehouseExists(id)) { return NotFound(); }
            else { throw; }
        }

        return NoContent();
    }

    [HttpPost]
    public async Task<ActionResult<Warehouse>> PostWarehouse(Warehouse warehouse)
    {
        _unitOfWork.Warehouses.Add(warehouse);
        await _unitOfWork.SaveChangesAsync();

        return CreatedAtAction("GetWarehouse", new { id = warehouse.Id }, warehouse);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteWarehouse(Guid id)
    {
        var warehouse = await _unitOfWork.Warehouses.FindAsync(id);
        if (warehouse == null) { return NotFound(); }

        _unitOfWork.Warehouses.Remove(warehouse);
        await _unitOfWork.SaveChangesAsync();

        return NoContent();
    }

    private bool WarehouseExists(Guid id)
    {
        return _unitOfWork.Warehouses.Any(e => e.Id == id);
    }
}
