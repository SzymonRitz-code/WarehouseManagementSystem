using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using WarehouseManagementSystem.API.DTO;
using WarehouseManagementSystem.Domain.Interfaces;
using WarehouseManagementSystem.Domain.Model.InventoryDomain;

namespace WarehouseManagementSystem.API.Controllers;

[Route("api/[controller]")]
[ApiController]
public class ProductBatchesController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public ProductBatchesController(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ProductBatchDto>>> GetProductBatches()
    {
        var batches = await _unitOfWork.ProductBatches.AllAsync();
        return Ok(_mapper.Map<IEnumerable<ProductBatchDto>>(batches));
    }

    [HttpGet("{productBatchId}")]
    public async Task<ActionResult<ProductBatchDto>> GetProductBatch(Guid productBatchId)
    {
        var batch = await _unitOfWork.ProductBatches.FindAsync(productBatchId);
        if (batch == null) return NotFound();

        return Ok(_mapper.Map<ProductBatchDto>(batch));
    }

    [HttpPost]
    public async Task<ActionResult<ProductBatchDto>> CreateProductBatch(ProductBatchDto batchDto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var batch = _mapper.Map<ProductBatch>(batchDto);

        if(_unitOfWork.ProductBatches.Any(p => p.Id == batchDto.ProductId) == false) { return BadRequest(batchDto); }

        _unitOfWork.ProductBatches.Add(batch);
        await _unitOfWork.SaveChangesAsync();

        var createdDto = _mapper.Map<ProductBatchDto>(batch);
        return CreatedAtAction(nameof(GetProductBatch), new { id = batch.Id }, createdDto);
    }

    [HttpPut("{productBatchId}")]
    public async Task<IActionResult> UpdateProductBatch(Guid productBatchId, ProductBatchDto batchDto)
    {
        if (productBatchId != batchDto.Id) return BadRequest();
        if (!ModelState.IsValid) return BadRequest(ModelState);


        var batch = await _unitOfWork.ProductBatches.FindAsync(productBatchId);
        if (batch == null) return NotFound();
        if (_unitOfWork.ProductBatches.Any(p => p.Id == batchDto.ProductId) == false) { return BadRequest(batchDto); }

        _mapper.Map(batchDto, batch);

        _unitOfWork.ProductBatches.Update(batch);
        await _unitOfWork.SaveChangesAsync();

        return NoContent();
    }

    [HttpDelete("{productBatchId}")]
    public async Task<IActionResult> DeleteProductBatch(Guid productBatchId)
    {
        var batch = await _unitOfWork.ProductBatches.FindAsync(productBatchId);
        if (batch == null) return NotFound();

        _unitOfWork.ProductBatches.Delete(batch);
        await _unitOfWork.SaveChangesAsync();

        return NoContent();
    }
}