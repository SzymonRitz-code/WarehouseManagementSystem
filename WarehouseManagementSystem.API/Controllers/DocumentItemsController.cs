using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WarehouseManagementSystem.API.DTO;
using WarehouseManagementSystem.API.Services.Queries;

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
    /// Pobiera wszystkie pozycje dokumentu.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<DocumentItemDto>>> GetAllItems(Guid documentId)
    {
        var document = await _documentQueryService.GetByIdAsync(documentId);
        if (document == null)
            return NotFound();

        var items = document.Items;
        var itemsDto = _mapper.Map<IEnumerable<DocumentItemDto>>(items);

        return Ok(itemsDto);
    }

    /// <summary>
    /// Pobiera konkretną pozycję dokumentu po Id.
    /// </summary>
    [HttpGet("{itemId}")]
    public async Task<ActionResult<DocumentItemDto>> GetItemById(Guid documentId, Guid itemId)
    {
        var document = await _documentQueryService.GetByIdAsync(documentId);
        if (document == null)
            return NotFound();

        var item = document.Items.FirstOrDefault(i => i.Id == itemId);
        if (item == null)
            return NotFound();

        var itemDto = _mapper.Map<DocumentItemDto>(item);
        return Ok(itemDto);
    }
}


