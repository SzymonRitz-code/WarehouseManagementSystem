using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WarehouseManagementSystem.API;
using WarehouseManagementSystem.API.DTO;
using WarehouseManagementSystem.API.Services.Documents;
using WarehouseManagementSystem.API.Services.Queries;
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
[Route("api/[controller]")]
public class DocumentsController : ControllerBase
{
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
    [HttpHead]
    [HttpGet]
    [ResponseCache(CacheProfileName = HttpCacheProfiles.OperationalData)]
    public async Task<ActionResult<PagedResult<DocumentListDto>>> GetDocuments([FromQuery] DocumentListQuery query, CancellationToken ct)
    {
        var documents = await _queryService.GetDocumentsPageAsync(query, ct);
        return Ok(documents);
    }
    /// <summary>
    /// Pobranie oczekujących dokumentów
    /// </summary>
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
    /// Pobranie dokumentu po Id
    /// </summary>
    [HttpHead("{documentId}")]
    [HttpGet("{documentId}")]
    [ResponseCache(CacheProfileName = HttpCacheProfiles.OperationalData)]
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
        if (documentDto.Items == null || !documentDto.Items.Any())
            ModelState.AddModelError(nameof(documentDto.Items), "Document must have at least one item.");

        if (!ModelState.IsValid) return ValidationProblem(ModelState);

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
                notes: documentDto.Notes);
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
    /// Aktualizuje dokument wraz z pozycjami (DocumentItemDraft → DocumentItem)
    /// </summary>
    /// <remarks>
    /// Ta metoda wykonuje asynchroniczne zapytanie do bazy danych. 
    /// W przypadku braku zamówienia zwraca wartość <c>null</c> zamiast zgłaszać wyjątek.
    /// </remarks>
    /// <param name="documentId">Unikalny identyfikator zamówienia (GUID).</param>
    /// <param name="documentDto">body dokumentu </param>
    /// <returns>
    /// Zadanie (Task) reprezentujące operację asynchroniczną. 
    /// Zwraca obiekt <see cref="OrderDetailsDto"/>, jeśli zamówienie istnieje; w przeciwnym razie <c>null</c>.
    /// </returns>
    /// <exception cref="ArgumentException">Gdy <paramref name="orderId"/> jest pustym GUIDem.</exception>
    /// <exception cref="TimeoutException">Gdy połączenie z bazą danych przekroczy limit czasu.</exception>
    [HttpPut("{documentId}")]
    public async Task<ActionResult<DocumentDto>> UpdateDocument([FromRoute] Guid documentId, [FromBody] UpdateDocumentDto documentDto)
    {
        if (documentId != documentDto.Id)
        {
            return BadRequest("Route ID and body ID mismatch.");
        }

        if (documentDto.Items == null || !documentDto.Items.Any())
            ModelState.AddModelError(nameof(documentDto.Items), "Document must have at least one item.");

        if (!ModelState.IsValid) return ValidationProblem(ModelState);

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
                notes: documentDto.Notes
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Document update failed for document {DocumentId}", documentId);
            throw;
        }
        return NoContent();
    }
    /// <summary>
    /// Potwierdza dokument
    /// </summary>
    /// <param name="documentId"></param>
    /// <returns></returns>
    [HttpPut("{documentId}/confirm")]
    public async Task<IActionResult> ConfirmDocument(Guid documentId)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);
        // Coś w stylu User.Identity.Name
        try
        {
            await _commandService.ConfirmDocumentAsync(documentId, _userService.GetUser(HttpContext));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Document confirm failed for document {DocumentId}", documentId);
            throw;
        }
        return NoContent();
    }

    /// <summary>
    /// Anuluje dokument
    /// </summary>
    /// <param name="documentId"></param>
    /// <returns></returns>
    [HttpPut("{documentId}/cancel")]
    public async Task<IActionResult> CancelDocument(Guid documentId)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);
        try
        {
            await _commandService.CancelDocumentAsync(documentId, _userService.GetUser(HttpContext));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Document cancel failed for document {DocumentId}", documentId);
            throw;
        }
        return NoContent();
    }

    /// <summary>
    /// Pobranie dokumentów po typie i statusie
    /// </summary>
    /// <param name="type"></param>
    /// <param name="status"></param>
    /// <returns></returns>
    [HttpHead("byTypeAndStatus")]
    [HttpGet("byTypeAndStatus")]
    [ResponseCache(CacheProfileName = HttpCacheProfiles.OperationalData)]
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
    /// <returns></returns>
    [HttpHead("drafts")]
    [HttpGet("drafts")]
    [ResponseCache(CacheProfileName = HttpCacheProfiles.VolatileData)]
    public async Task<ActionResult<IEnumerable<DocumentDto>>> GetDrafts()
    {
        var drafts = await _queryService.GetDraftsAsync();
        return Ok(_mapper.Map<IEnumerable<DocumentDto>>(drafts));
    }


    /// <summary>
    /// Pobranie ostatnich dokumentów
    /// </summary>
    /// <param name="take"></param>
    /// <returns></returns>
    [HttpHead("recent")]
    [HttpGet("recent")]
    [ResponseCache(CacheProfileName = HttpCacheProfiles.VolatileData)]
    public async Task<ActionResult<IEnumerable<DocumentDto>>> GetRecent([FromQuery] int take = 10)
    {
        var recent = await _queryService.GetRecentAsync(take);
        return Ok(_mapper.Map<IEnumerable<DocumentDto>>(recent));
    }

    /// <summary>
    /// Pobranie dostępnych metod HTTP dla tego kontrolera
    /// </summary>
    /// <returns></returns>
    [HttpOptions]
    public IActionResult GetOptions()
    {
        Response.Headers.Append("Allow", "GET, HEAD, POST, PUT, OPTIONS");
        return Ok();
    }
}
