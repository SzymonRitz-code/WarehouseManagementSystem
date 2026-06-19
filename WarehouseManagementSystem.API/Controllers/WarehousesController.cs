using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WarehouseManagementSystem.API;
using WarehouseManagementSystem.API.DTO;
using WarehouseManagementSystem.API.Services.AuditLogs;
using WarehouseManagementSystem.API.Services.Queries;
using WarehouseManagementSystem.API.Services.User;
using WarehouseManagementSystem.Domain.Interfaces;
using WarehouseManagementSystem.Domain.Model.WarehouseDomain;

namespace WarehouseManagementSystem.API.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class WarehousesController : ControllerBase
{
    private readonly IStockQueryService _stockQueryService;
    private readonly IWarehouseQueryService _warehouseQueryService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly IAuditLogService _auditLogService;
    private readonly ILogger<WarehousesController> _logger;
    private readonly IUserService _userService;

    public WarehousesController(
        IStockQueryService stockQueryService,
        IWarehouseQueryService warehouseQueryService,
        IUnitOfWork unitOfWork,
        IMapper mapper,
        IAuditLogService auditLogService,
        ILogger<WarehousesController> logger,
        IUserService userService)
    {
        _stockQueryService = stockQueryService;
        _warehouseQueryService = warehouseQueryService;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _auditLogService = auditLogService;
        _logger = logger;
        _userService = userService;
    }

    /// <summary>
    /// Pobranie wszystkich stocków w magazynie
    /// </summary>
    [HttpGet("{warehouseId}/stocks")]
    [ResponseCache(CacheProfileName = HttpCacheProfiles.VolatileData)]
    public async Task<ActionResult<IEnumerable<StockDto>>> GetStocksInWarehouse(Guid warehouseId)
    {
        var stocks = await _stockQueryService.GetByWarehouseAsync(warehouseId);
        return Ok(_mapper.Map<IEnumerable<StockDto>>(stocks));
    }

    /// <summary>
    /// Pobranie stocków dostępnych do kompletacji w magazynie
    /// </summary>
    [HttpGet("{warehouseId}/stocks/available-for-picking")]
    [ResponseCache(CacheProfileName = HttpCacheProfiles.VolatileData)]
    public async Task<ActionResult<IEnumerable<StockDto>>> GetAvailableForPicking(Guid warehouseId)
    {
        var stocks = await _stockQueryService.GetAvailableForPickingAsync(warehouseId);
        return Ok(_mapper.Map<IEnumerable<StockDto>>(stocks));
    }

    [HttpGet]
    [ResponseCache(CacheProfileName = HttpCacheProfiles.ReferenceData)]
    public async Task<ActionResult<IEnumerable<WarehouseListDto>>> GetWarehouses(CancellationToken ct)
    {
        var warehouses = await _warehouseQueryService.GetWarehousesAsync(ct);
        return Ok(warehouses);
    }

    [HttpGet("{warehouseId}")]
    [ResponseCache(CacheProfileName = HttpCacheProfiles.ReferenceData)]
    public async Task<ActionResult<WarehouseDetailsDto>> GetWarehouse(Guid warehouseId, CancellationToken ct)
    {
        var warehouse = await _warehouseQueryService.GetWarehouseAsync(warehouseId, ct);
        if (warehouse == null) return NotFound();

        return Ok(warehouse);
    }
    [HttpPost]
    public async Task<ActionResult<WarehouseDetailsDto>> CreateWarehouse(CreateWarehouseDto warehouseDto)
    {
        if (_unitOfWork.Warehouses.Any(w => w.Code == warehouseDto.Code))
            ModelState.AddModelError(nameof(warehouseDto.Code), "Code Already exists");

        if (!ModelState.IsValid) return ValidationProblem(ModelState);

        try
        {
            var warehouse = new Warehouse(
                warehouseDto.Code,
                warehouseDto.Name,
                warehouseDto.Country,
                warehouseDto.City,
                warehouseDto.Address, _userService.GetUser(HttpContext));

            _unitOfWork.Warehouses.Add(warehouse);
            var user = _userService.GetUser(HttpContext);
            await _auditLogService.LogChangesAsync(
                nameof(Warehouse),
                warehouse.Id,
                "Create",
                user.Id,
                null,
                AuditSnapshots.Warehouse(warehouse),
                HttpContext.Connection.RemoteIpAddress?.ToString());
            await _unitOfWork.SaveChangesAsync();
            _logger.LogInformation("Warehouse {WarehouseId} created by {UserId}", warehouse.Id, user.Id);

            var createdDto = _mapper.Map<WarehouseDetailsDto>(warehouse);
            return CreatedAtAction(nameof(GetWarehouse), new { warehouseId = warehouse.Id }, createdDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Warehouse create failed for code {Code}", warehouseDto.Code);
            throw;
        }
    }

    [HttpPut("{warehouseId}")]
    public async Task<IActionResult> UpdateWarehouse([FromRoute] Guid warehouseId, UpdateWarehouseDto warehouseDto)
    {
        if (warehouseId != warehouseDto.Id) return BadRequest("Route ID and body ID mismatch.");

        if (!ModelState.IsValid) return ValidationProblem(ModelState);
        var warehouse = await _unitOfWork.Warehouses.FindAsync(warehouseId);
        if (warehouse == null) return NotFound();
        var oldWarehouse = AuditSnapshots.Warehouse(warehouse);

        warehouse.SetCode(warehouseDto.Code);
        warehouse.SetName(warehouseDto.Name);
        warehouse.SetLocation(warehouseDto.Country, warehouseDto.City, warehouseDto.Address);

        if (warehouseDto.IsActive) { warehouse.Activate(); }
        else { warehouse.Deactivate(); }


        try
        {
            var user = _userService.GetUser(HttpContext);
            await _auditLogService.LogChangesAsync(
                nameof(Warehouse),
                warehouse.Id,
                "Update",
                user.Id,
                oldWarehouse,
                AuditSnapshots.Warehouse(warehouse),
                HttpContext.Connection.RemoteIpAddress?.ToString());
            await _unitOfWork.SaveChangesAsync();
            _logger.LogInformation("Warehouse {WarehouseId} updated by {UserId}", warehouse.Id, user.Id);
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!WarehouseExists(warehouseId)) return NotFound();
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Warehouse update failed for warehouse {WarehouseId}", warehouseId);
            throw;
        }

        return NoContent();
    }



    [HttpDelete("{warehouseId}")]
    public async Task<IActionResult> DeleteWarehouse(Guid warehouseId)
    {
        var warehouse = await _unitOfWork.Warehouses.FindAsync(warehouseId);
        if (warehouse == null) return NotFound();
        var oldWarehouse = AuditSnapshots.Warehouse(warehouse);

        try
        {
            _unitOfWork.Warehouses.Delete(warehouse);
            var user = _userService.GetUser(HttpContext);
            await _auditLogService.LogChangesAsync(
                nameof(Warehouse),
                warehouse.Id,
                "Delete",
                user.Id,
                oldWarehouse,
                null,
                HttpContext.Connection.RemoteIpAddress?.ToString());
            await _unitOfWork.SaveChangesAsync();
            _logger.LogInformation("Warehouse {WarehouseId} deleted by {UserId}", warehouse.Id, user.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Warehouse delete failed for warehouse {WarehouseId}", warehouseId);
            throw;
        }

        return NoContent();
    }

    private bool WarehouseExists(Guid warehouseId)
    {
        return _unitOfWork.Warehouses.Any(w => w.Id == warehouseId);
    }
}
