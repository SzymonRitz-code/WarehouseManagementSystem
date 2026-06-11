using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WarehouseManagementSystem.API.DTO;
using WarehouseManagementSystem.API.Services.AuditLogs;
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
    private readonly IAuditLogService _auditLogService;
    private readonly ILogger<WarehouseZonesController> _logger;
    private readonly IUserService _userService;

    public WarehouseZonesController(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        IAuditLogService auditLogService,
        ILogger<WarehouseZonesController> logger,
        IUserService userService)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _auditLogService = auditLogService;
        _logger = logger;
        _userService = userService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<WarehouseZoneDto>>> GetWarehouseZones()
    {
        var zones = await _unitOfWork.WarehouseZones.AllAsync();
        return Ok(_mapper.Map<IEnumerable<WarehouseZoneDto>>(zones));
    }

    [HttpGet("{warehouseZoneId}")]
    public async Task<ActionResult<WarehouseZoneDto>> GetWarehouseZone(Guid warehouseZoneId)
    {
        var zone = await _unitOfWork.WarehouseZones.FindAsync(warehouseZoneId);
        if (zone == null) return NotFound();

        return Ok(_mapper.Map<WarehouseZoneDto>(zone));
    }

    [HttpPut("{warehouseZoneId}")]
    public async Task<IActionResult> UpdateWarehouseZone(Guid warehouseZoneId, WarehouseZoneDto zoneDto)
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

        return NoContent();
    }

    [HttpPost]
    public async Task<ActionResult<CreateWarehouseZoneDto>> CreateWarehouseZone(CreateWarehouseZoneDto zoneDto)
    {
        if (_unitOfWork.WarehouseZones.Any(w => w.Code == zoneDto.Code))
        {
            ModelState.AddModelError(nameof(zoneDto.Code), "Code Already exists");
        }
        if (!ModelState.IsValid) return ValidationProblem(ModelState);

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


        var createdDto = _mapper.Map<WarehouseZoneDto>(zone);
        return CreatedAtAction(nameof(GetWarehouseZone), new { warehouseZoneId = zone.Id }, createdDto);
    }

    [HttpDelete("{warehouseZoneId}")]
    public async Task<IActionResult> DeleteWarehouseZone(Guid warehouseZoneId)
    {
        var zone = await _unitOfWork.WarehouseZones.FindAsync(warehouseZoneId);
        if (zone == null) return NotFound();
        var oldZone = AuditSnapshots.WarehouseZone(zone);

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

        return NoContent();
    }

    private bool WarehouseZoneExists(Guid warehouseZoneId)
    {
        return _unitOfWork.WarehouseZones.Any(z => z.Id == warehouseZoneId);
    }
}
