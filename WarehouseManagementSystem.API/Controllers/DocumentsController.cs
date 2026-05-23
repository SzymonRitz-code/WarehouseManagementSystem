using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WarehouseManagementSystem.API.DTO;
using WarehouseManagementSystem.API.Services.Documents;
using WarehouseManagementSystem.API.Services.Queries;
using WarehouseManagementSystem.API.Services.User;
using WarehouseManagementSystem.Domain.ValueObjects;

namespace WarehouseManagementSystem.API.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
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
    [HttpGet("test")]
    [AllowAnonymous]
    public IActionResult Test() => Ok("działa");
    /// <summary>
    /// Pobranie dokumentu po Id
    /// </summary>
    //[HttpGet("paginated")]
    //public async Task<ActionResult<DocumentDto>> GetDocuments([FromQuery]int page,[FromQuery] int pagesize)
    //{
    //    var documents = await _queryService.GetPagedAsync(page, pagesize);
    //    return Ok(_mapper.Map<DocumentDto>(documents));
    //}

    /// <summary>
    /// Pobranie isty dokumentów
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<DocumentListDto>> GetDocuments()
    {
        try
        {
            var documents = await _queryService.GetDocumentsAsync();
            return Ok(_mapper.Map<List<DocumentListDto>>(documents));
        }
        catch (Exception ex)
        {
            throw;
        }
        return Ok(new List<DocumentListDto>());
    }
    /// <summary>
    /// Pobranie oczekujących dokumentów
    /// </summary>
    [HttpGet("pending")]
    public async Task<ActionResult<IEnumerable<DocumentListDto>>> GetPendingDocuments()
    {
        try
        {
            var pending = await _queryService.GetPendingDocumentsAsync();
            return Ok(_mapper.Map<IEnumerable<DocumentListDto>>(pending));

        }
        catch (Exception ex)
        {
            throw;
        }
        return Ok(new List<DocumentListDto>());
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
    public async Task<ActionResult<DocumentDto>> CreateDocument([FromBody] CreateDocumentDto documentDto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        if (documentDto.Items == null || !documentDto.Items.Any())
            return BadRequest("Document must have at least one item.");

        // Mapujemy DTO → ValueObject (DocumentItemDraft)
        var itemDrafts = documentDto.Items.Select(i => new DocumentItemDraft(
            productId: i.ProductId,
            quantity: i.Quantity,
            productBatchId: i.ProductBatchId,
            sourceZoneId: i.SourceZoneId,
            targetZoneId: i.TargetZoneId
        )).ToList();

        // Tworzymy dokument poprzez serwis domenowy
        var document = await _commandService.CreateDocumentAsync(
            type: documentDto.Type,
            createdBy: UserService.GetUser(HttpContext), // TODO dodać do pozostałych klas CreatedBy i ewentualnie modifiedBy 
            sourceWarehouseId: documentDto.SourceWarehouseId,
            items: itemDrafts,
            documentDate: documentDto.DocumentDate,
            targetWarehouseId: documentDto.TargetWarehouseId,
            notes: documentDto.Notes
        );

        // Mapujemy agregat domenowy → DTO do zwrócenia
        DocumentDto createdDto = null;
        try
        {
            createdDto = _mapper.Map<DocumentDto>(document);
        }
        catch (Exception ex)
        {

        }


        return CreatedAtAction(nameof(GetDocumentById), new { documentId = document.Id }, createdDto);
    }
    [HttpPut("{documentId}")]
    public async Task<ActionResult<DocumentDto>> UpdateDocument([FromRoute] Guid documentId, [FromBody] DocumentDto documentDto)
    {
        if (documentId != documentDto.Id)
            return BadRequest("Route ID and body ID mismatch");

        if (!ModelState.IsValid) return BadRequest(ModelState);

        if (documentDto.Items == null || !documentDto.Items.Any())
            return BadRequest("Document must have at least one item.");

        // Mapujemy DTO → ValueObject (DocumentItemDraft)
        var itemDrafts = documentDto.Items.Select(i => new DocumentItemDraft(
            productId: i.ProductId,
            quantity: i.Quantity,
            productBatchId: i.ProductBatchId,
            sourceZoneId: i.SourceZoneId,
            targetZoneId: i.TargetZoneId
        )).ToList();

        // Tworzymy dokument poprzez serwis domenowy
        var document = await _commandService.UpdateDocumentAsync(
            documentId: documentDto.Id,
            type: documentDto.Type,
            sourceWarehouseId: documentDto.SourceWarehouseId,
            items: itemDrafts,
            documentDate: documentDto.DocumentDate,
            targetWarehouseId: documentDto.TargetWarehouseId,
            notes: documentDto.Notes
        );
        return NoContent();
    }
    /// <summary>
    /// Potwierdza dokument
    /// </summary>
    [HttpPut("{documentId}/transfer")]
    [Obsolete("MVP flow. Not used in current MM document-driven process. Reserved for future workflow-based transfer execution.")]
    public async Task<IActionResult> TransferDocument(Guid documentId, [FromQuery] Guid transferStartedById)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        await _commandService.StartTransferAsync(documentId, transferStartedById);
        return NoContent();
    }
    /// <summary>
    /// Rozpoczyna transwer dokument
    /// </summary>
    [HttpPut("{documentId}/confirm")]
    public async Task<IActionResult> ConfirmDocument(Guid documentId)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        // Coś w stylu User.Identity.Name
        await _commandService.ConfirmDocumentAsync(documentId, UserService.GetUser());
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
    /// Pobranie ostatnich dokumentów (np. dashboard)
    /// </summary>
    [HttpGet("recent")]
    public async Task<ActionResult<IEnumerable<DocumentDto>>> GetRecent([FromQuery] int take = 10)
    {
        var recent = await _queryService.GetRecentAsync(take);
        return Ok(_mapper.Map<IEnumerable<DocumentDto>>(recent));
    }
}
