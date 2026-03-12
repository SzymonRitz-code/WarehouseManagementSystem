using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WarehouseManagementSystem.API.DTO;
using WarehouseManagementSystem.Domain.Interfaces;
using WarehouseManagementSystem.Domain.Model.WarehouseDomain;
using WarehouseManagementSystem.Infrastructure.Persistence;

namespace WarehouseManagementSystem.API.Controllers;

[Route("api/[controller]")]
[ApiController]
public class WarehouseZonesController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;


    // TODO zamienić DbContext na IUnitOfWork
    public WarehouseZonesController(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
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
    public async Task<IActionResult> PutWarehouseZone(Guid warehouseZoneId, WarehouseZoneDto zoneDto)
    {
        if (warehouseZoneId != zoneDto.Id) return BadRequest();
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var zoneEntity = _mapper.Map<WarehouseZone>(zoneDto);
        _unitOfWork.WarehouseZones.Update(zoneEntity);

        try
        {
            await _unitOfWork.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!WarehouseZoneExists(warehouseZoneId)) return NotFound();
            throw;
        }

        return NoContent();
    }

    [HttpPost]
    public async Task<ActionResult<WarehouseZoneDto>> PostWarehouseZone(WarehouseZoneDto zoneDto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var zoneEntity = _mapper.Map<WarehouseZone>(zoneDto);
        _unitOfWork.WarehouseZones.Add(zoneEntity);
        await _unitOfWork.SaveChangesAsync();

        return CreatedAtAction(nameof(GetWarehouseZone), new { warehouseZoneId = zoneEntity.Id }, zoneDto);
    }

    [HttpDelete("{warehouseZoneId}")]
    public async Task<IActionResult> DeleteWarehouseZone(Guid warehouseZoneId)
    {
        var zone = await _unitOfWork.WarehouseZones.FindAsync(warehouseZoneId);
        if (zone == null) return NotFound();

        _unitOfWork.WarehouseZones.Delete(zone);
        await _unitOfWork.SaveChangesAsync();

        return NoContent();
    }

    private bool WarehouseZoneExists(Guid warehouseZoneId)
    {
        return _unitOfWork.WarehouseZones.Any(z => z.Id == warehouseZoneId);
    }
}