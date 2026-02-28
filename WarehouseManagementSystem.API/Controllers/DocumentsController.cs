using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using WarehouseManagementSystem.API.DTO;
using WarehouseManagementSystem.API.Services.Documents;
using WarehouseManagementSystem.API.Services.Queries;
using WarehouseManagementSystem.Domain.Model.InventoryDomain;
using WarehouseManagementSystem.Domain.ValueObjects;

namespace WarehouseManagementSystem.API.Controllers;

[Route("api/[controller]")]
[ApiController]
public class DocumentsController : ControllerBase
{
    private readonly IDocumentCommandService _commandService;
    private readonly IDocumentQueryService _queryService;
    private readonly IMapper _mapper;

    public DocumentsController(IDocumentCommandService commandService, IDocumentQueryService queryService, IMapper mapper)
    {
        _commandService = commandService;
        _queryService = queryService;
        _mapper = mapper;
    }

    /// <summary>
    /// Pobranie dokumentu po Id
    /// </summary>
    [HttpGet("{documentId}")]
    public async Task<ActionResult<DocumentDto>> GetDocumentById(Guid documentId)
    {
        var document = await _queryService.GetByIdAsync(documentId);
        if (document == null) return NotFound();

        return Ok(_mapper.Map<DocumentDto>(document));
    }

    /// <summary>
    /// Tworzy dokument wraz z pozycjami (DocumentItemDraft → DocumentItem)
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<DocumentDto>> CreateDocument([FromBody] CreateDocumentDto request)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        if (request.Items == null || !request.Items.Any())
            return BadRequest("Document must have at least one item.");

        // Mapujemy DTO → ValueObject (DocumentItemDraft)
        var itemDrafts = request.Items.Select(i => new DocumentItemDraft(
            productId: i.ProductId,
            quantity: i.Quantity,
            productBatchId: i.ProductBatchId,
            sourceZoneId: i.SourceZoneId,
            targetZoneId: i.TargetZoneId
        )).ToList();

        // Tworzymy dokument poprzez serwis domenowy
        var document = await _commandService.CreateDocumentAsync(
            type: request.Type,
            createdById: request.CreatedById,
            sourceWarehouseId: request.SourceWarehouseId,
            items: itemDrafts,
            documentDate: request.DocumentDate,
            targetWarehouseId: request.TargetWarehouseId,
            notes: request.Notes
        );

        // Mapujemy agregat domenowy → DTO do zwrócenia
        var documentDto = _mapper.Map<DocumentDto>(document);

        return CreatedAtAction(nameof(GetDocumentById), new { documentId = document.Id }, documentDto);
    }
    /// <summary>
    /// Potwierdza dokument
    /// </summary>
    [HttpPut("{documentId}/transfer")]
    public async Task<IActionResult> TransferDocument(Guid documentId, [FromQuery] Guid transferStartedById)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        await _commandService.StartTransferAsync(documentId, transferStartedById);
        return NoContent();
    }
    /// <summary>
    /// Potwierdza dokument
    /// </summary>
    [HttpPut("{documentId}/confirm")]
    public async Task<IActionResult> ConfirmDocument(Guid documentId, [FromQuery] Guid confirmedById)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        await _commandService.ConfirmDocumentAsync(documentId, confirmedById);
        return NoContent();
    }

    /// <summary>
    /// Anuluje dokument
    /// </summary>
    [HttpPut("{documentId}/cancel")]
    public async Task<IActionResult> CancelDocument(Guid documentId)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        await _commandService.CancelDocumentAsync(documentId);
        return NoContent();
    }

    /// <summary>
    /// Pobranie dokumentów wg typu i statusu
    /// </summary>
    [HttpGet("byTypeAndStatus")]
    public async Task<ActionResult<IEnumerable<DocumentDto>>> GetByTypeAndStatus(
        [FromQuery] string type,
        [FromQuery] string status)
    {
        if (!Enum.TryParse<Domain.Enums.DocumentType>(type, true, out var docType) ||
            !Enum.TryParse<Domain.Enums.DocumentStatus>(status, true, out var docStatus))
            return BadRequest("Invalid type or status.");

        var documents = await _queryService.GetByTypeAndStatusAsync(docType, docStatus);
        return Ok(_mapper.Map<IEnumerable<DocumentDto>>(documents));
    }

    /// <summary>
    /// Pobranie dokumentów w statusie Draft
    /// </summary>
    [HttpGet("drafts")]
    public async Task<ActionResult<IEnumerable<DocumentDto>>> GetDrafts()
    {
        var drafts = await _queryService.GetDraftsAsync();
        return Ok(_mapper.Map<IEnumerable<DocumentDto>>(drafts));
    }

    /// <summary>
    /// Pobranie dokumentów oczekujących potwierdzenia
    /// </summary>
    [HttpGet("pending")]
    public async Task<ActionResult<IEnumerable<DocumentDto>>> GetPendingConfirmation()
    {
        var pending = await _queryService.GetPendingConfirmationAsync();
        return Ok(_mapper.Map<IEnumerable<DocumentDto>>(pending));
    }

    /// <summary>
    /// Pobranie ostatnich dokumentów (np. dashboard)
    /// </summary>
    [HttpGet("recent")]
    public async Task<ActionResult<IEnumerable<DocumentDto>>> GetRecent([FromQuery] int take = 10)
    {
        var recent = await _queryService.GetRecentAsync(take);
        return Ok(_mapper.Map<IEnumerable<DocumentDto>>(recent));
    }
}
