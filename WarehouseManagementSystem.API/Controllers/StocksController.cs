using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WarehouseManagementSystem.API.DTO;
using WarehouseManagementSystem.Domain.Interfaces;
using WarehouseManagementSystem.Domain.Model.InventoryDomain;

namespace WarehouseManagementSystem.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class StocksController : ControllerBase
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _autoMapper;

        public StocksController(IUnitOfWork unitOfWork, IMapper autoMapper)
        {
            _unitOfWork = unitOfWork;
            _autoMapper = autoMapper;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<StockDto>>> GetStocks()
        {
            return Ok(_autoMapper.Map<IEnumerable<StockDto>>(await _unitOfWork.Stocks.All()));
        }

        [HttpGet("{stockId}")]
        public async Task<ActionResult<StockDto>> GetStock(Guid stockId)
        {
            var stockEntity = await _unitOfWork.Stocks.FindAsync(stockId);

            if (stockEntity == null) { return NotFound(); }

            var stock = _autoMapper.Map<StockDto>(stockEntity);

            return stock;
        }

        [HttpPut("{stockId}")]
        public async Task<IActionResult> PutStock(Guid stockId, StockDto stock)
        {
            if (stockId != stock.Id) { return BadRequest(); }

            var stockEntity = _autoMapper.Map<Stock>(stock);

            _unitOfWork.Stocks.Update(stockEntity);

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
        public async Task<ActionResult<Stock>> PostStock(StockDto stock)
        {
            var stockEntity = _autoMapper.Map<Stock>(stock);
            _unitOfWork.Stocks.Add(stockEntity);
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
