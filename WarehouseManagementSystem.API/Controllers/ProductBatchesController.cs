using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WarehouseManagementSystem.API.DTO;
  using WarehouseManagementSystem.API.Services.ProductBatches.Command;
using WarehouseManagementSystem.API.Services.ProductBatches.Query;
using WarehouseManagementSystem.API.Services.User;

namespace WarehouseManagementSystem.API.Controllers;

[Authorize]
[ApiController]
[Route("api/batches")]
public class ProductBatchesController : ControllerBase
{
    #region Fields and Constructor

    private readonly IMapper _mapper;
    private readonly IProductBatchQueryService _productBatchQueryService;
    private readonly IProductBatchCommandService _productBatchCommandService;
    private readonly IUserService _userService;

    public ProductBatchesController(
        IMapper mapper,
        IProductBatchQueryService productBatchQueryService,
        IProductBatchCommandService productBatchCommandService,
        IUserService userService)
    {
        _mapper = mapper;
        _productBatchQueryService = productBatchQueryService;
        _productBatchCommandService = productBatchCommandService;
        _userService = userService;
    }

    #endregion

    #region Query Actions

    /// <summary>
    /// Gets batches assigned to the specified product.
    /// </summary>
    /// <param name="productId">Unique product identifier.</param>
    /// <param name="ct">Operation cancellation token.</param>
    /// <returns>Product batch list.</returns>
    [HttpHead("/api/products/{productId:guid}/batches")]
    [HttpGet("/api/products/{productId:guid}/batches")]
    [ResponseCache(CacheProfileName = HttpCacheProfiles.ReferenceData)]
    public async Task<ActionResult<IEnumerable<ProductBatchListDto>>> GetBatchesByProduct([FromRoute] Guid productId, CancellationToken ct)
    {
        var batches = await _productBatchQueryService.GetBatchesByProductAsync(productId, ct);
        return Ok(batches);
    }

    /// <summary>
    /// Gets batch details in the context of the specified product.
    /// </summary>
    /// <param name="productId">Unique product identifier.</param>
    /// <param name="batchId">Unique product batch identifier.</param>
    /// <param name="ct">Operation cancellation token.</param>
    /// <returns>The product batch, or a 404 response if the batch does not exist or does not belong to the product.</returns>
    [HttpHead("/api/products/{productId:guid}/batches/{batchId:guid}")]
    [HttpGet("/api/products/{productId:guid}/batches/{batchId:guid}")]
    [ResponseCache(CacheProfileName = HttpCacheProfiles.ReferenceData)]
    public async Task<ActionResult<ProductBatchDto>> GetBatchByProduct([FromRoute] Guid productId, [FromRoute] Guid batchId, CancellationToken ct)
    {
        var batch = await _productBatchQueryService.GetBatchForProductAsync(productId, batchId, ct);
        return batch == null ? (ActionResult<ProductBatchDto>)NotFound() : (ActionResult<ProductBatchDto>)Ok(batch);
    }

    /// <summary>
    /// Gets the list of all product batches.
    /// </summary>
    /// <param name="ct">Operation cancellation token.</param>
    /// <returns>Product batch list.</returns>
    [HttpHead("/api/batches")]
    [HttpGet("/api/batches")]
    [ResponseCache(CacheProfileName = HttpCacheProfiles.ReferenceData)]
    public async Task<ActionResult<IEnumerable<ProductBatchListDto>>> GetAllBatches(CancellationToken ct)
    {
        var batches = await _productBatchQueryService.GetBatchesAsync(ct);
        return Ok(batches);
    }
    // TODO : Finish Get Batches to be consistent with UI implementation
    /// <summary>
    /// Gets a product batch by identifier.
    /// </summary>
    /// <param name="batchId">Unique product batch identifier.</param>
    /// <param name="ct">Operation cancellation token.</param>
    /// <returns>The product batch, or a 404 response if it does not exist.</returns>
    [HttpHead("/api/batches/{batchId:guid}")]
    [HttpGet("/api/batches/{batchId:guid}")]
    [ResponseCache(CacheProfileName = HttpCacheProfiles.ReferenceData)]
    public async Task<ActionResult<ProductBatchListDto>> GetBatch([FromRoute] Guid batchId, CancellationToken ct)
    {
        var result = await _productBatchQueryService.GetBatchListItemAsync(batchId, ct);
        return result == default ? (ActionResult<ProductBatchListDto>)NotFound() : (ActionResult<ProductBatchListDto>)Ok(result);
    }

    #endregion

