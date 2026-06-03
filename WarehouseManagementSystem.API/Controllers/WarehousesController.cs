using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WarehouseManagementSystem.API.DTO;
using WarehouseManagementSystem.API.Services.Queries;
using WarehouseManagementSystem.Domain.Model.WarehouseDomain;
using WarehouseManagementSystem.Infrastructure.Persistence;

namespace WarehouseManagementSystem.API.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
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
    [HttpPost]
    public async Task<ActionResult<WarehouseDto>> PostWarehouse(CreateWarehouseDto warehouseDto)
    {
        if (_unitOfWork.Warehouses.Any(w => w.Code == warehouseDto.Code))
        {
            ModelState.AddModelError(nameof(warehouseDto.Code), "Code Already exists");
        }
        if (!ModelState.IsValid) return ValidationProblem(ModelState);

        var warehouse = new Warehouse(
            warehouseDto.Code,
            warehouseDto.Name,
            warehouseDto.Country,
            warehouseDto.City,
            warehouseDto.Address);

        try
        {
            _unitOfWork.Warehouses.Add(warehouse);
            await _unitOfWork.SaveChangesAsync();
        }
        catch (Exception ex) {
            throw;
        }


        return CreatedAtAction(nameof(GetWarehouse), new { warehouseId = warehouse.Id }, warehouseDto);
    }

    [HttpPut("{warehouseId}")]
    public async Task<IActionResult> PutWarehouse([FromRoute] Guid warehouseId, WarehouseDto warehouseDto)
    {
        if (warehouseId != warehouseDto.Id) return BadRequest();
        if (!ModelState.IsValid) return BadRequest(ModelState);
        var warehouse = await _unitOfWork.Warehouses.FindAsync(warehouseId);
        if (warehouse == null) return NotFound();

        warehouse.SetCode(warehouseDto.Code);
        warehouse.SetName(warehouseDto.Name);
        warehouse.SetLocation(warehouseDto.Country, warehouseDto.City, warehouseDto.Address);

        if (warehouseDto.IsActive) { warehouse.Activate(); }
        else { warehouse.Deactivate(); }


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