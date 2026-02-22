using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WarehouseManagementSystem.Domain.Interfaces;
using WarehouseManagementSystem.Domain.Model.InventoryDomain;

namespace WarehouseManagementSystem.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class StocksController : ControllerBase
    {
        private readonly IUnitOfWork _unitOfWork;

        public StocksController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Stock>>> GetStocks()
        {
            return Ok(await Task.FromResult(_unitOfWork.Stocks.All().ToList()));
        }

        [HttpGet("{stockId}")]
        public async Task<ActionResult<Stock>> GetStock(Guid stockId)
        {
            var stock = await _unitOfWork.Stocks.FindAsync(stockId);

            if (stock == null) { return NotFound(); }

            return stock;
        }

        [HttpPut("{stockId}")]
        public async Task<IActionResult> PutStock(Guid stockId, Stock stock)
        {
            if (stockId != stock.Id) { return BadRequest(); }

            _unitOfWork.Stocks.Update(stock);

            try
            {
                await _unitOfWork.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!StockExists(stockId)) { return NotFound(); }
                else { throw; }
            }

            return NoContent();
        }

        [HttpPost]
        public async Task<ActionResult<Stock>> PostStock(Stock stock)
        {
            _unitOfWork.Stocks.Add(stock);
            await _unitOfWork.SaveChangesAsync();

            return CreatedAtAction("GetStock", new { id = stock.Id }, stock);
        }

        [HttpDelete("{stockId}")]
        public async Task<IActionResult> DeleteStock(Guid stockId)
        {
            var stock = await _unitOfWork.Stocks.FindAsync(stockId);
            if (stock == null) { return NotFound(); }

            _unitOfWork.Stocks.Delete(stock);
            await _unitOfWork.SaveChangesAsync();

            return NoContent();
        }

        private bool StockExists(Guid id)
        {
            return _unitOfWork.Stocks.Any(e => e.Id == id);
        }
    }
}
