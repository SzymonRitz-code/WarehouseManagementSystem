using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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
    /// Gets all stock records in the selected warehouse.
    /// </summary>
    /// <param name="warehouseId">Unique warehouse identifier.</param>
    /// <returns>List of stock records located in the warehouse.</returns>
    [HttpHead("{warehouseId}/stocks")]
    [HttpGet("{warehouseId}/stocks")]
    [ResponseCache(CacheProfileName = HttpCacheProfiles.VolatileData)]
    public async Task<ActionResult<IEnumerable<StockDto>>> GetStocksInWarehouse(Guid warehouseId)
    {
        var stocks = await _stockQueryService.GetByWarehouseAsync(warehouseId);
        return Ok(_mapper.Map<IEnumerable<StockDto>>(stocks));
    }

    /// <summary>
    /// Gets stock records available for picking in the selected warehouse.
    /// </summary>
    /// <param name="warehouseId">Unique warehouse identifier.</param>
    /// <returns>List of stock records available for picking.</returns>
    [HttpHead("{warehouseId}/stocks/available-for-picking")]
    [HttpGet("{warehouseId}/stocks/available-for-picking")]
    [ResponseCache(CacheProfileName = HttpCacheProfiles.VolatileData)]
    public async Task<ActionResult<IEnumerable<StockDto>>> GetAvailableForPicking(Guid warehouseId)
    {
        var stocks = await _stockQueryService.GetAvailableForPickingAsync(warehouseId);
        return Ok(_mapper.Map<IEnumerable<StockDto>>(stocks));
    }

    /// <summary>
    /// Gets the warehouse list.
    /// </summary>
    /// <param name="ct">Operation cancellation token.</param>
    /// <returns>Warehouse list.</returns>
    [HttpHead]
    [HttpGet]
    [ResponseCache(CacheProfileName = HttpCacheProfiles.ReferenceData)]
    public async Task<ActionResult<IEnumerable<WarehouseListDto>>> GetWarehouses(CancellationToken ct)
    {
        var warehouses = await _warehouseQueryService.GetWarehousesAsync(ct);
        return Ok(warehouses);
    }

    /// <summary>
    /// Gets warehouse details by identifier.
    /// </summary>
    /// <param name="warehouseId">Unique warehouse identifier.</param>
    /// <param name="ct">Operation cancellation token.</param>
    /// <returns>The warehouse with the specified identifier, or a 404 response if it does not exist.</returns>
    [HttpHead("{warehouseId}")]
    [HttpGet("{warehouseId}")]
    [ResponseCache(CacheProfileName = HttpCacheProfiles.ReferenceData)]
    public async Task<ActionResult<WarehouseDetailsDto>> GetWarehouse(Guid warehouseId, CancellationToken ct)
    {
        var warehouse = await _warehouseQueryService.GetWarehouseAsync(warehouseId, ct);
        if (warehouse == null) return NotFound();

        return Ok(warehouse);
    }
    /// <summary>
    /// Creates a new warehouse.
    /// </summary>
    /// <remarks>
    /// The warehouse code must be unique. An audit log entry is saved after the warehouse is created.
    /// </remarks>
    /// <param name="warehouseDto">Warehouse data to create.</param>
    /// <returns>The created warehouse with the URL for retrieving its details.</returns>
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

    /// <summary>
    /// Updates an existing warehouse.
    /// </summary>
    /// <remarks>
    /// The route identifier must match the identifier provided in the request body.
    /// An audit log entry is saved after the warehouse is updated.
    /// </remarks>
    /// <param name="warehouseId">Unique warehouse identifier from the request route.</param>
    /// <param name="warehouseDto">Warehouse data to update.</param>
    /// <returns>A 204 response after a successful update, or a 404 response if the warehouse does not exist.</returns>
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



    /// <summary>
    /// Deletes a warehouse.
    /// </summary>
    /// <remarks>
    /// An audit log entry is saved after the warehouse is deleted.
    /// </remarks>
    /// <param name="warehouseId">Unique identifier of the warehouse to delete.</param>
    /// <returns>A 204 response after a successful delete, or a 404 response if the warehouse does not exist.</returns>
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

    /// <summary>
    /// Returns the available HTTP methods supported by the warehouses controller.
    /// </summary>
    /// <returns>Response with the Allow header containing the list of available HTTP methods.</returns>
    [HttpOptions]
    public IActionResult GetOptions()
    {
        Response.Headers.Append("Allow", "GET, HEAD, POST, PUT, DELETE, OPTIONS");
        return Ok();
    }
}
