using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using WarehouseManagementSystem.API.DTO;
using WarehouseManagementSystem.API.Services.Queries;
using WarehouseManagementSystem.Domain.Services;

namespace WarehouseManagementSystem.API.Controllers;

[Route("api/[controller]")]
[ApiController]
public class StocksController : ControllerBase
{
    private readonly IStockService _stockService;
    private readonly IStockQueryService _stockQuery;
    private readonly IMapper _mapper;

    public StocksController(IStockService stockService, IStockQueryService stockQuery, IMapper mapper)
    {
        _stockService = stockService;
        _stockQuery = stockQuery;
        _mapper = mapper;
    }


    [HttpGet("{stockId}")]
    public async Task<ActionResult<StockDto>> GetStock(Guid stockId)
    {
        var stock = await _stockQuery.GetByIdAsync(stockId);
        if (stock == null) return NotFound();

        return Ok(_mapper.Map<StockDto>(stock));
    }

    [HttpPost("increase")]
    public async Task<IActionResult> IncreaseStock([FromBody] StockChangeDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        try
        {
            await _stockService.IncreaseStockAsync(
                dto.ProductId, dto.WarehouseId, dto.WarehouseZoneId, dto.Quantity, dto.ProductBatchId);
            return NoContent();
        }
        catch (Exception ex)
        {
            return Conflict(ex.Message);
        }
    }

    [HttpPost("decrease")]
    public async Task<IActionResult> DecreaseStock([FromBody] StockChangeDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        try
        {
            await _stockService.DecreaseStockAsync(
                dto.ProductId, dto.WarehouseId, dto.WarehouseZoneId, dto.Quantity, dto.ProductBatchId);
            return NoContent();
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPost("move")]
    public async Task<IActionResult> MoveStock([FromBody] StockMoveDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        try
        {
            await _stockService.MoveStockAsync(
                dto.ProductId,
                dto.SourceWarehouseId,
                dto.SourceZoneId,
                dto.TargetWarehouseId,
                dto.TargetZoneId,
                dto.Quantity,
                dto.ProductBatchId);
            return NoContent();
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPost("reserve")]
    public async Task<IActionResult> ReserveStock([FromBody] StockReservationRequestDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        try
        {
            await _stockService.ReserveStockAsync(
                dto.StockId, dto.Quantity, dto.ReservationSource, dto.CreatedBy, dto.ExpiresAt);
            return NoContent();
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPost("release")]
    public async Task<IActionResult> ReleaseReservation([FromBody] StockReservationReleaseDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        try
        {
            await _stockService.ReleaseReservationAsync(dto.StockId, dto.ReservationId);
            return NoContent();
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }
}