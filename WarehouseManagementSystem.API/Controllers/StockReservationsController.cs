using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WarehouseManagementSystem.API.DTO;
using WarehouseManagementSystem.Domain.Interfaces;
using WarehouseManagementSystem.Domain.Model.InventoryDomain;

namespace WarehouseManagementSystem.API.Controllers;

[Route("api/Stocks/{stockId}/[controller]")]
[ApiController]
public class StockReservationsController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _autoMapper;

    public StockReservationsController(IUnitOfWork context, IMapper autoMapper)
    {
        _unitOfWork = context;
        _autoMapper = autoMapper;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<StockReservationDto>>> GetStockReservations()
    {
        return Ok(_autoMapper.Map<IEnumerable<StockReservationDto>>(await _unitOfWork.StockReservations.AllAsync()));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<StockReservationDto>> GetStockReservation(Guid id)
    {
        var stockReservationEntity = await _unitOfWork.StockReservations.FindAsync(id);

        if (stockReservationEntity == null) { return NotFound(); }

        var stockReservation = _autoMapper.Map<StockReservationDto>(stockReservationEntity);

        return stockReservation;
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> PutStockReservation(Guid id, StockReservationDto stockReservation)
    {
        if (id != stockReservation.Id) { return BadRequest(); }

        var stockReservationEntity = _autoMapper.Map<StockReservation>(stockReservation);
        _unitOfWork.StockReservations.Update(stockReservationEntity);

        try
        {
            await _unitOfWork.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!StockReservationExists(id)) { return NotFound(); }
            else { throw; }
        }

        return NoContent();
    }

    [HttpPost]
    public async Task<ActionResult<StockReservationDto>> PostStockReservation(StockReservationDto stockReservation)
    {
        var stockReservationEntity = _autoMapper.Map<StockReservation>(stockReservation);
        _unitOfWork.StockReservations.Add(stockReservationEntity);
        await _unitOfWork.SaveChangesAsync();

        return CreatedAtAction("GetStockReservation", new { id = stockReservation.Id }, stockReservation);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteStockReservation(Guid id)
    {
        var stockReservation = await _unitOfWork.StockReservations.FindAsync(id);
        if (stockReservation == null) { return NotFound(); }

        _unitOfWork.StockReservations.Delete(stockReservation);
        await _unitOfWork.SaveChangesAsync();

        return NoContent();
    }

    private bool StockReservationExists(Guid id)
    {
        return _unitOfWork.StockReservations.Any(e => e.Id == id);
    }
}
