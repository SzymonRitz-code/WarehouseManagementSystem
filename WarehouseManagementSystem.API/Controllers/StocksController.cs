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
    private readonly IStockReservationService _reservationService;
    private readonly IMapper _mapper;

    public StocksController(
        IStockService stockService,
        IStockQueryService stockQuery,
        IStockReservationService reservationService,
        IMapper mapper)
    {
        _stockService = stockService;
        _stockQuery = stockQuery;
        _reservationService = reservationService;
        _mapper = mapper;
    }

    // ===== QUERY OPERATIONS =====

    [HttpGet("{stockId}")]
    public async Task<ActionResult<StockDto>> GetStock(Guid stockId)
    {
        var stock = await _stockQuery.GetByIdAsync(stockId);
        if (stock == null) return NotFound();

        return Ok(_mapper.Map<StockDto>(stock));
    }

    [HttpGet("product/{productId}")]
    public async Task<ActionResult<IEnumerable<StockDto>>> GetByProduct(Guid productId)
    {
        var stocks = await _stockQuery.GetByProductAsync(productId);
        return Ok(_mapper.Map<IEnumerable<StockDto>>(stocks));
    }

    [HttpGet("warehouse/{warehouseId}/available")]
    public async Task<ActionResult<IEnumerable<StockDto>>> GetAvailableForPicking(Guid warehouseId)
    {
        var stocks = await _stockQuery.GetAvailableForPickingAsync(warehouseId);
        return Ok(_mapper.Map<IEnumerable<StockDto>>(stocks));
    }

    [HttpGet("product/{productId}/warehouse/{warehouseId}")]
    public async Task<ActionResult<decimal>> GetAvailableQuantity(
        Guid productId,
        Guid warehouseId,
        [FromQuery] Guid? batchId = null,
        [FromQuery] Guid? zoneId = null)
    {
        var available = await _stockQuery.GetAvailableQuantityAsync(
            productId, batchId, warehouseId, zoneId);
        return Ok(available);
    }

    // ===== COMMAND OPERATIONS =====

    [HttpPost("increase")]
    public async Task<IActionResult> IncreaseStock([FromBody] StockChangeDto dto)
    {
        await _stockService.IncreaseStockAsync(
            dto.ProductId, dto.WarehouseId, dto.WarehouseZoneId, dto.Quantity, dto.ProductBatchId);
        return NoContent();
    }

    [HttpPost("decrease")]
    public async Task<IActionResult> DecreaseStock([FromBody] StockChangeDto dto)
    {
        await _stockService.DecreaseStockAsync(
            dto.ProductId, dto.WarehouseId, dto.WarehouseZoneId, dto.Quantity, dto.ProductBatchId);
        return NoContent();
    }

    [HttpPost("move")]
    public async Task<IActionResult> MoveStock([FromBody] StockMoveDto dto)
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

    [HttpPost("reserve")]
    public async Task<IActionResult> ReserveStock([FromBody] StockReservationRequestDto dto)
    {
        await _stockService.ReserveStockAsync(
            dto.StockId, dto.Quantity, dto.ReservationSource, dto.CreatedBy, dto.ExpiresAt);
        return NoContent();
    }

    [HttpPost("release")]
    public async Task<IActionResult> ReleaseReservation([FromBody] StockReservationReleaseDto dto)
    {
        await _stockService.ReleaseReservationAsync(dto.StockId, dto.Quantity);
        return NoContent();
    }
}