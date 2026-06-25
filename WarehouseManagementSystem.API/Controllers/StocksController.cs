using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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

    /// <summary>
    /// Gets a paginated list of stock records using the provided filters.
    /// </summary>
    /// <param name="query">Filtering, sorting, and pagination parameters for stock records.</param>
    /// <param name="ct">Operation cancellation token.</param>
    /// <returns>Paginated stock record list.</returns>
    [HttpHead]
    [HttpGet]
    [ResponseCache(CacheProfileName = HttpCacheProfiles.VolatileData)]
    public async Task<ActionResult<PagedResult<StockDto>>> GetStocks([FromQuery] StockListQuery query, CancellationToken ct)
    {
        var stocks = await _stockQuery.GetStocksAsync(query, ct);

        return Ok(stocks);
    }

    /// <summary>
    /// Gets the list of available stock records.
    /// </summary>
    /// <param name="ct">Operation cancellation token.</param>
    /// <returns>List of stock records with availability information.</returns>
    [HttpHead("availability")]
    [HttpGet("availability")]
    [ResponseCache(CacheProfileName = HttpCacheProfiles.VolatileData)]
    public async Task<ActionResult<IEnumerable<StockDto>>> GetStockAvailability(CancellationToken ct)
    {
        var stocks = await _stockQuery.GetStockAvailabilityAsync(ct);

        return Ok(stocks);
    }

    /// <summary>
    /// Gets stock record details by identifier.
    /// </summary>
    /// <param name="stockId">Unique stock record identifier.</param>
    /// <param name="ct">Operation cancellation token.</param>
    /// <returns>The stock record with the specified identifier, or a 404 response if it does not exist.</returns>
    [HttpHead("{stockId:guid}")]
    [HttpGet("{stockId:guid}")]
    [ResponseCache(CacheProfileName = HttpCacheProfiles.VolatileData)]
    public async Task<ActionResult<StockDto>> GetStock(Guid stockId, CancellationToken ct)
    {
        var stock = await _stockQuery.GetStockDetailsAsync(stockId, ct);
        return stock is null ? (ActionResult<StockDto>)NotFound() : (ActionResult<StockDto>)Ok(stock);
    }

    /// <summary>
    /// Returns the available HTTP methods supported by the stocks controller.
    /// </summary>
    /// <returns>Response with the Allow header containing the list of available HTTP methods.</returns>
    [HttpOptions]
    public IActionResult GetOptions()
    {
        Response.Headers.Append("Allow", "GET, HEAD, OPTIONS");
        return Ok();
    }
}