    #region Create, Update and Delete Actions

    /// <summary>
    /// Creates a new product batch.
    /// </summary>
    /// <remarks>
    /// The batch number must be unique. An audit log entry is saved after the batch is created.
    /// </remarks>
    /// <param name="batchDto">Product batch data to create.</param>
    /// <returns>The created product batch with the URL for retrieving its details.</returns>
    [HttpPost("/api/products/{productId:guid}/batches")]
    public async Task<ActionResult<ProductBatchDto>> CreateProductBatch(Guid productId, CreateProductBatchDto batchDto, CancellationToken ct)
    {
        if (productId != batchDto.ProductId)
        {
            return BadRequest("Route ID and body Product ID mismatch.");
        }

        if (await _productBatchCommandService.BatchNumberExistsAsync(batchDto.BatchNumber, null, ct))
        {
            ModelState.AddModelError(nameof(batchDto.BatchNumber), "BatchNumber already exists");
        }

        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var user = _userService.GetUser(HttpContext);
        var batch = await _productBatchCommandService.CreateAsync(
            batchDto,
            user,
            HttpContext.Connection.RemoteIpAddress?.ToString(),
            ct);

        var createdDto = _mapper.Map<ProductBatchDto>(batch);
        return CreatedAtAction(nameof(GetBatchByProduct), new { productId = batch.ProductId, batchId = batch.Id }, createdDto);
    }

    /// <summary>
    /// Updates an existing product batch.
    /// </summary>
    /// <remarks>
    /// The batch identifier from the route must match the identifier provided in the request body.
    /// The batch must belong to the product specified in the route.
    /// </remarks>
    /// <param name="productId">Unique product identifier from the request route.</param>
    /// <param name="batchId">Unique batch identifier from the request route.</param>
    /// <param name="batchDto">Product batch data to update.</param>
    /// <returns>The updated product batch, or a validation response if the data is invalid.</returns>
    [HttpPut("/api/products/{productId:guid}/batches/{batchId:guid}")]
    public async Task<ActionResult<ProductBatchDto>> UpdateProductBatch(Guid productId, Guid batchId, UpdateProductBatchDto batchDto, CancellationToken ct)
    {
        if (batchId != batchDto.Id)
        {
            return BadRequest("Route ID and body ID mismatch.");
        }

        if (await _productBatchCommandService.BatchNumberExistsAsync(batchDto.BatchNumber, batchId, ct))
        {
            ModelState.AddModelError(nameof(batchDto.BatchNumber), "BatchNumber already exists");
        }

        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var user = _userService.GetUser(HttpContext);

        try
        {
            var updated = await _productBatchCommandService.UpdateAsync(
                productId,
                batchId,
                batchDto,
                user,
                HttpContext.Connection.RemoteIpAddress?.ToString(),
                ct);

            if (updated == null)
            {
                return NotFound();
            }
        }
        catch (InvalidOperationException)
        {
            return BadRequest("Product batch does not belong to the route product.");
        }

        var updatedBatch = await _productBatchQueryService.GetBatchAsync(batchId, ct);
        return Ok(updatedBatch);
    }

    /// <summary>
    /// Deletes a product batch.
    /// </summary>
    /// <remarks>
    /// An audit log entry is saved after the product batch is deleted.
    /// </remarks>
    /// <param name="batchId">Unique identifier of the product batch to delete.</param>
    /// <returns>A 204 response after a successful delete, or a 404 response if the batch does not exist.</returns>
    [HttpDelete("{batchId}")]
    public async Task<IActionResult> DeleteProductBatch(Guid batchId, CancellationToken ct)
    {
        var user = _userService.GetUser(HttpContext);
        var deleted = await _productBatchCommandService.DeleteAsync(
            batchId,
            user,
            HttpContext.Connection.RemoteIpAddress?.ToString(),
            ct);

        return deleted ? NoContent() : NotFound();
    }

    #endregion

    #region Options Action

    /// <summary>
    /// Returns the available HTTP methods supported by the product batches controller.
    /// </summary>
    /// <returns>Response with the Allow header containing the list of available HTTP methods.</returns>
    [HttpOptions]
    [HttpOptions("{batchId:guid}")]
    [HttpOptions("/api/products/{productId:guid}/batches")]
    [HttpOptions("/api/products/{productId:guid}/batches/{batchId:guid}")]
    public IActionResult GetOptions()
    {
        Response.Headers.Append("Allow", "GET, HEAD, POST, PUT, DELETE, OPTIONS");
        return Ok();
    }

    #endregion
}
