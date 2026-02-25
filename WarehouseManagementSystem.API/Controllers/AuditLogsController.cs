using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using WarehouseManagementSystem.API.DTO;
using WarehouseManagementSystem.Domain.Interfaces;

namespace WarehouseManagementSystem.API.Controllers;

[Route("api/[controller]")]
[ApiController]
public class AuditLogsController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public AuditLogsController(
        IUnitOfWork unitOfWork,
        IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    // GET: api/auditlogs
    [HttpGet]
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
    [HttpGet("{id}", Name = "GetAuditLog")]
    public async Task<ActionResult<AuditLogDto>> GetAuditLog(Guid id)
    {
        var log = await _unitOfWork.AuditLogs.FindAsync(id);

        if (log == null)
            return NotFound();

        return Ok(_mapper.Map<AuditLogDto>(log));
    }
}

