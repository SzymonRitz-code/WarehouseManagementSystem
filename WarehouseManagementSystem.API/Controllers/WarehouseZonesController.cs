using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WarehouseManagementSystem.API.DTO;
using WarehouseManagementSystem.API.Services.User;
using WarehouseManagementSystem.API.Services.Warehouses.Command;
using WarehouseManagementSystem.API.Services.Warehouses.Query;

namespace WarehouseManagementSystem.API.Controllers;

[Authorize]
[ApiController]
[Route("api/zones")]
public class WarehouseZonesController : ControllerBase
{
    #region Fields and Constructor

    private readonly IMapper _mapper;
    private readonly IWarehouseQueryService _warehouseQueryService;
    private readonly IWarehouseZoneCommandService _warehouseZoneCommandService;
    private readonly IUserService _userService;

    public WarehouseZonesController(
        IMapper mapper,
        IWarehouseQueryService warehouseQueryService,
        IWarehouseZoneCommandService warehouseZoneCommandService,
        IUserService userService)
    {
        _mapper = mapper;
        _warehouseQueryService = warehouseQueryService;
        _warehouseZoneCommandService = warehouseZoneCommandService;
        _userService = userService;
    }

    #endregion

    #region Query Actions

    /// <summary>
    /// Gets the warehouse zone list.
    /// </summary>
    /// <param name="ct">Operation cancellation token.</param>
    /// <returns>Warehouse zone list.</returns>
    [HttpHead]
    [HttpGet]
    [ResponseCache(CacheProfileName = HttpCacheProfiles.ReferenceData)]
    public async Task<ActionResult<IEnumerable<WarehouseZoneListDto>>> GetWarehouseZones(CancellationToken ct)
    {
        var zones = await _warehouseQueryService.GetWarehouseZonesAsync(ct);
        return Ok(zones);
    }

    /// <summary>
    /// Gets warehouse zone details by identifier.
    /// </summary>
    /// <param name="warehouseZoneId">Unique warehouse zone identifier.</param>
    /// <param name="ct">Operation cancellation token.</param>
    /// <returns>The warehouse zone with the specified identifier, or a 404 response if it does not exist.</returns>
    [HttpHead("{warehouseZoneId}")]
    [HttpGet("{warehouseZoneId}")]
    [ResponseCache(CacheProfileName = HttpCacheProfiles.ReferenceData)]
    public async Task<ActionResult<WarehouseZoneDetailsDto>> GetWarehouseZone(Guid warehouseZoneId, CancellationToken ct)
    {
        var zone = await _warehouseQueryService.GetWarehouseZoneAsync(warehouseZoneId, ct);
        return zone == null ? (ActionResult<WarehouseZoneDetailsDto>)NotFound() : (ActionResult<WarehouseZoneDetailsDto>)Ok(zone);
    }

    #endregion

    #region Create, Update and Delete Actions

    /// <summary>
    /// Updates an existing warehouse zone.
    /// </summary>
    /// <remarks>
    /// The route identifier must match the identifier provided in the request body.
    /// An audit log entry is saved after the warehouse zone is updated.
    /// </remarks>
    /// <param name="warehouseZoneId">Unique warehouse zone identifier from the request route.</param>
    /// <param name="zoneDto">Warehouse zone data to update.</param>
    /// <returns>A 204 response after a successful update, or a 404 response if the zone does not exist.</returns>
    [HttpPut("{warehouseZoneId}")]
    public async Task<IActionResult> UpdateWarehouseZone(Guid warehouseZoneId, UpdateWarehouseZoneDto zoneDto, CancellationToken ct)
    {
        if (warehouseZoneId != zoneDto.Id)
        {
            return BadRequest("Route ID and body ID mismatch.");
        }

        if (await _warehouseZoneCommandService.CodeExistsAsync(zoneDto.Code, warehouseZoneId, ct))
        {
            ModelState.AddModelError(nameof(zoneDto.Code), "Code Already exists");
        }

        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var user = _userService.GetUser(HttpContext);
        var updated = await _warehouseZoneCommandService.UpdateAsync(
            warehouseZoneId,
            zoneDto,
            user,
            HttpContext.Connection.RemoteIpAddress?.ToString(),
            ct);

        return updated == null ? NotFound() : NoContent();
    }

    /// <summary>
    /// Creates a new warehouse zone.
    /// </summary>
    /// <remarks>
    /// The warehouse zone code must be unique. An audit log entry is saved after the zone is created.
    /// </remarks>
    /// <param name="zoneDto">Warehouse zone data to create.</param>
    /// <returns>The created warehouse zone with the URL for retrieving its details.</returns>
    [HttpPost]
    public async Task<ActionResult<WarehouseZoneDetailsDto>> CreateWarehouseZone(CreateWarehouseZoneDto zoneDto, CancellationToken ct)
    {
        if (await _warehouseZoneCommandService.CodeExistsAsync(zoneDto.Code, null, ct))
        {
            ModelState.AddModelError(nameof(zoneDto.Code), "Code Already exists");
        }

        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var user = _userService.GetUser(HttpContext);
        var zone = await _warehouseZoneCommandService.CreateAsync(
            zoneDto,
            user,
            HttpContext.Connection.RemoteIpAddress?.ToString(),
            ct);

        var createdDto = _mapper.Map<WarehouseZoneDetailsDto>(zone);
        return CreatedAtAction(nameof(GetWarehouseZone), new { warehouseZoneId = zone.Id }, createdDto);
    }

    /// <summary>
    /// Deletes a warehouse zone.
    /// </summary>
    /// <remarks>
    /// An audit log entry is saved after the warehouse zone is deleted.
    /// </remarks>
    /// <param name="warehouseZoneId">Unique identifier of the warehouse zone to delete.</param>
    /// <returns>A 204 response after a successful delete, or a 404 response if the zone does not exist.</returns>
    [HttpDelete("{warehouseZoneId}")]
    public async Task<IActionResult> DeleteWarehouseZone(Guid warehouseZoneId, CancellationToken ct)
    {
        var user = _userService.GetUser(HttpContext);
        var deleted = await _warehouseZoneCommandService.DeleteAsync(
            warehouseZoneId,
            user,
            HttpContext.Connection.RemoteIpAddress?.ToString(),
            ct);

        return deleted ? NoContent() : NotFound();
    }

    #endregion

    #region Options Action

    /// <summary>
    /// Returns the available HTTP methods supported by the warehouse zones controller.
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
