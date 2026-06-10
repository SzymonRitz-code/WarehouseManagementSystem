using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WarehouseManagementSystem.API.DTO;
using WarehouseManagementSystem.API.Services.AuditLogs;
using WarehouseManagementSystem.API.Services.Queries;
using WarehouseManagementSystem.API.Services.User;
using WarehouseManagementSystem.Domain.Interfaces;
using WarehouseManagementSystem.Domain.Model.InventoryDomain;
using WarehouseManagementSystem.Domain.Services;

namespace WarehouseManagementSystem.API.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class StocksController : ControllerBase
{
    private readonly IStockService _stockService;
    private readonly IStockQueryService _stockQuery;
    private readonly IMapper _mapper;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuditLogService _auditLogService;
    private readonly ILogger<StocksController> _logger;
    private readonly IUserService _userService;

    public StocksController(
        IStockService stockService,
        IStockQueryService stockQuery,
        IMapper mapper,
        IUnitOfWork unitOfWork,
        IAuditLogService auditLogService,
        ILogger<StocksController> logger, IUserService userService)
    {
        _stockService = stockService;
        _stockQuery = stockQuery;
        _mapper = mapper;
        _unitOfWork = unitOfWork;
        _auditLogService = auditLogService;
        _logger = logger;
        _userService = userService;
    }
    [HttpGet()]
    public async Task<ActionResult<StockDto>> GetStocks()
    {
        var stocks = await _stockQuery.GetStocksAsync();

        return Ok(_mapper.Map<List<StockDto>>(stocks));
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
        if (!ModelState.IsValid) return ValidationProblem(ModelState);

        try
        {
            var user = _userService.GetUser(HttpContext);
            var oldStock = await _unitOfWork.Stocks.GetByProductAndWarehouseAsNoTrackingAsync(
                dto.ProductId, dto.WarehouseId, dto.WarehouseZoneId, dto.ProductBatchId);

            await _stockService.IncreaseStockAsync(
                dto.ProductId, dto.WarehouseId, dto.WarehouseZoneId, dto.Quantity, dto.ProductBatchId);
            var newStock = await _unitOfWork.Stocks.GetByProductAndWarehouseAsync(
                dto.ProductId, dto.WarehouseId, dto.WarehouseZoneId, dto.ProductBatchId);

            await _auditLogService.LogChangesAsync(
                nameof(Stock),
                newStock!.Id,
                "Increase",
                user.Id,
                oldStock is null ? null : AuditSnapshots.Stock(oldStock),
                AuditSnapshots.Stock(newStock),
                HttpContext.Connection.RemoteIpAddress?.ToString());
            await _unitOfWork.SaveChangesAsync();
            _logger.LogInformation("Stock {StockId} increased by {Quantity} by {UserId}", newStock.Id, dto.Quantity, user.Id);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Stock increase failed for product {ProductId}", dto.ProductId);
            return Conflict(ex.Message);
        }
    }

