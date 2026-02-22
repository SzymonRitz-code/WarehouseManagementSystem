using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WarehouseManagementSystem.Domain.Model.WarehouseDomain;
using WarehouseManagementSystem.Infrastructure.Persistence;

namespace WarehouseManagementSystem.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class WarehousesController : ControllerBase
    {
        private readonly WarehouseManagementSystemDbContext _context;

        public WarehousesController(WarehouseManagementSystemDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Warehouse>>> GetWarehouses()
        {
            return await _context.Warehouses.ToListAsync();
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Warehouse>> GetWarehouse(Guid id)
        {
            var warehouse = await _context.Warehouses.FindAsync(id);

            if (warehouse == null) { return NotFound(); }

            return warehouse;
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> PutWarehouse(Guid id, Warehouse warehouse)
        {
            if (id != warehouse.Id) { return BadRequest(); }

            _context.Entry(warehouse).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!WarehouseExists(id)) { return NotFound(); }
                else { throw; }
            }

            return NoContent();
        }

        [HttpPost]
        public async Task<ActionResult<Warehouse>> PostWarehouse(Warehouse warehouse)
        {
            _context.Warehouses.Add(warehouse);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetWarehouse", new { id = warehouse.Id }, warehouse);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteWarehouse(Guid id)
        {
            var warehouse = await _context.Warehouses.FindAsync(id);
            if (warehouse == null) { return NotFound(); }

            _context.Warehouses.Remove(warehouse);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool WarehouseExists(Guid id)
        {
            return _context.Warehouses.Any(e => e.Id == id);
        }
    }
}
