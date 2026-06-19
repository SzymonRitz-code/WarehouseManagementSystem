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
[Route("api/zones")]
public class WarehouseZonesController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly IWarehouseQueryService _warehouseQueryService;
    private readonly IAuditLogService _auditLogService;
    private readonly ILogger<WarehouseZonesController> _logger;
    private readonly IUserService _userService;

    public WarehouseZonesController(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        IWarehouseQueryService warehouseQueryService,
        IAuditLogService auditLogService,
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

    [HttpGet]
    [ResponseCache(CacheProfileName = HttpCacheProfiles.ReferenceData)]
    public async Task<ActionResult<IEnumerable<WarehouseZoneListDto>>> GetWarehouseZones(CancellationToken ct)
    {
        var zones = await _warehouseQueryService.GetWarehouseZonesAsync(ct);
        return Ok(zones);
    }

    [HttpGet("{warehouseZoneId}")]
    [ResponseCache(CacheProfileName = HttpCacheProfiles.ReferenceData)]
    public async Task<ActionResult<WarehouseZoneDetailsDto>> GetWarehouseZone(Guid warehouseZoneId, CancellationToken ct)
    {
        var zone = await _warehouseQueryService.GetWarehouseZoneAsync(warehouseZoneId, ct);
        if (zone == null) return NotFound();

        return Ok(zone);
    }

    [HttpPut("{warehouseZoneId}")]
    public async Task<IActionResult> UpdateWarehouseZone(Guid warehouseZoneId, UpdateWarehouseZoneDto zoneDto)
    {
        if (warehouseZoneId != zoneDto.Id) return BadRequest("Route ID and body ID mismatch.");

        if (!ModelState.IsValid) return ValidationProblem(ModelState);

        var zone = await _unitOfWork.WarehouseZones.FindAsync(warehouseZoneId);
        if (zone == null) return NotFound();
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
            if (!WarehouseZoneExists(warehouseZoneId)) return NotFound();
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Warehouse zone update failed for warehouseZone {WarehouseZoneId}", warehouseZoneId);
            throw;
        }

        return NoContent();
    }

    [HttpPost]
    public async Task<ActionResult<WarehouseZoneDetailsDto>> CreateWarehouseZone(CreateWarehouseZoneDto zoneDto)
    {
        if (_unitOfWork.WarehouseZones.Any(w => w.Code == zoneDto.Code))
        {
            ModelState.AddModelError(nameof(zoneDto.Code), "Code Already exists");
        }
        if (!ModelState.IsValid) return ValidationProblem(ModelState);

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

    [HttpDelete("{warehouseZoneId}")]
    public async Task<IActionResult> DeleteWarehouseZone(Guid warehouseZoneId)
    {
        var zone = await _unitOfWork.WarehouseZones.FindAsync(warehouseZoneId);
        if (zone == null) return NotFound();
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

    private bool WarehouseZoneExists(Guid warehouseZoneId)
    {
        return _unitOfWork.WarehouseZones.Any(z => z.Id == warehouseZoneId);
    }
}
