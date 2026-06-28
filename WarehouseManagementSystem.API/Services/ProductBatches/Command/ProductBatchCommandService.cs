using WarehouseManagementSystem.API.DTO;
using WarehouseManagementSystem.API.Services.AuditLogs;
using WarehouseManagementSystem.API.Services.AuditLogs.Command;
using WarehouseManagementSystem.Domain.Interfaces;
using WarehouseManagementSystem.Domain.Model.InventoryDomain;
using WarehouseManagementSystem.Domain.ValueObjects;

namespace WarehouseManagementSystem.API.Services.ProductBatches.Command;

public class ProductBatchCommandService : IProductBatchCommandService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuditLogCommandService _auditLogService;
    private readonly ILogger<ProductBatchCommandService> _logger;

    public ProductBatchCommandService(
        IUnitOfWork unitOfWork,
        IAuditLogCommandService auditLogService,
        ILogger<ProductBatchCommandService> logger)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _auditLogService = auditLogService ?? throw new ArgumentNullException(nameof(auditLogService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public Task<bool> BatchNumberExistsAsync(string batchNumber, Guid? excludeBatchId = null, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        var exists = excludeBatchId.HasValue
            ? _unitOfWork.ProductBatches.Any(p => p.BatchNumber == batchNumber && p.Id != excludeBatchId.Value)
            : _unitOfWork.ProductBatches.Any(p => p.BatchNumber == batchNumber);

        return Task.FromResult(exists);
    }

    public async Task<ProductBatch> CreateAsync(
        CreateProductBatchDto dto,
        UserSnapshot createdBy,
        string? ipAddress = null,
        CancellationToken ct = default)
    {
        var batch = new ProductBatch(
            dto.ProductId,
            dto.BatchNumber,
            createdBy,
            dto.ManufacturedDate,
            dto.ExpirationDate);

        _unitOfWork.ProductBatches.Add(batch);

        await _auditLogService.LogChangesAsync(
            nameof(ProductBatch),
            batch.Id,
            "Create",
            createdBy.Id,
            null,
            AuditSnapshots.ProductBatch(batch),
            ipAddress,
            ct);

        await _unitOfWork.SaveChangesAsync(ct);

        _logger.LogInformation("Product batch {BatchId} created by {UserId}", batch.Id, createdBy.Id);

        return batch;
    }

    public async Task<ProductBatch?> UpdateAsync(
        Guid productId,
        Guid batchId,
        UpdateProductBatchDto dto,
        UserSnapshot updatedBy,
        string? ipAddress = null,
        CancellationToken ct = default)
    {
        var batch = await _unitOfWork.ProductBatches.FindAsync(batchId);
        if (batch == null)
        {
            return null;
        }

        if (batch.ProductId != productId)
        {
            throw new InvalidOperationException("Product batch does not belong to the route product.");
        }

        var oldBatch = AuditSnapshots.ProductBatch(batch);

        batch.SetBatchNumber(dto.BatchNumber);
        batch.SetManufacturingDates(dto.ManufacturedDate, dto.ExpirationDate);

        _unitOfWork.ProductBatches.Update(batch);

        await _auditLogService.LogChangesAsync(
            nameof(ProductBatch),
            batch.Id,
            "Update",
            updatedBy.Id,
            oldBatch,
            AuditSnapshots.ProductBatch(batch),
            ipAddress,
            ct);

        await _unitOfWork.SaveChangesAsync(ct);

        _logger.LogInformation("Product batch {BatchId} updated by {UserId}", batch.Id, updatedBy.Id);

        return batch;
    }

    public async Task<bool> DeleteAsync(
        Guid batchId,
        UserSnapshot deletedBy,
        string? ipAddress = null,
        CancellationToken ct = default)
    {
        var batch = await _unitOfWork.ProductBatches.FindAsync(batchId);
        if (batch == null)
        {
            return false;
        }

        var oldBatch = AuditSnapshots.ProductBatch(batch);

        _unitOfWork.ProductBatches.Delete(batch);

        await _auditLogService.LogChangesAsync(
            nameof(ProductBatch),
            batch.Id,
            "Delete",
            deletedBy.Id,
            oldBatch,
            null,
            ipAddress,
            ct);

        await _unitOfWork.SaveChangesAsync(ct);

        _logger.LogInformation("Product batch {BatchId} deleted by {UserId}", batch.Id, deletedBy.Id);

        return true;
    }
}
