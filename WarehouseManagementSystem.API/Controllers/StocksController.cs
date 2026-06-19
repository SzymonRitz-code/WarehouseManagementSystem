using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WarehouseManagementSystem.API;
using WarehouseManagementSystem.API.DTO;
using WarehouseManagementSystem.API.Services.Queries;

namespace WarehouseManagementSystem.API.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class StocksController : ControllerBase
{
    private readonly IStockQueryService _stockQuery;

    public StocksController(IStockQueryService stockQuery)
    {
        _stockQuery = stockQuery;
    }

    [HttpGet]
    [ResponseCache(CacheProfileName = HttpCacheProfiles.VolatileData)]
    public async Task<ActionResult<PagedResult<StockDto>>> GetStocks([FromQuery] StockListQuery query, CancellationToken ct)
    {
        var stocks = await _stockQuery.GetStocksAsync(query, ct);

        return Ok(stocks);
    }

    [HttpGet("availability")]
    [ResponseCache(CacheProfileName = HttpCacheProfiles.VolatileData)]
    public async Task<ActionResult<IEnumerable<StockDto>>> GetStockAvailability(CancellationToken ct)
    {
        var stocks = await _stockQuery.GetStockAvailabilityAsync(ct);

        return Ok(stocks);
    }

    [HttpGet("{stockId:guid}")]
    [ResponseCache(CacheProfileName = HttpCacheProfiles.VolatileData)]
    public async Task<ActionResult<StockDto>> GetStock(Guid stockId, CancellationToken ct)
    {
        var stock = await _stockQuery.GetStockDetailsAsync(stockId, ct);
        if (stock is null)
            return NotFound();

        return Ok(stock);
    }
}
