using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WarehouseManagementSystem.API.DTO;
using WarehouseManagementSystem.API.Services.Queries;
using WarehouseManagementSystem.Domain.Model.CatalogDomain;
using WarehouseManagementSystem.Infrastructure.Persistence;

namespace WarehouseManagementSystem.API.Controllers;

[Route("api/[controller]")]
[ApiController]
public class ProductsController : ControllerBase
{
    private readonly IStockQueryService _stockQueryService;
    private readonly IMapper _mapper;
    private readonly WarehouseManagementSystemDbContext _unitOfWork;

    public ProductsController(IStockQueryService stockQueryService,
        IMapper mapper, WarehouseManagementSystemDbContext unitOfWork)
    {
        _stockQueryService = stockQueryService;
        _mapper = mapper;
        _unitOfWork = unitOfWork;
    }
    [HttpGet("{productId}/stocks")]
    public async Task<ActionResult<IEnumerable<StockDto>>> GetStocksForProduct(Guid productId)
    {
        var stocks = await _stockQueryService.GetByProductAsync(productId);
        return Ok(_mapper.Map<IEnumerable<StockDto>>(stocks));
    }

    // GET: api/Products/{productId}/stocks/available?warehouseId=...
    [HttpGet("{productId}/stocks/available")]
    public async Task<ActionResult<decimal>> GetAvailableQuantityForProduct(Guid productId, [FromQuery] Guid warehouseId)
    {
        var available = await _stockQueryService.GetAvailableQuantityAsync(productId, null, warehouseId, null);
        return Ok(available);
    }
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Product>>> GetProducts()
    {
        return await _unitOfWork.Products.ToListAsync();
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Product>> GetProduct(Guid id)
    {
        var product = await _unitOfWork.Products.FindAsync(id);

        if (product == null) { return NotFound(); }

        return product;
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> PutProduct(Guid id, Product product)
    {
        if (id != product.Id) { return BadRequest(); }

        _unitOfWork.Entry(product).State = EntityState.Modified;

        try
        {
            await _unitOfWork.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!ProductExists(id)) { return NotFound(); }
            else { throw; }
        }

        return NoContent();
    }

    [HttpPost]
    public async Task<ActionResult<Product>> PostProduct(Product product)
    {
        _unitOfWork.Products.Add(product);
        await _unitOfWork.SaveChangesAsync();

        return CreatedAtAction("GetProduct", new { id = product.Id }, product);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteProduct(Guid id)
    {
        var product = await _unitOfWork.Products.FindAsync(id);
        if (product == null) { return NotFound(); }

        _unitOfWork.Products.Remove(product);
        await _unitOfWork.SaveChangesAsync();

        return NoContent();
    }

    private bool ProductExists(Guid id)
    {
        return _unitOfWork.Products.Any(e => e.Id == id);
    }
}
