using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WarehouseManagementSystem.Domain.Model.WarehouseDomain;
using WarehouseManagementSystem.Infrastructure.Persistence;

namespace WarehouseManagementSystem.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class WarehouseZonesController : ControllerBase
    {
        private readonly WarehouseManagementSystemDbContext _context;

        public WarehouseZonesController(WarehouseManagementSystemDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<WarehouseZone>>> GetWarehouseZones()
        {
            return await _context.WarehouseZones.ToListAsync();
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<WarehouseZone>> GetWarehouseZone(Guid id)
        {
            var warehouseZone = await _context.WarehouseZones.FindAsync(id);

            if (warehouseZone == null) { return NotFound(); }

            return warehouseZone;
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> PutWarehouseZone(Guid id, WarehouseZone warehouseZone)
        {
            if (id != warehouseZone.Id) { return BadRequest(); }

            _context.Entry(warehouseZone).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!WarehouseZoneExists(id)) { return NotFound(); }
                else { throw; }
            }

            return NoContent();
        }

        [HttpPost]
        public async Task<ActionResult<WarehouseZone>> PostWarehouseZone(WarehouseZone warehouseZone)
        {
            _context.WarehouseZones.Add(warehouseZone);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetWarehouseZone", new { id = warehouseZone.Id }, warehouseZone);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteWarehouseZone(Guid id)
        {
            var warehouseZone = await _context.WarehouseZones.FindAsync(id);
            if (warehouseZone == null) { return NotFound(); }

            _context.WarehouseZones.Remove(warehouseZone);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool WarehouseZoneExists(Guid id)
        {
            return _context.WarehouseZones.Any(e => e.Id == id);
        }
    }
}
