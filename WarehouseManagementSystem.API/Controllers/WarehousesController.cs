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


    //TODO zamienić DbContext na IUnitOfWork
    public WarehousesController(IStockQueryService stockQueryService, WarehouseManagementSystemDbContext unitOfWork, IMapper mapper)
    {
        _stockQueryService = stockQueryService;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    /// <summary>
    /// Pobranie wszystkich stocków w magazynie
    /// </summary>
    [HttpGet("{warehouseId}/stocks")]
    public async Task<ActionResult<IEnumerable<StockDto>>> GetStocksInWarehouse(Guid warehouseId)
    {
        var stocks = await _stockQueryService.GetByWarehouseAsync(warehouseId);
        return Ok(_mapper.Map<IEnumerable<StockDto>>(stocks));
    }

    /// <summary>
    /// Pobranie stocków dostępnych do kompletacji w magazynie
    /// </summary>
    [HttpGet("{warehouseId}/stocks/available-for-picking")]
    public async Task<ActionResult<IEnumerable<StockDto>>> GetAvailableForPicking(Guid warehouseId)
    {
        var stocks = await _stockQueryService.GetAvailableForPickingAsync(warehouseId);
        return Ok(_mapper.Map<IEnumerable<StockDto>>(stocks));
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<WarehouseDto>>> GetWarehouses()
    {
        var warehouses = await _unitOfWork.Warehouses.ToListAsync();
        return Ok(_mapper.Map<IEnumerable<WarehouseDto>>(warehouses));
    }

    [HttpGet("{warehouseId}")]
    public async Task<ActionResult<WarehouseDto>> GetWarehouse(Guid warehouseId)
    {
        var warehouse = await _unitOfWork.Warehouses.FindAsync(warehouseId);
        if (warehouse == null) return NotFound();

        return Ok(_mapper.Map<WarehouseDto>(warehouse));
    }

    [HttpPut("{warehouseId}")]
    public async Task<IActionResult> PutWarehouse(Guid warehouseId, WarehouseDto warehouseDto)
    {
        if (warehouseId != warehouseDto.Id) return BadRequest();
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var warehouseEntity = _mapper.Map<Warehouse>(warehouseDto);
        _unitOfWork.Entry(warehouseEntity).State = EntityState.Modified;

        try
        {
            await _unitOfWork.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!WarehouseExists(warehouseId)) return NotFound();
            throw;
        }

        return NoContent();
    }

    [HttpPost]
    public async Task<ActionResult<WarehouseDto>> PostWarehouse(WarehouseDto warehouseDto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var warehouseEntity = _mapper.Map<Warehouse>(warehouseDto);
        _unitOfWork.Warehouses.Add(warehouseEntity);
        await _unitOfWork.SaveChangesAsync();

        return CreatedAtAction(nameof(GetWarehouse), new { warehouseId = warehouseEntity.Id }, warehouseDto);
    }

    [HttpDelete("{warehouseId}")]
    public async Task<IActionResult> DeleteWarehouse(Guid warehouseId)
    {
        var warehouse = await _unitOfWork.Warehouses.FindAsync(warehouseId);
        if (warehouse == null) return NotFound();

        _unitOfWork.Warehouses.Remove(warehouse);
        await _unitOfWork.SaveChangesAsync();

        return NoContent();
    }

    private bool WarehouseExists(Guid warehouseId)
    {
        return _unitOfWork.Warehouses.Any(w => w.Id == warehouseId);
    }
}