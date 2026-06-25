using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WarehouseManagementSystem.API.DTO;
using WarehouseManagementSystem.API.Services.AuditLogs;
using WarehouseManagementSystem.API.Services.Queries;
using WarehouseManagementSystem.API.Services.User;
using WarehouseManagementSystem.Domain.Interfaces;
using WarehouseManagementSystem.Domain.Model.InventoryDomain;

namespace WarehouseManagementSystem.API.Controllers;

[Authorize]
[ApiController]
[Route("api/batches")]
public class ProductBatchesController : ControllerBase
{
    #region Fields and Constructor

    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly IProductBatchQueryService _productBatchQueryService;
    private readonly IAuditLogService _auditLogService;
    private readonly ILogger<ProductBatchesController> _logger;
    private readonly IUserService _userService;

    public ProductBatchesController(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        IProductBatchQueryService productBatchQueryService,
        IAuditLogService auditLogService,
        ILogger<ProductBatchesController> logger, IUserService userService)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _productBatchQueryService = productBatchQueryService;
        _auditLogService = auditLogService;
        _logger = logger;
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
        var batches = await _productBatchQueryService.GetProductBatchList(pb => pb.ProductId == productId, ct);
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
        var batch = await _productBatchQueryService.GetProductBatchDetails(batchId, ct);
        return batch == null
            ? (ActionResult<ProductBatchDto>)NotFound()
            : batch.ProductId != productId ? (ActionResult<ProductBatchDto>)NotFound() : (ActionResult<ProductBatchDto>)Ok(batch);
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
        var batches = await _productBatchQueryService.GetProductBatchList(ct: ct);
        return Ok(batches);
    }

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
        var batch = await _productBatchQueryService.GetProductBatchList(
            pb => pb.Id == batchId, ct);

        var result = batch.FirstOrDefault();
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
    public async Task<ActionResult<ProductBatchDto>> CreateProductBatch(CreateProductBatchDto batchDto)
    {
        if (_unitOfWork.ProductBatches.Any(p => p.BatchNumber == batchDto.BatchNumber))
        {
            ModelState.AddModelError(nameof(batchDto.BatchNumber), "BatchNumber already exists");
        }
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        ProductBatch batch;
        try
        {
            batch = new ProductBatch(
                batchDto.ProductId,
                batchDto.BatchNumber,
                _userService.GetUser(HttpContext),
                batchDto.ManufacturedDate,
                batchDto.ExpirationDate);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Product batch create failed for product {ProductId} and batch {BatchNumber}", batchDto.ProductId, batchDto.BatchNumber);
            throw;
        }

        try
        {
            _unitOfWork.ProductBatches.Add(batch);
            var user = _userService.GetUser(HttpContext);
            await _auditLogService.LogChangesAsync(
                nameof(ProductBatch),
                batch.Id,
                "Create",
                user.Id,
                null,
                AuditSnapshots.ProductBatch(batch),
                HttpContext.Connection.RemoteIpAddress?.ToString());
            await _unitOfWork.SaveChangesAsync();
            _logger.LogInformation("Product batch {BatchId} created by {UserId}", batch.Id, user.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Product batch persistence failed for batch {BatchId}", batch.Id);
            throw;
        }

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
    public async Task<ActionResult<ProductBatchDto>> UpdateProductBatch(Guid productId, Guid batchId, UpdateProductBatchDto batchDto)
    {
        if (batchId != batchDto.Id)
        {
            return BadRequest("Route ID and body ID mismatch.");
        }

        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var batch = await _unitOfWork.ProductBatches.FindAsync(batchId);
        if (batch == null)
        {
            return NotFound();
        }

        if (batch.ProductId != productId)
        {
            return BadRequest("Product batch does not belong to the route product.");
        }

        var oldBatch = AuditSnapshots.ProductBatch(batch);

        batch.SetBatchNumber(batchDto.BatchNumber);
        batch.SetManufacturingDates(batchDto.ManufacturedDate, batchDto.ExpirationDate);

        try
        {
            _unitOfWork.ProductBatches.Update(batch);
            var user = _userService.GetUser(HttpContext);
            await _auditLogService.LogChangesAsync(
                nameof(ProductBatch),
                batch.Id,
                "Update",
                user.Id,
                oldBatch,
                AuditSnapshots.ProductBatch(batch),
                HttpContext.Connection.RemoteIpAddress?.ToString());
            await _unitOfWork.SaveChangesAsync();
            _logger.LogInformation("Product batch {BatchId} updated by {UserId}", batch.Id, user.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Product batch {BatchId} update failed", batchId);
            throw;
        }

        var updatedBatch = await _productBatchQueryService.GetProductBatchDetails(batchId);
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
    public async Task<IActionResult> DeleteProductBatch(Guid batchId)
    {
        var batch = await _unitOfWork.ProductBatches.FindAsync(batchId);
        if (batch == null)
        {
            return NotFound();
        }

        var oldBatch = AuditSnapshots.ProductBatch(batch);

        try
        {
            _unitOfWork.ProductBatches.Delete(batch);
            var user = _userService.GetUser(HttpContext);
            await _auditLogService.LogChangesAsync(
                nameof(ProductBatch),
                batch.Id,
                "Delete",
                user.Id,
                oldBatch,
                null,
                HttpContext.Connection.RemoteIpAddress?.ToString());
            await _unitOfWork.SaveChangesAsync();
            _logger.LogInformation("Product batch {BatchId} deleted by {UserId}", batch.Id, user.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Product batch delete failed for batch {BatchId}", batchId);
            throw;
        }

        return NoContent();
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
