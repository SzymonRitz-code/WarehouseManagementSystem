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

    [HttpGet]
    public async Task<ActionResult<IEnumerable<StockDto>>> GetStocks(CancellationToken ct)
    {
        var stocks = await _stockQuery.GetStocksAsync(ct);

        return Ok(stocks);
    }

    [HttpGet("{stockId:guid}")]
    public async Task<ActionResult<StockDto>> GetStock(Guid stockId, CancellationToken ct)
    {
        var stock = await _stockQuery.GetStockDetailsAsync(stockId, ct);
        if (stock is null)
            return NotFound();

        return Ok(stock);
    }
}
