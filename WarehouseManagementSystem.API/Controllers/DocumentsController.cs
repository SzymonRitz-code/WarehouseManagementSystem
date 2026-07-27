using Asp.Versioning;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WarehouseManagementSystem.API.DTO;
using WarehouseManagementSystem.API.Services.Documents.Command;
using WarehouseManagementSystem.API.Services.Documents.Query;
using WarehouseManagementSystem.API.Services.User;
using WarehouseManagementSystem.Domain.Model.DocumentsDomain;
using WarehouseManagementSystem.Domain.ValueObjects;

namespace WarehouseManagementSystem.API.Controllers;

// Wymaga uwierzytelnienia dla wszystkich akcji w tym kontrolerze, jest to ustawione na poziomie kontrolera,
// więc wszystkie akcje dziedziczą to ustawienie. Można je nadpisać na poziomie akcji, jeśli jest to konieczne.
// Jest dodany filtr globalny w klasie Program.cs, który obsługuje uwierzytelnianie i autoryzację,
// więc nie trzeba dodawać [Authorize] do każdej akcji. Wystarczy dodać [AllowAnonymous] do akcji,
// które mają być dostępne bez uwierzytelnienia.
[Authorize]
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public class DocumentsController : ControllerBase
{
    #region Fields and Constructor

    private readonly IDocumentCommandService _commandService;
    private readonly IDocumentQueryService _queryService;
    private readonly IMapper _mapper;
    private readonly IUserService _userService;
    private readonly ILogger<DocumentsController> _logger;

    public DocumentsController(
        IDocumentCommandService commandService,
        IDocumentQueryService queryService,
        IMapper mapper,
        IUserService userService,
        ILogger<DocumentsController> logger)
    {
        _commandService = commandService;
        _queryService = queryService;
        _mapper = mapper;
        _userService = userService;
        _logger = logger;
    }

    #endregion

    #region Query Actions

    /// <summary>
    /// Gets a paginated list of documents using the provided filters.
    /// </summary>
    /// <param name="query">Filtering, sorting, and pagination parameters for the document list.</param>
    /// <param name="ct">Operation cancellation token.</param>
    /// <returns>Paginated document list.</returns>
    [HttpHead]
    [HttpGet]
    [ResponseCache(CacheProfileName = HttpCacheProfiles.OperationalData)]
    public async Task<ActionResult<PagedResult<DocumentListDto>>> GetDocuments([FromQuery] DocumentListQuery query, CancellationToken ct)
    {
        var documents = await _queryService.GetDocumentsPageAsync(query, ct);
        return Ok(documents);
    }
    /// <summary>
    /// Gets a paginated list of pending documents, meaning documents in draft status.
    /// </summary>
    /// <param name="query">Filtering, sorting, and pagination parameters for the document list.</param>
    /// <param name="ct">Operation cancellation token.</param>
    /// <returns>Paginated pending document list.</returns>
    [HttpHead("pending")]
    [HttpGet("pending")]
    [ResponseCache(CacheProfileName = HttpCacheProfiles.VolatileData)]
    public async Task<ActionResult<PagedResult<DocumentListDto>>> GetPendingDocuments([FromQuery] DocumentListQuery query, CancellationToken ct)
    {
        query.Status = Domain.Enums.DocumentStatus.Draft;

        var pending = await _queryService.GetDocumentsPageAsync(query, ct);
        return Ok(pending);
    }
    /// <summary>
    /// Gets document details by identifier.
    /// </summary>
    /// <param name="documentId">Unique document identifier.</param>
    /// <returns>The document with the specified identifier, or a 404 response if it does not exist.</returns>
    [HttpHead("{documentId}")]
    [HttpGet("{documentId}")]
    [ResponseCache(CacheProfileName = HttpCacheProfiles.OperationalData)]
    public async Task<ActionResult<DocumentDto>> GetDocumentById(Guid documentId, CancellationToken ct = default)
    {
        var document = await _queryService.GetByIdAsync(documentId, ct);
        return document == null ? NotFound() : Ok(document);
    }

    #endregion

    #region Create and Update Actions

    /// <summary>
    /// Creates a new document with its items.
    /// </summary>
    /// <remarks>
    /// Items provided in the request are converted to document item drafts,
    /// and then passed to the domain service responsible for creating the document.
    /// </remarks>
    /// <param name="documentDto">Document data and the list of items to create.</param>
    /// <returns>The created document with the URL for retrieving its details.</returns>
    [HttpPost]
    public async Task<ActionResult<DocumentDto>> CreateDocument([FromBody] CreateDocumentDto documentDto, CancellationToken ct = default)
    {
        // Mapujemy DTO → ValueObject (DocumentItemDraft)
        var itemDrafts = documentDto.Items.Select(i => new DocumentItemDraft(
            productId: i.ProductId,
            quantity: i.Quantity,
            productBatchId: i.ProductBatchId,
            sourceZoneId: i.SourceZoneId,
            targetZoneId: i.TargetZoneId
        )).ToList();

        // Tworzymy dokument poprzez serwis domenowy
        Document document;
        try
        {
            document = await _commandService.CreateDocumentAsync(
                type: documentDto.Type,
                createdBy: _userService.GetUser(HttpContext),
                sourceWarehouseId: documentDto.SourceWarehouseId,
                items: itemDrafts,
                documentDate: documentDto.DocumentDate,
                targetWarehouseId: documentDto.TargetWarehouseId,
                notes: documentDto.Notes,
                ct: ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Document create failed for user {UserId}", _userService.GetUser(HttpContext).Id);
            throw;
        }


        // Mapujemy agregat domenowy → DTO do zwrócenia
        DocumentDto createdDto = _mapper.Map<DocumentDto>(document);

        return CreatedAtAction(nameof(GetDocumentById), new { documentId = document.Id }, createdDto);
    }
    /// <summary>
    /// Updates an existing document with its items.
    /// </summary>
    /// <remarks>
    /// The route identifier must match the identifier provided in the request body.
    /// Document items are passed to the domain service as document item drafts.
    /// </remarks>
    /// <param name="documentId">Unique document identifier from the request route.</param>
    /// <param name="documentDto">Document data and the list of items to update.</param>
    /// <returns>A 204 response after a successful update, or a validation response when the data is invalid.</returns>
    [HttpPut("{documentId}")]
    public async Task<ActionResult<DocumentDto>> UpdateDocument([FromRoute] Guid documentId, [FromBody] UpdateDocumentDto documentDto, CancellationToken ct = default)
    {
        if (documentId != documentDto.Id)
        {
            return BadRequest("Route ID and body ID mismatch.");
        }

        // Mapujemy DTO → ValueObject (DocumentItemDraft)
        var itemDrafts = documentDto.Items.Select(i => new DocumentItemDraft(
            productId: i.ProductId,
            quantity: i.Quantity,
            productBatchId: i.ProductBatchId,
            sourceZoneId: i.SourceZoneId,
            targetZoneId: i.TargetZoneId
        )).ToList();

        // Tworzymy dokument poprzez serwis domenowy
        try
        {
            await _commandService.UpdateDocumentAsync(
                documentId: documentDto.Id,
                _userService.GetUser(HttpContext),
                type: documentDto.Type,
                sourceWarehouseId: documentDto.SourceWarehouseId,
                items: itemDrafts,
                documentDate: documentDto.DocumentDate,
                targetWarehouseId: documentDto.TargetWarehouseId,
                notes: documentDto.Notes,
                ct: ct
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Document update failed for document {DocumentId}", documentId);
            throw;
        }
        return NoContent();
    }

    #endregion

    #region Workflow Actions

    /// <summary>
    /// Confirms the document and triggers the resulting domain changes.
    /// </summary>
    /// <param name="documentId">Unique identifier of the document to confirm.</param>
    /// <returns>A 204 response after the document is successfully confirmed.</returns>
    [HttpPut("{documentId}/confirm")]
    public async Task<IActionResult> ConfirmDocument(Guid documentId, CancellationToken ct = default)
    {
        // Coś w stylu User.Identity.Name
        try
        {
            await _commandService.ConfirmDocumentAsync(documentId, _userService.GetUser(HttpContext), ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Document confirm failed for document {DocumentId}", documentId);
            throw;
        }
        return NoContent();
    }

    /// <summary>
    /// Cancels the document.
    /// </summary>
    /// <param name="documentId">Unique identifier of the document to cancel.</param>
    /// <returns>A 204 response after the document is successfully canceled.</returns>
    [HttpPut("{documentId}/cancel")]
    public async Task<IActionResult> CancelDocument(Guid documentId, CancellationToken ct = default)
    {
        try
        {
            await _commandService.CancelDocumentAsync(documentId, _userService.GetUser(HttpContext), ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Document cancel failed for document {DocumentId}", documentId);
            throw;
        }
        return NoContent();
    }

    #endregion

    #region Specialized Query Actions

    /// <summary>
    /// Gets documents matching the specified type and status.
    /// </summary>
    /// <param name="type">Document type as text matching the values of <see cref="Domain.Enums.DocumentType"/>.</param>
    /// <param name="status">Document status as text matching the values of <see cref="Domain.Enums.DocumentStatus"/>.</param>
    /// <returns>List of documents matching the specified type and status, or a 400 response if the parameters are invalid.</returns>
    [HttpHead("byTypeAndStatus")]
    [HttpGet("byTypeAndStatus")]
    [ResponseCache(CacheProfileName = HttpCacheProfiles.OperationalData)]
    public async Task<ActionResult<IEnumerable<DocumentDto>>> GetByTypeAndStatus(
        [FromQuery] string type,
        [FromQuery] string status,
        CancellationToken ct = default)
    {
        if (!Enum.TryParse<Domain.Enums.DocumentType>(type, true, out var docType) ||
            !Enum.TryParse<Domain.Enums.DocumentStatus>(status, true, out var docStatus))
        {
            return BadRequest("Invalid type or status.");
        }

        var documents = await _queryService.GetByTypeAndStatusAsync(docType, docStatus, ct);
        return Ok(documents);
    }

    /// <summary>
    /// Gets documents in draft status.
    /// </summary>
    /// <returns>List of documents in Draft status.</returns>
    [HttpHead("drafts")]
    [HttpGet("drafts")]
    [ResponseCache(CacheProfileName = HttpCacheProfiles.VolatileData)]
    public async Task<ActionResult<IEnumerable<DocumentDto>>> GetDrafts(CancellationToken ct = default)
    {
        var drafts = await _queryService.GetDraftsAsync(ct);
        return Ok(drafts);
    }


    /// <summary>
    /// Gets recently created or modified documents.
    /// </summary>
    /// <param name="take">Maximum number of documents to retrieve.</param>
    /// <returns>List of recent documents limited by the <paramref name="take"/> parameter.</returns>
    [HttpHead("recent")]
    [HttpGet("recent")]
    [ResponseCache(CacheProfileName = HttpCacheProfiles.VolatileData)]
    public async Task<ActionResult<IEnumerable<DocumentDto>>> GetRecent([FromQuery] int take = 10, CancellationToken ct = default)
    {
        var recent = await _queryService.GetRecentAsync(take, ct);
        return Ok(recent);
    }

    #endregion

    #region Options Action

    /// <summary>
    /// Returns the available HTTP methods supported by the documents controller.
    /// </summary>
    /// <returns>Response with the Allow header containing the list of available HTTP methods.</returns>
    [HttpOptions]
    public IActionResult GetOptions()
    {
        Response.Headers.Append("Allow", "GET, HEAD, POST, PUT, OPTIONS");
        return Ok();
    }

    #endregion
}
