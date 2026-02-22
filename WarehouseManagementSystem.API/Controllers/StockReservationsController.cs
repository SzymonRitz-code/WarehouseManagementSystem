using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WarehouseManagementSystem.Domain.Interfaces;
using WarehouseManagementSystem.Domain.Model.InventoryDomain;

namespace WarehouseManagementSystem.API.Controllers
{
    [Route("api/Stocks/{stockId}/[controller]")]
    [ApiController]
    public class StockReservationsController : ControllerBase
    {
        private readonly IUnitOfWork _unitOfWork;

        public StockReservationsController(IUnitOfWork context)
        {
            _unitOfWork = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<StockReservation>>> GetStockReservations()
        {
            return Ok(await _unitOfWork.StockReservations.AllAsync());
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<StockReservation>> GetStockReservation(Guid id)
        {
            var stockReservation = await _unitOfWork.StockReservations.FindAsync(id);

            if (stockReservation == null) { return NotFound(); }

            return stockReservation;
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> PutStockReservation(Guid id, StockReservation stockReservation)
        {
            if (id != stockReservation.Id) { return BadRequest(); }

            _unitOfWork.StockReservations.Update(stockReservation);

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
        public async Task<ActionResult<StockReservation>> PostStockReservation(StockReservation stockReservation)
        {
            _unitOfWork.StockReservations.Add(stockReservation);
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
}
