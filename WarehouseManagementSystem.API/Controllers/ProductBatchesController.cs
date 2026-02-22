using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WarehouseManagementSystem.Domain.Model.InventoryDomain;
using WarehouseManagementSystem.Infrastructure.Persistence;

namespace WarehouseManagementSystem.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductBatchesController : ControllerBase
    {
        private readonly WarehouseManagementSystemDbContext _context;

        public ProductBatchesController(WarehouseManagementSystemDbContext context)
        {
            _context = context;
        }

        // GET: api/ProductBatches
        [HttpGet]
        public async Task<ActionResult<IEnumerable<ProductBatch>>> GetProductBatches()
        {
            return await _context.ProductBatches.ToListAsync();
        }

        // GET: api/ProductBatches/5
        [HttpGet("{id}")]
        public async Task<ActionResult<ProductBatch>> GetProductBatch(Guid id)
        {
            var productBatch = await _context.ProductBatches.FindAsync(id);

            if (productBatch == null)
            {
                return NotFound();
            }

            return productBatch;
        }

        // PUT: api/ProductBatches/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> PutProductBatch(Guid id, ProductBatch productBatch)
        {
            if (id != productBatch.Id)
            {
                return BadRequest();
            }

            _context.Entry(productBatch).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ProductBatchExists(id))
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

        // POST: api/ProductBatches
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<ProductBatch>> PostProductBatch(ProductBatch productBatch)
        {
            _context.ProductBatches.Add(productBatch);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetProductBatch", new { id = productBatch.Id }, productBatch);
        }

        // DELETE: api/ProductBatches/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteProductBatch(Guid id)
        {
            var productBatch = await _context.ProductBatches.FindAsync(id);
            if (productBatch == null)
            {
                return NotFound();
            }

            _context.ProductBatches.Remove(productBatch);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool ProductBatchExists(Guid id)
        {
            return _context.ProductBatches.Any(e => e.Id == id);
        }
    }
}
