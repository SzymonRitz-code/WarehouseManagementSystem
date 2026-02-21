using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WarehouseManagementSystem.DataAccessLayer;
using WarehouseManagementSystem.Domain.Model.InventoryDomain;

namespace WarehouseManagementSystem.API.Controllers
{
    [Route("api/Stocks/{stockId}/[controller]")]
    [ApiController]
    public class StockReservationsController : ControllerBase
    {
        private readonly WarehouseManagementSystemDbContext _context;

        public StockReservationsController(WarehouseManagementSystemDbContext context)
        {
            _context = context;
        }

        // GET: api/StockReservations
        [HttpGet]
        public async Task<ActionResult<IEnumerable<StockReservation>>> GetStockReservations()
        {
            return await _context.StockReservations.ToListAsync();
        }

        // GET: api/StockReservations/5
        [HttpGet("{id}")]
        public async Task<ActionResult<StockReservation>> GetStockReservation(Guid id)
        {
            var stockReservation = await _context.StockReservations.FindAsync(id);

            if (stockReservation == null)
            {
                return NotFound();
            }

            return stockReservation;
        }

        // PUT: api/StockReservations/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> PutStockReservation(Guid id, StockReservation stockReservation)
        {
            if (id != stockReservation.Id)
            {
                return BadRequest();
            }

            _context.Entry(stockReservation).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!StockReservationExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        // POST: api/StockReservations
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<StockReservation>> PostStockReservation(StockReservation stockReservation)
        {
            _context.StockReservations.Add(stockReservation);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetStockReservation", new { id = stockReservation.Id }, stockReservation);
        }

        // DELETE: api/StockReservations/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteStockReservation(Guid id)
        {
            var stockReservation = await _context.StockReservations.FindAsync(id);
            if (stockReservation == null)
            {
                return NotFound();
            }

            _context.StockReservations.Remove(stockReservation);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool StockReservationExists(Guid id)
        {
            return _context.StockReservations.Any(e => e.Id == id);
        }
    }
}
