using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WarehouseManagementSystem.API.DTO;
using WarehouseManagementSystem.API.Services.AuditLogs.Command;
using WarehouseManagementSystem.API.Services.AuditLogs;
using WarehouseManagementSystem.API.Services.User;
using WarehouseManagementSystem.API.Services.Warehouses.Query;
using WarehouseManagementSystem.Domain.Interfaces;
using WarehouseManagementSystem.Domain.Model.WarehouseDomain;

namespace WarehouseManagementSystem.API.Controllers;

[Authorize]
[ApiController]
[Route("api/zones")]
public class WarehouseZonesController : ControllerBase
{
    #region Fields and Constructor

    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly IWarehouseQueryService _warehouseQueryService;
    private readonly IAuditLogCommandService _auditLogService;
    private readonly ILogger<WarehouseZonesController> _logger;
    private readonly IUserService _userService;

    public WarehouseZonesController(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        IWarehouseQueryService warehouseQueryService,
        IAuditLogCommandService auditLogService,
        ILogger<WarehouseZonesController> logger,
        IUserService userService)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _warehouseQueryService = warehouseQueryService;
        _auditLogService = auditLogService;
        _logger = logger;
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
    public async Task<IActionResult> UpdateWarehouseZone(Guid warehouseZoneId, UpdateWarehouseZoneDto zoneDto)
    {
        if (warehouseZoneId != zoneDto.Id)
        {
            return BadRequest("Route ID and body ID mismatch.");
        }

        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var zone = await _unitOfWork.WarehouseZones.FindAsync(warehouseZoneId);
        if (zone == null)
        {
            return NotFound();
        }

        var oldZone = AuditSnapshots.WarehouseZone(zone);

        zone.SetCode(zoneDto.Code);
        zone.SetName(zoneDto.Name);
        zone.SetTemperatureType(zoneDto.TemperatureType);
        zone.SetPickingZone(zoneDto.IsPickingZone);
        zone.SetWarehouse(zoneDto.WarehouseId);

        try
        {
            _unitOfWork.WarehouseZones.Update(zone);
            var user = _userService.GetUser(HttpContext);
            await _auditLogService.LogChangesAsync(
                nameof(WarehouseZone),
                zone.Id,
                "Update",
                user.Id,
                oldZone,
                AuditSnapshots.WarehouseZone(zone),
                HttpContext.Connection.RemoteIpAddress?.ToString());
            await _unitOfWork.SaveChangesAsync();
            _logger.LogInformation("Warehouse zone {WarehouseZoneId} updated by {UserId}", zone.Id, user.Id);
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!WarehouseZoneExists(warehouseZoneId))
            {
                return NotFound();
            }

            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Warehouse zone update failed for warehouseZone {WarehouseZoneId}", warehouseZoneId);
            throw;
        }

        return NoContent();
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
    public async Task<ActionResult<WarehouseZoneDetailsDto>> CreateWarehouseZone(CreateWarehouseZoneDto zoneDto)
    {
        if (_unitOfWork.WarehouseZones.Any(w => w.Code == zoneDto.Code))
        {
            ModelState.AddModelError(nameof(zoneDto.Code), "Code Already exists");
        }
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        try
        {
            var zone = new WarehouseZone(zoneDto.Code, zoneDto.Name, zoneDto.TemperatureType, zoneDto.IsPickingZone, zoneDto.WarehouseId, _userService.GetUser(HttpContext));
            _unitOfWork.WarehouseZones.Add(zone);
            var user = _userService.GetUser(HttpContext);
            await _auditLogService.LogChangesAsync(
                nameof(WarehouseZone),
                zone.Id,
                "Create",
                user.Id,
                null,
                AuditSnapshots.WarehouseZone(zone),
                HttpContext.Connection.RemoteIpAddress?.ToString());
            await _unitOfWork.SaveChangesAsync();
            _logger.LogInformation("Warehouse zone {WarehouseZoneId} created by {UserId}", zone.Id, user.Id);

            var createdDto = _mapper.Map<WarehouseZoneDetailsDto>(zone);
            return CreatedAtAction(nameof(GetWarehouseZone), new { warehouseZoneId = zone.Id }, createdDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Warehouse zone create failed for code {Code}", zoneDto.Code);
            throw;
        }
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
    public async Task<IActionResult> DeleteWarehouseZone(Guid warehouseZoneId)
    {
        var zone = await _unitOfWork.WarehouseZones.FindAsync(warehouseZoneId);
        if (zone == null)
        {
            return NotFound();
        }

        var oldZone = AuditSnapshots.WarehouseZone(zone);

        try
        {
            _unitOfWork.WarehouseZones.Delete(zone);
            var user = _userService.GetUser(HttpContext);
            await _auditLogService.LogChangesAsync(
                nameof(WarehouseZone),
                zone.Id,
                "Delete",
                user.Id,
                oldZone,
                null,
                HttpContext.Connection.RemoteIpAddress?.ToString());
            await _unitOfWork.SaveChangesAsync();
            _logger.LogInformation("Warehouse zone {WarehouseZoneId} deleted by {UserId}", zone.Id, user.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Warehouse zone delete failed for warehouseZone {WarehouseZoneId}", warehouseZoneId);
            throw;
        }

        return NoContent();
    }

    #endregion

    #region Helper Methods

    private bool WarehouseZoneExists(Guid warehouseZoneId)
    {
        return _unitOfWork.WarehouseZones.Any(z => z.Id == warehouseZoneId);
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
