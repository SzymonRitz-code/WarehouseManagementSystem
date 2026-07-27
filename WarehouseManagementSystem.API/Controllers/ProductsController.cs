using Asp.Versioning;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WarehouseManagementSystem.API.DTO;
using WarehouseManagementSystem.API.Services.Products.Command;
using WarehouseManagementSystem.API.Services.Products.Query;
using WarehouseManagementSystem.API.Services.Stocks.Query;
using WarehouseManagementSystem.API.Services.User;

namespace WarehouseManagementSystem.API.Controllers;

[Authorize]
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public class ProductsController : ControllerBase
{
    #region Fields and Constructor

    private readonly IProductCommandService _productCommandService;
    private readonly IMapper _mapper;
    private readonly IProductQueryService _productQueryService;
    private readonly IStockQueryService _stockQueryService;
    private readonly IUserService _userService;

    public ProductsController(
        IProductCommandService productCommandService,
        IMapper mapper,
        IProductQueryService productQueryService,
        IStockQueryService stockQueryService,
        IUserService userService)
    {
        _productCommandService = productCommandService;
        _mapper = mapper;
        _productQueryService = productQueryService;
        _stockQueryService = stockQueryService;
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
    public async Task<ActionResult<ProductDetailsDto>> CreateProduct(CreateProductDto productDto, CancellationToken ct)
    {
        if (_productCommandService.SkuExists(productDto.Sku!, ct: ct))
        {
            ModelState.AddModelError(nameof(productDto.Sku), "Sku already exists");
            return ValidationProblem(ModelState);
        }

        var user = _userService.GetUser(HttpContext);
        var product = await _productCommandService.CreateProductAsync(
            productDto,
            user,
            HttpContext.Connection.RemoteIpAddress?.ToString(),
            ct);

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
    public async Task<IActionResult> UpdateProduct([FromRoute] Guid productId, UpdateProductDto productDto, CancellationToken ct)
    {
        if (productId != productDto.Id)
        {
            return BadRequest("Route ID and body ID mismatch.");
        }

        if (_productCommandService.SkuExists(productDto.Sku!, productId, ct))
        {
            ModelState.AddModelError(nameof(productDto.Sku), "Sku already exists");
            return ValidationProblem(ModelState);
        }

        var user = _userService.GetUser(HttpContext);
        var updated = await _productCommandService.UpdateProductAsync(
            productId,
            productDto,
            user,
            HttpContext.Connection.RemoteIpAddress?.ToString(),
            ct);

        if (updated == null)
        {
            return NotFound();
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
    public async Task<IActionResult> DeleteProduct(Guid productId, CancellationToken ct)
    {
        var user = _userService.GetUser(HttpContext);
        var deleted = await _productCommandService.DeleteProductAsync(
            productId,
            user,
            HttpContext.Connection.RemoteIpAddress?.ToString(),
            ct);

        return deleted ? NoContent() : NotFound();
    }

    #endregion

    #region Stock Query Actions

    private async Task<bool> ProductExistsAsync(Guid id, CancellationToken ct)
    {
        return await _productQueryService.GetProductAsync(id, ct) != null;
    }

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
        if (!await ProductExistsAsync(productId, ct))
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
    public async Task<ActionResult<decimal>> GetAvailableQuantityForProduct(Guid productId, [FromQuery] Guid warehouseId, CancellationToken ct = default)
    {
        var available = await _stockQueryService.GetAvailableQuantityAsync(productId, null, warehouseId, null, ct);
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