    [HttpPost("decrease")]
    public async Task<IActionResult> DecreaseStock([FromBody] StockChangeDto dto)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);

        try
        {
            var user = _userService.GetUser(HttpContext);
            var oldStock = await _unitOfWork.Stocks.GetByProductAndWarehouseAsNoTrackingAsync(
                dto.ProductId, dto.WarehouseId, dto.WarehouseZoneId, dto.ProductBatchId);

            await _stockService.DecreaseStockAsync(
                dto.ProductId, dto.WarehouseId, dto.WarehouseZoneId, dto.Quantity, dto.ProductBatchId);
            var newStock = await _unitOfWork.Stocks.GetByProductAndWarehouseAsync(
                dto.ProductId, dto.WarehouseId, dto.WarehouseZoneId, dto.ProductBatchId);

            await _auditLogService.LogChangesAsync(
                nameof(Stock),
                newStock!.Id,
                "Decrease",
                user.Id,
                oldStock is null ? null : AuditSnapshots.Stock(oldStock),
                AuditSnapshots.Stock(newStock),
                HttpContext.Connection.RemoteIpAddress?.ToString());
            await _unitOfWork.SaveChangesAsync();
            _logger.LogInformation("Stock {StockId} decreased by {Quantity} by {UserId}", newStock.Id, dto.Quantity, user.Id);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Stock decrease failed for product {ProductId}", dto.ProductId);
            return BadRequest(ex.Message);
        }
    }

    [HttpPost("move")]
    public async Task<IActionResult> MoveStock([FromBody] StockMoveDto dto)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);

        try
        {
            var user = _userService.GetUser(HttpContext);
            var oldSourceStock = await _unitOfWork.Stocks.GetByProductAndWarehouseAsNoTrackingAsync(
                dto.ProductId, dto.SourceWarehouseId, dto.SourceZoneId, dto.ProductBatchId);
            var oldTargetStock = await _unitOfWork.Stocks.GetByProductAndWarehouseAsNoTrackingAsync(
                dto.ProductId, dto.TargetWarehouseId, dto.TargetZoneId, dto.ProductBatchId);

            await _stockService.MoveStockAsync(
                dto.ProductId,
                dto.SourceWarehouseId,
                dto.SourceZoneId,
                dto.TargetWarehouseId,
                dto.TargetZoneId,
                dto.Quantity,
                dto.ProductBatchId);
            var newSourceStock = await _unitOfWork.Stocks.GetByProductAndWarehouseAsync(
                dto.ProductId, dto.SourceWarehouseId, dto.SourceZoneId, dto.ProductBatchId);
            var newTargetStock = await _unitOfWork.Stocks.GetByProductAndWarehouseAsync(
                dto.ProductId, dto.TargetWarehouseId, dto.TargetZoneId, dto.ProductBatchId);

            await _auditLogService.LogChangesAsync(
                nameof(Stock),
                newSourceStock!.Id,
                "MoveOut",
                user.Id,
                oldSourceStock is null ? null : AuditSnapshots.Stock(oldSourceStock),
                AuditSnapshots.Stock(newSourceStock),
                HttpContext.Connection.RemoteIpAddress?.ToString());
            await _auditLogService.LogChangesAsync(
                nameof(Stock),
                newTargetStock!.Id,
                "MoveIn",
                user.Id,
                oldTargetStock is null ? null : AuditSnapshots.Stock(oldTargetStock),
                AuditSnapshots.Stock(newTargetStock),
                HttpContext.Connection.RemoteIpAddress?.ToString());
            await _unitOfWork.SaveChangesAsync();
            _logger.LogInformation(
                "Stock moved for product {ProductId}, quantity {Quantity}, by {UserId}",
                dto.ProductId,
                dto.Quantity,
                user.Id);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Stock move failed for product {ProductId}", dto.ProductId);
            return BadRequest(ex.Message);
        }
    }

    [HttpPost("reserve")]
    public async Task<IActionResult> ReserveStock([FromBody] StockReservationRequestDto dto)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);

        try
        {
            var oldStock = await _unitOfWork.Stocks.FindAsync(dto.StockId);
            var oldStockSnapshot = oldStock is null ? null : AuditSnapshots.Stock(oldStock);
            var reservation = await _stockService.ReserveStockAsync(
                dto.StockId, dto.Quantity, dto.ReservationSource, _userService.GetUser(HttpContext), dto.ExpiresAt);
            var newStock = await _unitOfWork.Stocks.FindAsync(dto.StockId);

            await _auditLogService.LogChangesAsync(
                nameof(Stock),
                dto.StockId,
                "Reserve",
                dto.CreatedBy,
                oldStockSnapshot,
                AuditSnapshots.Stock(newStock!),
                HttpContext.Connection.RemoteIpAddress?.ToString());
            await _auditLogService.LogChangesAsync(
                nameof(StockReservation),
                reservation.Id,
                "Create",
                dto.CreatedBy,
                null,
                AuditSnapshots.StockReservation(reservation),
                HttpContext.Connection.RemoteIpAddress?.ToString());
            await _unitOfWork.SaveChangesAsync();
            _logger.LogInformation("Reservation {ReservationId} created for stock {StockId} by {UserId}", reservation.Id, dto.StockId, dto.CreatedBy);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Stock reservation failed for stock {StockId}", dto.StockId);
            return BadRequest(ex.Message);
        }
    }

    [HttpPost("release")]
    public async Task<IActionResult> ReleaseReservation([FromBody] StockReservationReleaseDto dto)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);

        try
        {
            var user = _userService.GetUser(HttpContext);
            var oldStock = await _unitOfWork.Stocks.FindAsync(dto.StockId);
            var oldStockSnapshot = oldStock is null ? null : AuditSnapshots.Stock(oldStock);
            var oldReservation = (await _unitOfWork.Stocks.FindReservationsByStockIdAsync(dto.StockId))
                .FirstOrDefault(r => r.Id == dto.ReservationId);

            await _stockService.ReleaseReservationAsync(dto.StockId, dto.ReservationId);
            var newStock = await _unitOfWork.Stocks.FindAsync(dto.StockId);
            var newReservation = (await _unitOfWork.Stocks.FindReservationsByStockIdAsync(dto.StockId))
                .FirstOrDefault(r => r.Id == dto.ReservationId);

            await _auditLogService.LogChangesAsync(
                nameof(Stock),
                dto.StockId,
                "ReleaseReservation",
                user.Id,
                oldStockSnapshot,
                AuditSnapshots.Stock(newStock!),
                HttpContext.Connection.RemoteIpAddress?.ToString());
            await _auditLogService.LogChangesAsync(
                nameof(StockReservation),
                dto.ReservationId,
                "Release",
                user.Id,
                oldReservation is null ? null : AuditSnapshots.StockReservation(oldReservation),
                newReservation is null ? null : AuditSnapshots.StockReservation(newReservation),
                HttpContext.Connection.RemoteIpAddress?.ToString());
            await _unitOfWork.SaveChangesAsync();
            _logger.LogInformation("Reservation {ReservationId} released from stock {StockId} by {UserId}", dto.ReservationId, dto.StockId, user.Id);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Reservation release failed for reservation {ReservationId}", dto.ReservationId);
            return BadRequest(ex.Message);
        }
    }
}
