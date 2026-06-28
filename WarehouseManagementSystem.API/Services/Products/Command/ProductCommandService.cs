using WarehouseManagementSystem.API.DTO;
using WarehouseManagementSystem.API.Services.AuditLogs.Command;
using WarehouseManagementSystem.API.Services.AuditLogs;
using WarehouseManagementSystem.Domain.Interfaces;
using WarehouseManagementSystem.Domain.Model.CatalogDomain;
using WarehouseManagementSystem.Domain.ValueObjects;

namespace WarehouseManagementSystem.API.Services.Products.Command;

public class ProductCommandService : IProductCommandService
{
    #region Fields and Constructor

    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuditLogCommandService _auditLogService;
    private readonly ILogger<ProductCommandService> _logger;

    public ProductCommandService(
        IUnitOfWork unitOfWork,
        IAuditLogCommandService auditLogService,
        ILogger<ProductCommandService> logger)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _auditLogService = auditLogService ?? throw new ArgumentNullException(nameof(auditLogService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    #endregion

    #region Command Operations

    public bool SkuExists(string sku, Guid? excludeProductId = null, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        return excludeProductId.HasValue
            ? _unitOfWork.Products.Any(p => p.SKU == sku && p.Id != excludeProductId.Value)
            : _unitOfWork.Products.Any(p => p.SKU == sku);
    }

    public async Task<Product> CreateProductAsync(
        CreateProductDto dto,
        UserSnapshot createdBy,
        string? ipAddress = null,
        CancellationToken ct = default)
    {
        var product = new Product(
            dto.Sku,
            dto.Name,
            dto.Unit,
            dto.RequiresBatch,
            createdBy,
            dto.Weight,
            dto.Volume,
            dto.Description);

        _unitOfWork.Products.Add(product);

        await _auditLogService.LogChangesAsync(
            nameof(Product),
            product.Id,
            "Create",
            createdBy.Id,
            null,
            AuditSnapshots.Product(product),
            ipAddress,
            ct);

        await _unitOfWork.SaveChangesAsync(ct);

        _logger.LogInformation("Product {ProductId} created by {UserId}", product.Id, createdBy.Id);

        return product;
    }

    public async Task<Product?> UpdateProductAsync(
        Guid productId,
        UpdateProductDto dto,
        UserSnapshot updatedBy,
        string? ipAddress = null,
        CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        var product = await _unitOfWork.Products.FindAsync(productId);
        if (product == null)
        {
            return null;
        }

        var oldProduct = AuditSnapshots.Product(product);

        product.SetName(dto.Name);
        product.SetSku(dto.Sku);
        product.SetUnit(dto.Unit);

        if (dto.RequiresBatch)
        {
            product.RequireBatchTracking();
        }
        else
        {
            product.DisableBatchTracking();
        }

        if (dto.IsActive)
        {
            product.Activate();
        }
        else
        {
            product.Deactivate();
        }

        product.SetWeight(dto.Weight);
        product.SetVolume(dto.Volume);
        product.SetDescription(dto.Description);

        _unitOfWork.Products.Update(product);

        await _auditLogService.LogChangesAsync(
            nameof(Product),
            product.Id,
            "Update",
            updatedBy.Id,
            oldProduct,
            AuditSnapshots.Product(product),
            ipAddress,
            ct);

        await _unitOfWork.SaveChangesAsync(ct);

        _logger.LogInformation("Product {ProductId} updated by {UserId}", product.Id, updatedBy.Id);

        return product;
    }

    public async Task<bool> DeleteProductAsync(
        Guid productId,
        UserSnapshot deletedBy,
        string? ipAddress = null,
        CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        var product = await _unitOfWork.Products.FindAsync(productId);
        if (product == null)
        {
            return false;
        }

        var oldProduct = AuditSnapshots.Product(product);

        _unitOfWork.Products.Delete(product);

        await _auditLogService.LogChangesAsync(
            nameof(Product),
            product.Id,
            "Delete",
            deletedBy.Id,
            oldProduct,
            null,
            ipAddress,
            ct);

        await _unitOfWork.SaveChangesAsync(ct);

        _logger.LogInformation("Product {ProductId} deleted by {UserId}", product.Id, deletedBy.Id);

        return true;
    }

    #endregion
}
