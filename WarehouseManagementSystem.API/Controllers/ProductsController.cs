using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WarehouseManagementSystem.API.DTO;
using WarehouseManagementSystem.API.Services.AuditLogs;
using WarehouseManagementSystem.API.Services.Queries;
using WarehouseManagementSystem.API.Services.User;
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
    private readonly IProductQueryService _productQueryService;
    private readonly IStockQueryService _stockQueryService;
    private readonly IAuditLogService _auditLogService;
    private readonly ILogger<ProductsController> _logger;
    private readonly IUserService _userService;

    public ProductsController(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        IProductQueryService productQueryService,
        IStockQueryService stockQueryService,
        IAuditLogService auditLogService,
        ILogger<ProductsController> logger, IUserService userService)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _productQueryService = productQueryService;
        _stockQueryService = stockQueryService;
        _auditLogService = auditLogService;
        _logger = logger;
        _userService = userService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ProductListDto>>> GetProducts(CancellationToken ct)
    {
        var products = await _productQueryService.GetProductsAsync(ct);
        return Ok(products);
    }

    [HttpGet("{productId}")]
    public async Task<ActionResult<ProductDetailsDto>> GetProduct(Guid productId, CancellationToken ct)
    {
        var product = await _productQueryService.GetProductAsync(productId, ct);
        if (product == null) return NotFound();

        return Ok(product);
    }

    [HttpPost]
    public async Task<ActionResult<ProductDetailsDto>> CreateProduct(CreateProductDto productDto)
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
            productDto.RequiresBatch,
            _userService.GetUser(HttpContext),
            productDto.Weight,
            productDto.Volume,
            productDto.Description);

        try
        {
            _unitOfWork.Products.Add(product);
            var user = _userService.GetUser(HttpContext);
            await _auditLogService.LogChangesAsync(
                nameof(Product),
                product.Id,
                "Create",
                user.Id,
                null,
                AuditSnapshots.Product(product),
                HttpContext.Connection.RemoteIpAddress?.ToString());
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Product {ProductId} created by {UserId}", product.Id, user.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Product create failed for SKU {Sku}", productDto.Sku);
            throw;
        }

        var createdDto = _mapper.Map<ProductDetailsDto>(product);
        return CreatedAtAction(nameof(GetProduct), new { productId = product.Id }, createdDto);
    }

    [HttpPut("{productId}")]
    public async Task<IActionResult> UpdateProduct([FromRoute] Guid productId, UpdateProductDto productDto)
    {
        if (productId != productDto.Id) return BadRequest("Route ID and body ID mismatch.");

        if (_unitOfWork.Products.Any(p => p.SKU == productDto.Sku && p.Id != productId))
            ModelState.AddModelError(nameof(productDto.Sku), "Sku already exists");

        if (!ModelState.IsValid) return ValidationProblem(ModelState);

        var product = await _unitOfWork.Products.FindAsync(productId);
        if (product == null) return NotFound();
        var oldProduct = AuditSnapshots.Product(product);

        product.SetName(productDto.Name);
        product.SetSku(productDto.Sku);
        product.SetUnit(productDto.Unit);

        if (productDto.RequiresBatch)
            product.RequireBatchTracking();
        else product.DisableBatchTracking();

        if (productDto.IsActive)
            product.Activate();
        else product.Deactivate();

        product.SetWeight(productDto.Weight);
        product.SetVolume(productDto.Volume);

        product.SetDescription(productDto.Description);

        try
        {
            _unitOfWork.Products.Update(product);
            var user = _userService.GetUser(HttpContext);
            await _auditLogService.LogChangesAsync(
                nameof(Product),
                product.Id,
                "Update",
                user.Id,
                oldProduct,
                AuditSnapshots.Product(product),
                HttpContext.Connection.RemoteIpAddress?.ToString());
            await _unitOfWork.SaveChangesAsync();
            _logger.LogInformation("Product {ProductId} updated by {UserId}", product.Id, user.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Product {ProductId} update failed", productId);
            throw;
        }


        return NoContent();
    }

    [HttpDelete("{productId}")]
    public async Task<IActionResult> DeleteProduct(Guid productId)
    {
        var product = await _unitOfWork.Products.FindAsync(productId);
        if (product == null) return NotFound();
        var oldProduct = AuditSnapshots.Product(product);

        try
        {
            _unitOfWork.Products.Delete(product);
            var user = _userService.GetUser(HttpContext);
            await _auditLogService.LogChangesAsync(
                nameof(Product),
                product.Id,
                "Delete",
                user.Id,
                oldProduct,
                null,
                HttpContext.Connection.RemoteIpAddress?.ToString());
            await _unitOfWork.SaveChangesAsync();
            _logger.LogInformation("Product {ProductId} deleted by {UserId}", product.Id, user.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Product delete failed for product {ProductId}", productId);
            throw;
        }

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
