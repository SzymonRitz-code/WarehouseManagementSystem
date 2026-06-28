using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WarehouseManagementSystem.API.DTO;
using WarehouseManagementSystem.API.Services.Stocks.Query;
using WarehouseManagementSystem.API.Services.User;
using WarehouseManagementSystem.API.Services.Warehouses.Command;
using WarehouseManagementSystem.API.Services.Warehouses.Query;

namespace WarehouseManagementSystem.API.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class WarehousesController : ControllerBase
{
    #region Fields and Constructor

    private readonly IStockQueryService _stockQueryService;
    private readonly IWarehouseQueryService _warehouseQueryService;
    private readonly IWarehouseCommandService _warehouseCommandService;
    private readonly IMapper _mapper;
    private readonly IUserService _userService;

    public WarehousesController(
        IStockQueryService stockQueryService,
        IWarehouseQueryService warehouseQueryService,
        IWarehouseCommandService warehouseCommandService,
        IMapper mapper,
        IUserService userService)
    {
        _stockQueryService = stockQueryService;
        _warehouseQueryService = warehouseQueryService;
        _warehouseCommandService = warehouseCommandService;
        _mapper = mapper;
        _userService = userService;
    }

    #endregion

    #region Query Actions

    /// <summary>
    /// Gets all stock records in the selected warehouse.
    /// </summary>
    /// <param name="warehouseId">Unique warehouse identifier.</param>
    /// <returns>List of stock records located in the warehouse.</returns>
    [HttpHead("{warehouseId}/stocks")]
    [HttpGet("{warehouseId}/stocks")]
    [ResponseCache(CacheProfileName = HttpCacheProfiles.VolatileData)]
    public async Task<ActionResult<IEnumerable<StockDto>>> GetStocksInWarehouse(Guid warehouseId, CancellationToken ct)
    {
        var stocks = await _stockQueryService.GetByWarehouseAsync(warehouseId, ct);
        return Ok(stocks);
    }

    /// <summary>
    /// Gets stock records available for picking in the selected warehouse.
    /// </summary>
    /// <param name="warehouseId">Unique warehouse identifier.</param>
    /// <returns>List of stock records available for picking.</returns>
    [HttpHead("{warehouseId}/stocks/available-for-picking")]
    [HttpGet("{warehouseId}/stocks/available-for-picking")]
    [ResponseCache(CacheProfileName = HttpCacheProfiles.VolatileData)]
    public async Task<ActionResult<IEnumerable<StockDto>>> GetAvailableForPicking(Guid warehouseId, CancellationToken ct)
    {
        var stocks = await _stockQueryService.GetAvailableForPickingAsync(warehouseId, ct);
        return Ok(stocks);
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
        return warehouse == null ? (ActionResult<WarehouseDetailsDto>)NotFound() : (ActionResult<WarehouseDetailsDto>)Ok(warehouse);
    }

    #endregion

    #region Create, Update and Delete Actions

    /// <summary>
    /// Creates a new warehouse.
    /// </summary>
    /// <remarks>
    /// The warehouse code must be unique. An audit log entry is saved after the warehouse is created.
    /// </remarks>
    /// <param name="warehouseDto">Warehouse data to create.</param>
    /// <returns>The created warehouse with the URL for retrieving its details.</returns>
    [HttpPost]
    public async Task<ActionResult<WarehouseDetailsDto>> CreateWarehouse(CreateWarehouseDto warehouseDto, CancellationToken ct)
    {
        if (_warehouseCommandService.CodeExists(warehouseDto.Code, ct: ct))
        {
            ModelState.AddModelError(nameof(warehouseDto.Code), "Code Already exists");
        }

        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var user = _userService.GetUser(HttpContext);
        var warehouse = await _warehouseCommandService.CreateAsync(
            warehouseDto,
            user,
            HttpContext.Connection.RemoteIpAddress?.ToString(),
            ct);

        var createdDto = _mapper.Map<WarehouseDetailsDto>(warehouse);
        return CreatedAtAction(nameof(GetWarehouse), new { warehouseId = warehouse.Id }, createdDto);
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
    public async Task<IActionResult> UpdateWarehouse([FromRoute] Guid warehouseId, UpdateWarehouseDto warehouseDto, CancellationToken ct)
    {
        if (warehouseId != warehouseDto.Id)
        {
            return BadRequest("Route ID and body ID mismatch.");
        }

        if (_warehouseCommandService.CodeExists(warehouseDto.Code, warehouseId, ct))
        {
            ModelState.AddModelError(nameof(warehouseDto.Code), "Code Already exists");
        }

        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var user = _userService.GetUser(HttpContext);
        var updated = await _warehouseCommandService.UpdateAsync(
            warehouseId,
            warehouseDto,
            user,
            HttpContext.Connection.RemoteIpAddress?.ToString(),
            ct);

        return updated == null ? NotFound() : NoContent();
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
    public async Task<IActionResult> DeleteWarehouse(Guid warehouseId, CancellationToken ct)
    {
        var user = _userService.GetUser(HttpContext);
        var deleted = await _warehouseCommandService.DeleteAsync(
            warehouseId,
            user,
            HttpContext.Connection.RemoteIpAddress?.ToString(),
            ct);

        return deleted ? NoContent() : NotFound();
    }

    #endregion

    #region Options Action

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

    #endregion
}
