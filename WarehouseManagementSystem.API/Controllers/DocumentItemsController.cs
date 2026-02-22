using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WarehouseManagementSystem.Domain.Model.DocumentsDomain;
using WarehouseManagementSystem.Infrastructure.Persistence;

namespace WarehouseManagementSystem.API.Controllers
{
    [Route("api/Documents/{documentId}/[controller]")]
    [ApiController]
    public class DocumentItemsController : ControllerBase
    {
        private readonly WarehouseManagementSystemDbContext _context;

        public DocumentItemsController(WarehouseManagementSystemDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<DocumentItem>>> GetDocumentItems()
        {
            return await _context.DocumentItems.ToListAsync();
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<DocumentItem>> GetDocumentItem(Guid id)
        {
            var documentItem = await _context.DocumentItems.FindAsync(id);

            if (documentItem == null) { return NotFound(); }

            return documentItem;
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> PutDocumentItem(Guid id, DocumentItem documentItem)
        {
            if (id != documentItem.Id) { return BadRequest(); }

            _context.Entry(documentItem).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!DocumentItemExists(id)) { return NotFound(); }
                else { throw; }
            }

            return NoContent();
        }

        [HttpPost]
        public async Task<ActionResult<DocumentItem>> PostDocumentItem(DocumentItem documentItem)
        {
            _context.DocumentItems.Add(documentItem);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetDocumentItem", new { id = documentItem.Id }, documentItem);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteDocumentItem(Guid id)
        {
            var documentItem = await _context.DocumentItems.FindAsync(id);
            if (documentItem == null) { return NotFound(); }

            _context.DocumentItems.Remove(documentItem);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool DocumentItemExists(Guid id)
        {
            return _context.DocumentItems.Any(e => e.Id == id);
        }
    }
}
