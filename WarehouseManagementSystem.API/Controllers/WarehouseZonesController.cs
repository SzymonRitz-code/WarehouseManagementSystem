using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WarehouseManagementSystem.API.DTO;
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
    
    //TODO - ustandaryzować nazwnictwo metod w kontrolerze 
    [HttpPut("{warehouseZoneId}")]
    public async Task<IActionResult> PutWarehouseZone(Guid warehouseZoneId, WarehouseZoneDto zoneDto)
     {
        if (warehouseZoneId != zoneDto.Id) return BadRequest();
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var zone = await _unitOfWork.WarehouseZones.FindAsync(warehouseZoneId);
        if (zone == null) return NotFound();

        //TODO Zastanowć się nad spujną logiką ustawiania asocjacji
        //TODO Obsłużyć błędy domenowe 
        zone.SetCode(zoneDto.Code);
        zone.SetName(zoneDto.Name);
        zone.SetTemperatureType(zoneDto.TemperatureType);
        zone.SetPickingZone(zoneDto.IsPickingZone);
        zone.SetWarehouse(zoneDto.WarehouseId);

        try
        {
            _unitOfWork.WarehouseZones.Update(zone);
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
    public async Task<ActionResult<CreateWarehouseZoneDto>> PostWarehouseZone(CreateWarehouseZoneDto zoneDto)
    {
        if (_unitOfWork.WarehouseZones.Any(w => w.Code == zoneDto.Code))
        {
            ModelState.AddModelError(nameof(zoneDto.Code), "Code Already exists");
        }
        if (!ModelState.IsValid) return ValidationProblem(ModelState);

        var zone = new WarehouseZone(zoneDto.Code, zoneDto.Name, zoneDto.TemperatureType, zoneDto.IsPickingZone, zoneDto.WarehouseId);
        _unitOfWork.WarehouseZones.Add(zone);
        await _unitOfWork.SaveChangesAsync();

        return CreatedAtAction(nameof(GetWarehouseZone), new { warehouseZoneId = zone.Id }, zoneDto);
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