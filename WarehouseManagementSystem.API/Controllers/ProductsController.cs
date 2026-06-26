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
    #region Fields and Constructor

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

    #endregion

    #region Query Actions

    /// <summary>
    /// Gets the product list.
    /// </summary>
    /// <param name="ct">Operation cancellation token.</param>
    /// <returns>Product list.</returns>
    [HttpHead]
    [HttpGet]
    [ResponseCache(CacheProfileName = HttpCacheProfiles.ReferenceData)]
    public async Task<ActionResult<IEnumerable<ProductListDto>>> GetProducts(CancellationToken ct)
    {
        var products = await _productQueryService.GetProductsAsync(ct);
        return Ok(products);
    }
    /// <summary>
    /// Gets a paginated list of products using the provided filters.
    /// </summary>
    /// <param name="query">Filtering, sorting, and pagination parameters for the product list.</param>
    /// <param name="ct">Operation cancellation token.</param>
    /// <returns>Paginated product list.</returns>
    [HttpHead("paged")]
    [HttpGet("paged")]
    [ResponseCache(CacheProfileName = HttpCacheProfiles.ReferenceData)]
    public async Task<ActionResult<PagedResult<ProductListDto>>> GetProductsPage([FromQuery] ProductListQuery query, CancellationToken ct)
    {
        var products = await _productQueryService.GetProductsPageAsync(query, ct);
        return Ok(products);
    }

    /// <summary>
    /// Gets product details by identifier.
    /// </summary>
    /// <param name="productId">Unique product identifier.</param>
    /// <param name="ct">Operation cancellation token.</param>
    /// <returns>The product with the specified identifier, or a 404 response if it does not exist.</returns>
    [HttpHead("{productId:guid}")]
    [HttpGet("{productId:guid}")] // wcześniej było [HttpGet("{productId}")] ale po dodaniu [HttpGet("paged")] api rzucało 409 co oznacza kolizję routingu,
                                  // bo nie wiedziało czy to ma być paged czy productId. Dlatego dodałem constraint :guid
    [ResponseCache(CacheProfileName = HttpCacheProfiles.ReferenceData)]
    public async Task<ActionResult<ProductDetailsDto>> GetProduct(Guid productId, CancellationToken ct)
    {
        var product = await _productQueryService.GetProductAsync(productId, ct);
        return product == null ? (ActionResult<ProductDetailsDto>)NotFound() : (ActionResult<ProductDetailsDto>)Ok(product);
    }

    #endregion

    #region Create and Update Actions

    /// <summary>
    /// Creates a new product.
    /// </summary>
    /// <remarks>
    /// The product SKU must be unique. An audit log entry is saved after the product is created.
    /// </remarks>
    /// <param name="productDto">Product data to create.</param>
    /// <returns>The created product with the URL for retrieving its details.</returns>
    [HttpPost]
    public async Task<ActionResult<ProductDetailsDto>> CreateProduct(CreateProductDto productDto)
    {
        if (_unitOfWork.Products.Any(p => p.SKU == productDto.Sku))
        {
            ModelState.AddModelError(nameof(productDto.Sku), "Sku already exists");
        }
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

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

    /// <summary>
    /// Updates an existing product.
    /// </summary>
    /// <remarks>
    /// The route identifier must match the identifier provided in the request body.
    /// An audit log entry is saved after the product is updated.
    /// </remarks>
    /// <param name="productId">Unique product identifier from the request route.</param>
    /// <param name="productDto">Product data to update.</param>
    /// <returns>A 204 response after a successful update, or a 404 response if the product does not exist.</returns>
    [HttpPut("{productId:guid}")]
    public async Task<IActionResult> UpdateProduct([FromRoute] Guid productId, UpdateProductDto productDto)
    {
        if (productId != productDto.Id)
        {
            return BadRequest("Route ID and body ID mismatch.");
        }

        if (_unitOfWork.Products.Any(p => p.SKU == productDto.Sku && p.Id != productId))
        {
            ModelState.AddModelError(nameof(productDto.Sku), "Sku already exists");
        }

        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var product = await _unitOfWork.Products.FindAsync(productId);
        if (product == null)
        {
            return NotFound();
        }

        var oldProduct = AuditSnapshots.Product(product);

        product.SetName(productDto.Name);
        product.SetSku(productDto.Sku);
        product.SetUnit(productDto.Unit);

        if (productDto.RequiresBatch)
        {
            product.RequireBatchTracking();
        }
        else
        {
            product.DisableBatchTracking();
        }

        if (productDto.IsActive)
        {
            product.Activate();
        }
        else
        {
            product.Deactivate();
        }

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

    #endregion

    #region Delete Action

    /// <summary>
    /// Deletes a product.
    /// </summary>
    /// <remarks>
    /// An audit log entry is saved after the product is deleted.
    /// </remarks>
    /// <param name="productId">Unique identifier of the product to delete.</param>
    /// <returns>A 204 response after a successful delete, or a 404 response if the product does not exist.</returns>
    [HttpDelete("{productId:guid}")]
    public async Task<IActionResult> DeleteProduct(Guid productId)
    {
        var product = await _unitOfWork.Products.FindAsync(productId);
        if (product == null)
        {
            return NotFound();
        }

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

    #endregion

    #region Stock Query Actions

    private bool ProductExists(Guid id) => _unitOfWork.Products.Any(p => p.Id == id);

    /// <summary>
    /// Gets stock records for the specified product.
    /// </summary>
    /// <param name="productId">Unique product identifier.</param>
    /// <returns>List of stock records assigned to the product.</returns>
    [HttpHead("{productId:guid}/stocks")]
    [HttpGet("{productId:guid}/stocks")]
    [ResponseCache(CacheProfileName = HttpCacheProfiles.VolatileData)]
    public async Task<ActionResult<IEnumerable<StockDto>>> GetStocksForProduct(Guid productId, CancellationToken ct)
    {
        if (!ProductExists(productId))
        {
            return NotFound();
        }

        var stocks = await _stockQueryService.GetProductStocksAsync(productId, ct);
        return Ok(stocks);
    }

    /// <summary>
    /// Gets the available product quantity in the selected warehouse.
    /// </summary>
    /// <param name="productId">Unique product identifier.</param>
    /// <param name="warehouseId">Unique warehouse identifier.</param>
    /// <returns>Available product quantity in the warehouse.</returns>
    [HttpHead("{productId:guid}/stocks/available")]
    [HttpGet("{productId:guid}/stocks/available")]
    [ResponseCache(CacheProfileName = HttpCacheProfiles.VolatileData)]
    public async Task<ActionResult<decimal>> GetAvailableQuantityForProduct(Guid productId, [FromQuery] Guid warehouseId)
    {
        var available = await _stockQueryService.GetAvailableQuantityAsync(productId, null, warehouseId, null);
        return Ok(available);
    }

    #endregion

    #region Options Action

    /// <summary>
    /// Returns the available HTTP methods supported by the products controller.
    /// </summary>
    /// <returns>Response with the Allow header containing the list of available HTTP methods.</returns>
    [HttpOptions]
    public IActionResult GetOptions()
    {
        Response.Headers.Append("Allow", "GET, HEAD, POST, PUT, DELETE, OPTIONS");
        return Ok();
    }

    #endregion
}
