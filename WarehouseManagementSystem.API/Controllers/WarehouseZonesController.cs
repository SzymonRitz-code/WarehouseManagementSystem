using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WarehouseManagementSystem.DataAccessLayer;
using WarehouseManagementSystem.Domain.Model.WarehouseDomain;

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

        // GET: api/WarehouseZones
        [HttpGet]
        public async Task<ActionResult<IEnumerable<WarehouseZone>>> GetWarehouseZones()
        {
            return await _context.WarehouseZones.ToListAsync();
        }

        // GET: api/WarehouseZones/5
        [HttpGet("{id}")]
        public async Task<ActionResult<WarehouseZone>> GetWarehouseZone(Guid id)
        {
            var warehouseZone = await _context.WarehouseZones.FindAsync(id);

            if (warehouseZone == null)
            {
                return NotFound();
            }

            return warehouseZone;
        }

        // PUT: api/WarehouseZones/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> PutWarehouseZone(Guid id, WarehouseZone warehouseZone)
        {
            if (id != warehouseZone.Id)
            {
                return BadRequest();
            }

            _context.Entry(warehouseZone).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!WarehouseZoneExists(id))
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

        // POST: api/WarehouseZones
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<WarehouseZone>> PostWarehouseZone(WarehouseZone warehouseZone)
        {
            _context.WarehouseZones.Add(warehouseZone);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetWarehouseZone", new { id = warehouseZone.Id }, warehouseZone);
        }

        // DELETE: api/WarehouseZones/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteWarehouseZone(Guid id)
        {
            var warehouseZone = await _context.WarehouseZones.FindAsync(id);
            if (warehouseZone == null)
            {
                return NotFound();
            }

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
