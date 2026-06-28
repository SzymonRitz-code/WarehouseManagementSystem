using WarehouseManagementSystem.API.DTO;
using WarehouseManagementSystem.API.Services.AuditLogs.Command;
using WarehouseManagementSystem.API.Services.AuditLogs;
using WarehouseManagementSystem.Domain.Interfaces;
using WarehouseManagementSystem.Domain.Model.WarehouseDomain;
using WarehouseManagementSystem.Domain.ValueObjects;

namespace WarehouseManagementSystem.API.Services.Warehouses.Command;

public class WarehouseCommandService : IWarehouseCommandService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuditLogCommandService _auditLogService;
    private readonly ILogger<WarehouseCommandService> _logger;

    public WarehouseCommandService(
        IUnitOfWork unitOfWork,
        IAuditLogCommandService auditLogService,
        ILogger<WarehouseCommandService> logger)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _auditLogService = auditLogService ?? throw new ArgumentNullException(nameof(auditLogService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public bool CodeExists(string code, Guid? excludeWarehouseId = null)
    {
        return excludeWarehouseId.HasValue
            ? _unitOfWork.Warehouses.Any(w => w.Code == code && w.Id != excludeWarehouseId.Value)
            : _unitOfWork.Warehouses.Any(w => w.Code == code);
    }

    public async Task<Warehouse> CreateAsync(
        CreateWarehouseDto dto,
        UserSnapshot createdBy,
        string? ipAddress = null,
        CancellationToken ct = default)
    {
        var warehouse = new Warehouse(
            dto.Code,
            dto.Name,
            dto.Country,
            dto.City,
            dto.Address,
            createdBy);

        _unitOfWork.Warehouses.Add(warehouse);

        await _auditLogService.LogChangesAsync(
            nameof(Warehouse),
            warehouse.Id,
            "Create",
            createdBy.Id,
            null,
            AuditSnapshots.Warehouse(warehouse),
            ipAddress,
            ct);

        await _unitOfWork.SaveChangesAsync(ct);

        _logger.LogInformation("Warehouse {WarehouseId} created by {UserId}", warehouse.Id, createdBy.Id);

        return warehouse;
    }

    public async Task<Warehouse?> UpdateAsync(
        Guid warehouseId,
        UpdateWarehouseDto dto,
        UserSnapshot updatedBy,
        string? ipAddress = null,
        CancellationToken ct = default)
    {
        var warehouse = await _unitOfWork.Warehouses.FindAsync(warehouseId);
        if (warehouse == null)
        {
            return null;
        }

        var oldWarehouse = AuditSnapshots.Warehouse(warehouse);

        warehouse.SetCode(dto.Code);
        warehouse.SetName(dto.Name);
        warehouse.SetLocation(dto.Country, dto.City, dto.Address);

        if (dto.IsActive)
        {
            warehouse.Activate();
        }
        else
        {
            warehouse.Deactivate();
        }

        await _auditLogService.LogChangesAsync(
            nameof(Warehouse),
            warehouse.Id,
            "Update",
            updatedBy.Id,
            oldWarehouse,
            AuditSnapshots.Warehouse(warehouse),
            ipAddress,
            ct);

        await _unitOfWork.SaveChangesAsync(ct);

        _logger.LogInformation("Warehouse {WarehouseId} updated by {UserId}", warehouse.Id, updatedBy.Id);

        return warehouse;
    }

    public async Task<bool> DeleteAsync(
        Guid warehouseId,
        UserSnapshot deletedBy,
        string? ipAddress = null,
        CancellationToken ct = default)
    {
        var warehouse = await _unitOfWork.Warehouses.FindAsync(warehouseId);
        if (warehouse == null)
        {
            return false;
        }

        var oldWarehouse = AuditSnapshots.Warehouse(warehouse);

        _unitOfWork.Warehouses.Delete(warehouse);

        await _auditLogService.LogChangesAsync(
            nameof(Warehouse),
            warehouse.Id,
            "Delete",
            deletedBy.Id,
            oldWarehouse,
            null,
            ipAddress,
            ct);

        await _unitOfWork.SaveChangesAsync(ct);

        _logger.LogInformation("Warehouse {WarehouseId} deleted by {UserId}", warehouse.Id, deletedBy.Id);

        return true;
    }
}
