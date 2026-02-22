using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WarehouseManagementSystem.API.DTO;
using WarehouseManagementSystem.Domain.Model.AuditDomain;
using WarehouseManagementSystem.Infrastructure.Persistence;

namespace WarehouseManagementSystem.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuditLogsController : ControllerBase
    {
        private readonly WarehouseManagementSystemDbContext _context;
        private readonly IMapper _autoMapper;

        public AuditLogsController(WarehouseManagementSystemDbContext context, IMapper autoMapper)
        {
            _context = context;
            _autoMapper = autoMapper;
        }

        // GET: api/AuditLogs
        [HttpGet]
        public async Task<ActionResult<IEnumerable<AuditLogDto>>> GetAuditLogs()
        {
            var audits = await _context.AuditLogs.ToListAsync();
            return Ok(_autoMapper.Map<IEnumerable<AuditLogDto>>(audits));
        }

        // GET: api/AuditLogs/5
        [HttpGet("{id}")]
        public async Task<ActionResult<AuditLogDto>> GetAuditLog(Guid id)
        {
            var auditLog = await _context.AuditLogs.FindAsync(id);

            if (auditLog == null)
            {
                return NotFound();
            }

            return Ok(_autoMapper.Map<AuditLogDto>(auditLog));
        }

        // PUT: api/AuditLogs/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        // TODO poprawić walidacje oraz zaimplementować PUT
        [HttpPut("{id}")]
        public async Task<IActionResult> PutAuditLog(Guid id, AuditLogDto auditLog)
        {
            if (id != auditLog.Id)
            {
                return BadRequest();
            }
            if (ModelState.IsValid == false) { return BadRequest(auditLog); }
            var entry = _autoMapper.Map<AuditLog>(auditLog);
            _context.Update(entry);

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!AuditLogExists(id))
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

        // POST: api/AuditLogs
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<AuditLog>> PostAuditLog(AuditLogDto auditLog)
        {
            if (ModelState.IsValid == false) { return BadRequest(auditLog); }
            var entry = _autoMapper.Map<AuditLog>(auditLog);
            _context.AuditLogs.Add(entry);
            await _context.SaveChangesAsync();

            return CreatedAtRoute("GetAuditLog", new { id = auditLog.Id }, auditLog);
        }

        // DELETE: api/AuditLogs/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteAuditLog(Guid id)
        {
            var auditLog = await _context.AuditLogs.FindAsync(id);
            if (auditLog == null)
            {
                return NotFound();
            }

            _context.AuditLogs.Remove(auditLog);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool AuditLogExists(Guid id)
        {
            return _context.AuditLogs.Any(e => e.Id == id);
        }
    }
}
