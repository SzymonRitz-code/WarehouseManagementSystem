using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WarehouseManagementSystem.API.DTO;
using WarehouseManagementSystem.API.Services.Documents.Query;

namespace WarehouseManagementSystem.API.Controllers;

[Authorize]
[ApiController]
[Route("api/Documents/{documentId}/[controller]")]
public class DocumentItemsController : ControllerBase
{
    private readonly IDocumentQueryService _documentQueryService;
    private readonly IMapper _mapper;

    public DocumentItemsController(IDocumentQueryService documentQueryService, IMapper mapper)
    {
        _documentQueryService = documentQueryService;
        _mapper = mapper;
    }

    /// <summary>
    /// Gets all document items.
    /// </summary>
    /// <param name="documentId">Unique document identifier.</param>
    /// <returns>List of document items, or a 404 response if the document does not exist.</returns>
    [HttpHead]
    [HttpGet]
    [ResponseCache(CacheProfileName = HttpCacheProfiles.OperationalData)]
    public async Task<ActionResult<IEnumerable<DocumentItemDto>>> GetAllItems(Guid documentId, CancellationToken ct = default)
    {
        var document = await _documentQueryService.GetByIdAsync(documentId, ct);
        if (document == null)
        {
            return NotFound();
        }

        var items = document.Items;
        var itemsDto = _mapper.Map<IEnumerable<DocumentItemDto>>(items);

        return Ok(itemsDto);
    }

    /// <summary>
    /// Gets a specific document item by identifier.
    /// </summary>
    /// <param name="documentId">Unique document identifier.</param>
    /// <param name="itemId">Unique document item identifier.</param>
    /// <returns>The document item with the specified identifier, or a 404 response if the document or item does not exist.</returns>
    [HttpHead("{itemId}")]
    [HttpGet("{itemId}")]
    [ResponseCache(CacheProfileName = HttpCacheProfiles.OperationalData)]
    public async Task<ActionResult<DocumentItemDto>> GetItemById(Guid documentId, Guid itemId, CancellationToken ct = default)
    {
        var document = await _documentQueryService.GetByIdAsync(documentId, ct);
        if (document == null)
        {
            return NotFound();
        }

        var item = document.Items.FirstOrDefault(i => i.Id == itemId);
        if (item == null)
        {
            return NotFound();
        }

        var itemDto = _mapper.Map<DocumentItemDto>(item);
        return Ok(itemDto);
    }

    /// <summary>
    /// Returns the available HTTP methods supported by the document items controller.
    /// </summary>
    /// <returns>Response with the Allow header containing the list of available HTTP methods.</returns>
    [HttpOptions]
    public IActionResult GetOptions()
    {
        Response.Headers.Append("Allow", "GET, HEAD, OPTIONS");
        return Ok();
    }
}


