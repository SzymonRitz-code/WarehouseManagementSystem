using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using WarehouseManagementSystem.API.DTO;
using WarehouseManagementSystem.API.Services.Queries;
using WarehouseManagementSystem.Domain.Interfaces;
using WarehouseManagementSystem.Domain.Model.InventoryDomain;

namespace WarehouseManagementSystem.API.Controllers;

[ApiController]
[Route("api/batches")]
public class ProductBatchesController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly IProductBatchQueryService _productBatchQueryService;

    public ProductBatchesController(IUnitOfWork unitOfWork, IMapper mapper, IProductBatchQueryService productBatchQueryService)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _productBatchQueryService = productBatchQueryService;
    }


    [HttpGet("/api/products/{productId:guid}/batches")]
    public async Task<ActionResult<IEnumerable<ProductBatchListDto>>> GetBatchesByProduct([FromRoute] Guid productId, CancellationToken ct)
    {
        var batches = await _productBatchQueryService.GetProductBatchList(pb => pb.ProductId == productId, ct);
        return Ok(batches);
    }

    [HttpGet("/api/products/{productId:guid}/batches/{batchId:guid}")]
    public async Task<ActionResult<ProductBatchDto>> GetBatchByProduct([FromRoute] Guid productId, [FromRoute] Guid batchId, CancellationToken ct)
    {
        var batch = await _unitOfWork.ProductBatches.FindAsync(batchId);
        if (batch == null) return NotFound();

        return Ok(_mapper.Map<ProductBatchDto>(batch));
    }

    // ===========================
    // MODE 2 — Global
    // ===========================

    [HttpGet("/api/batches")]
    public async Task<ActionResult<IEnumerable<ProductBatchListDto>>> GetAllBatches(CancellationToken ct)
    {
        var batches = await _productBatchQueryService.GetProductBatchList(ct: ct);
        return Ok(batches);
    }

    [HttpGet("/api/batches/{batchId:guid}")]
    public async Task<ActionResult<ProductBatchListDto>> GetBatch([FromRoute] Guid batchId, CancellationToken ct)
    {
        var batch = await _productBatchQueryService.GetProductBatchList(
            pb => pb.Id == batchId, ct);

        var result = batch.FirstOrDefault();
        if (result == default) return NotFound();

        return Ok(result);
    }
    // przy wielu trybach HttpPost musi odpowiadać URL GET zwaracnemu w return inaczej aplikacja zwraca 405 not allowed
    //[HttpPost]
    [HttpPost("/api/products/{productId:guid}/batches")]
    public async Task<ActionResult<ProductBatchDto>> CreateProductBatch(CreateProductBatchDto batchDto)
    {
        if (_unitOfWork.ProductBatches.Any(p => p.BatchNumber == batchDto.BatchNumber))
        {
            ModelState.AddModelError(nameof(batchDto.BatchNumber), "BatchNumber already exists");
        }
        if (!ModelState.IsValid) return ValidationProblem(ModelState);

        ProductBatch batch;
        // TODO Dodać obługę walidacji błędów domenowych. Komentarze można robić też /// wewnątrz kodu i onzaczają dokumentację poszczególnych lini kodu
        try
        {
            batch = new ProductBatch(
                batchDto.ProductId,
                batchDto.BatchNumber,
                batchDto.ManufacturedDate,
                batchDto.ExpirationDate);

        }
        catch (Exception ex)
        {
            throw;
        }

        _unitOfWork.ProductBatches.Add(batch);
        await _unitOfWork.SaveChangesAsync();

        var createdDto = _mapper.Map<ProductBatchDto>(batch);
        // Gdy mam sub-path trzeba dodać wszystkie zeminne w route(tutaj productId i Batch) 
        // CreatedAtAction i created at route działają bardzo podobnie i korzystają z UrlHelper i oba kierują do zasobu GET
        return CreatedAtAction(nameof(GetBatchByProduct), new { productId = batch.ProductId, batchId = batch.Id }, createdDto);
    }

    [HttpPut("/api/products/{productId:guid}/batches/{batchId:guid}")]
    public async Task<IActionResult> UpdateProductBatch(Guid batchId, ProductBatchDto batchDto)
    {
        // można użyć wersję z kursu REST API. Wersja gdzie robię patch obiektu o pola które uległy zmiane => zabezpiecza przez nadpisaniem danych nie wypełnianych w formularzu
        // TODO zastosować to dla pozostałych formularzy
        // TODO extra update => poprawić walidację o middleWere i zwracane błedu można w tym uwzględnić błędy domenowe
        // Wybrałem wewrsję w której aktualizuję tylko edytowalne pola
        if (batchId != batchDto.Id) return BadRequest();
        if (!ModelState.IsValid) return BadRequest(ModelState);


        var batch = await _unitOfWork.ProductBatches.FindAsync(batchId);
        if (batch == null) return NotFound();
        if (_unitOfWork.ProductBatches.Any(p => p.Id == batchDto.Id) == false) { return BadRequest(batchDto); }

        batch.SetBatchNumber(batchDto.BatchNumber);
        batch.SetManufacturingDates(batchDto.ManufacturedDate,batchDto.ExpirationDate);
 
        try
        {
            _unitOfWork.ProductBatches.Update(batch);
            await _unitOfWork.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            throw;
        }

        // zwracam OK zamiast NoContent żeby móc korzystać z Id obiektu który edytuję
        return Ok(batch);
    }

    [HttpDelete("{batchId}")]
    public async Task<IActionResult> DeleteProductBatch(Guid batchId)
    {
        var batch = await _unitOfWork.ProductBatches.FindAsync(batchId);
        if (batch == null) return NotFound();

        _unitOfWork.ProductBatches.Delete(batch);
        await _unitOfWork.SaveChangesAsync();

        return NoContent();
    }
}