using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WarehouseManagementSystem.API.DTO;
using WarehouseManagementSystem.API.Services.AuditLogs.Query;

namespace WarehouseManagementSystem.API.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class AuditLogsController : ControllerBase
{
    private readonly IAuditLogQueryService _auditLogQueryService;
    private readonly IMapper _mapper;

    public AuditLogsController(IAuditLogQueryService auditLogQueryService, IMapper mapper)
    {
        _auditLogQueryService = auditLogQueryService;
        _mapper = mapper;
    }

    /// <summary>
    /// Gets a list of audit log entries with optional filtering.
    /// </summary>
    /// <param name="entityName">Optional entity name for which audit log entries should be retrieved.</param>
    /// <param name="entityId">Optional entity identifier for which audit log entries should be retrieved.</param>
    /// <param name="performedById">Optional identifier of the user who performed the operation.</param>
    /// <returns>List of audit log entries matching the provided criteria.</returns>
    [HttpHead]
    [HttpGet]
    [ResponseCache(CacheProfileName = HttpCacheProfiles.AuditData)] // Dodanie cache dla endpointu zwracającego logi audytu, ponieważ logi audytu nie zmieniają się często i mogą być przechowywane w pamięci podręcznej przez określony czas.
    public async Task<ActionResult<IEnumerable<AuditLogDto>>> GetAuditLogs(
        [FromQuery] string? entityName,
        [FromQuery] Guid? entityId,
        [FromQuery] Guid? performedById)
    {
        var logs = await _auditLogQueryService.GetFilteredAsync(
            entityName,
            entityId,
            performedById);

        return Ok(_mapper.Map<IEnumerable<AuditLogDto>>(logs));
    }

    /// <summary>
    /// Gets a single audit log entry by identifier.
    /// </summary>
    /// <param name="id">Unique audit log entry identifier.</param>
    /// <returns>The audit log entry with the specified identifier, or a 404 response if it does not exist.</returns>
    [HttpHead("{id}")]
    [HttpGet("{id}", Name = "GetAuditLog")]
    [ResponseCache(CacheProfileName = HttpCacheProfiles.AuditData)]
    public async Task<ActionResult<AuditLogDto>> GetAuditLog(Guid id)
    {
        var log = await _auditLogQueryService.GetByIdAsync(id);

        return log == null ? (ActionResult<AuditLogDto>)NotFound() : (ActionResult<AuditLogDto>)Ok(_mapper.Map<AuditLogDto>(log));
    }

    /// <summary>
    /// Returns the available HTTP methods supported by the audit controller.
    /// </summary>
    /// <returns>Response with the Allow header containing the list of available HTTP methods.</returns>
    [HttpOptions]
    public IActionResult GetOptions()
    {
        Response.Headers.Append("Allow", "GET, HEAD, OPTIONS");
        return Ok();
    }
}

