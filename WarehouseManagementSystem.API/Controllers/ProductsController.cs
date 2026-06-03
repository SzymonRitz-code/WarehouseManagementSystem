using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WarehouseManagementSystem.API.DTO;
using WarehouseManagementSystem.API.Services.Queries;
using WarehouseManagementSystem.Domain.Interfaces;
using WarehouseManagementSystem.Domain.Model.CatalogDomain;

namespace WarehouseManagementSystem.API.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly IStockQueryService _stockQueryService;

    public ProductsController(IUnitOfWork unitOfWork, IMapper mapper, IStockQueryService stockQueryService)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _stockQueryService = stockQueryService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ProductDto>>> GetProducts()
    {
        var products = await _unitOfWork.Products.AllAsync();
        return Ok(_mapper.Map<IEnumerable<ProductDto>>(products));
    }

    [HttpGet("{productId}")]
    public async Task<ActionResult<ProductDto>> GetProduct(Guid productId)
    {
        var product = await _unitOfWork.Products.FindAsync(productId);
        if (product == null) return NotFound();

        return Ok(_mapper.Map<ProductDto>(product));
    }

    [HttpPost]
    public async Task<ActionResult<ProductDto>> CreateProduct(CreateProductDto productDto)
    {
        if (_unitOfWork.Products.Any(p => p.SKU == productDto.Sku))
        {
            ModelState.AddModelError(nameof(productDto.Sku), "Sku already exists");
        }
        if (!ModelState.IsValid) return ValidationProblem(ModelState);

        var product = new Product(
            productDto.Sku,
            productDto.Name,
            productDto.Unit,
            productDto.RequiresBatch ?? false,
            productDto.Weight,
            productDto.Volume,
            productDto.Description);


        _unitOfWork.Products.Add(product);
        await _unitOfWork.SaveChangesAsync();

        var createdDto = _mapper.Map<ProductDto>(product);
        return CreatedAtAction(nameof(GetProduct), new { productId = product.Id }, createdDto);
    }

    [HttpPut("{productId}")]
    public async Task<IActionResult> UpdateProduct([FromRoute] Guid productId, ProductDto productDto)
    {
        if (productId != productDto.Id) return BadRequest();
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var product = await _unitOfWork.Products.FindAsync(productId);
        if (product == null) return NotFound();

        product.SetName(productDto.Name);
        product.SetSku(productDto.Sku);
        product.SetUnit(productDto.Unit);

        if (productDto.RequiresBatch ?? false)
            product.RequireBatchTracking();
        else product.DisableBatchTracking();

        if (productDto.IsActive ?? false)
            product.Activate();
        else product.Deactivate();

        product.SetWeight(productDto.Weight);
        product.SetVolume(productDto.Volume);

        product.SetDescription(productDto.Description);

        try
        {
            _unitOfWork.Products.Update(product);
            await _unitOfWork.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            throw;
        }


        return NoContent();
    }

    [HttpDelete("{productId}")]
    public async Task<IActionResult> DeleteProduct(Guid productId)
    {
        var product = await _unitOfWork.Products.FindAsync(productId);
        if (product == null) return NotFound();

        _unitOfWork.Products.Delete(product);
        await _unitOfWork.SaveChangesAsync();

        return NoContent();
    }

    private bool ProductExists(Guid id) => _unitOfWork.Products.Any(p => p.Id == id);

    [HttpGet("{productId}/stocks")]
    public async Task<ActionResult<IEnumerable<StockDto>>> GetStocksForProduct(Guid productId)
    {
        var stocks = await _stockQueryService.GetByProductAsync(productId);
        return Ok(_mapper.Map<IEnumerable<StockDto>>(stocks));
    }

    [HttpGet("{productId}/stocks/available")]
    public async Task<ActionResult<decimal>> GetAvailableQuantityForProduct(Guid productId, [FromQuery] Guid warehouseId)
    {
        var available = await _stockQueryService.GetAvailableQuantityAsync(productId, null, warehouseId, null);
        return Ok(available);
    }

}