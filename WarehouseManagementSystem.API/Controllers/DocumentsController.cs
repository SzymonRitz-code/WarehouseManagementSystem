using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WarehouseManagementSystem.Domain.Model.DocumentsDomain;
using WarehouseManagementSystem.Infrastructure.Persistence;

namespace WarehouseManagementSystem.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DocumentsController : ControllerBase
    {
        private readonly WarehouseManagementSystemDbContext _context;

        public DocumentsController(WarehouseManagementSystemDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Document>>> GetDocuments()
        {
            return await _context.Documents.ToListAsync();
        }

        [HttpGet("{documentId}")]
        public async Task<ActionResult<Document>> GetDocument(Guid documentId)
        {
            var document = await _context.Documents.FindAsync(documentId);

            if (document == null) { return NotFound(); }

            return document;
        }

        [HttpPut("{documentId}")]
        public async Task<IActionResult> PutDocument(Guid documentId, Document document)
        {
            if (documentId != document.Id) { return BadRequest(); }

            _context.Entry(document).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!DocumentExists(documentId)) { return NotFound(); }
                else { throw; }
            }

            return NoContent();
        }

        // POST: api/Documents
        [HttpPost]
        public async Task<ActionResult<Document>> PostDocument(Document document)
        {
            _context.Documents.Add(document);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetDocument", new { id = document.Id }, document);
        }

        // DELETE: api/Documents/5
        [HttpDelete("{documentId}")]
        public async Task<IActionResult> DeleteDocument(Guid documentId)
        {
            var document = await _context.Documents.FindAsync(documentId);
            if (document == null) { return NotFound(); }

            _context.Documents.Remove(document);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool DocumentExists(Guid id)
        {
            return _context.Documents.Any(e => e.Id == id);
        }
    }
}
