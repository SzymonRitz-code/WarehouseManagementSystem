using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WarehouseManagementSystem.API;
using WarehouseManagementSystem.API.DTO;
using WarehouseManagementSystem.Domain.Interfaces;

namespace WarehouseManagementSystem.API.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class AuditLogsController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public AuditLogsController(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    // GET: api/auditlogs
    [HttpHead]
    [HttpGet]
    [ResponseCache(CacheProfileName = HttpCacheProfiles.AuditData)] // Dodanie cache dla endpointu zwracającego logi audytu, ponieważ logi audytu nie zmieniają się często i mogą być przechowywane w pamięci podręcznej przez określony czas.
    public async Task<ActionResult<IEnumerable<AuditLogDto>>> GetAuditLogs(
        [FromQuery] string? entityName,
        [FromQuery] Guid? entityId,
        [FromQuery] Guid? performedById)
    {
        var logs = await _unitOfWork.AuditLogs.GetFilteredAsync(
            entityName,
            entityId,
            performedById);

        return Ok(_mapper.Map<IEnumerable<AuditLogDto>>(logs));
    }

    // GET: api/auditlogs/{id}
    [HttpHead("{id}")]
    [HttpGet("{id}", Name = "GetAuditLog")]
    [ResponseCache(CacheProfileName = HttpCacheProfiles.AuditData)]
    public async Task<ActionResult<AuditLogDto>> GetAuditLog(Guid id)
    {
        var log = await _unitOfWork.AuditLogs.FindAsync(id);

        if (log == null)
            return NotFound();

        return Ok(_mapper.Map<AuditLogDto>(log));
    }

    [HttpOptions]
    public IActionResult GetOptions()
    {
        Response.Headers.Append("Allow", "GET, HEAD, OPTIONS");
        return Ok();
    }
}

