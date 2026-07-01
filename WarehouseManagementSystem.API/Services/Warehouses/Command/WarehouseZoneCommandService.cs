using WarehouseManagementSystem.API.Caching;
using WarehouseManagementSystem.API.DTO;
using WarehouseManagementSystem.API.Services.AuditLogs;
using WarehouseManagementSystem.API.Services.AuditLogs.Command;
using WarehouseManagementSystem.Domain.Interfaces;
using WarehouseManagementSystem.Domain.Model.WarehouseDomain;
using WarehouseManagementSystem.Domain.ValueObjects;

namespace WarehouseManagementSystem.API.Services.Warehouses.Command;

public class WarehouseZoneCommandService : IWarehouseZoneCommandService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuditLogCommandService _auditLogService;
    private readonly ICacheInvalidationService _cacheInvalidation;
    private readonly ILogger<WarehouseZoneCommandService> _logger;

    public WarehouseZoneCommandService(
        IUnitOfWork unitOfWork,
        IAuditLogCommandService auditLogService,
        ICacheInvalidationService cacheInvalidation,
        ILogger<WarehouseZoneCommandService> logger)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _auditLogService = auditLogService ?? throw new ArgumentNullException(nameof(auditLogService));
        _cacheInvalidation = cacheInvalidation ?? throw new ArgumentNullException(nameof(cacheInvalidation));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public Task<bool> CodeExistsAsync(string code, Guid? excludeWarehouseZoneId = null, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        var exists = excludeWarehouseZoneId.HasValue
            ? _unitOfWork.WarehouseZones.Any(wz => wz.Code == code && wz.Id != excludeWarehouseZoneId.Value)
            : _unitOfWork.WarehouseZones.Any(wz => wz.Code == code);

        return Task.FromResult(exists);
    }

    public Task<bool> ExistsAsync(Guid warehouseZoneId, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        return Task.FromResult(_unitOfWork.WarehouseZones.Any(z => z.Id == warehouseZoneId));
    }

    public async Task<WarehouseZone> CreateAsync(
        CreateWarehouseZoneDto dto,
        UserSnapshot createdBy,
        string? ipAddress = null,
        CancellationToken ct = default)
    {
        var zone = new WarehouseZone(
            dto.Code,
            dto.Name,
            dto.TemperatureType,
            dto.IsPickingZone,
            dto.WarehouseId,
            createdBy);

        _unitOfWork.WarehouseZones.Add(zone);

        await _auditLogService.LogChangesAsync(
            nameof(WarehouseZone),
            zone.Id,
            "Create",
            createdBy.Id,
            null,
            AuditSnapshots.WarehouseZone(zone),
            ipAddress,
            ct);

        await _unitOfWork.SaveChangesAsync(ct);
        await InvalidateAsync(CacheInvalidationMatrix.WarehouseZoneMutation, ct);

        _logger.LogInformation("Warehouse zone {WarehouseZoneId} created by {UserId}", zone.Id, createdBy.Id);

        return zone;
    }

    public async Task<WarehouseZone?> UpdateAsync(
        Guid warehouseZoneId,
        UpdateWarehouseZoneDto dto,
        UserSnapshot updatedBy,
        string? ipAddress = null,
        CancellationToken ct = default)
    {
        var zone = await _unitOfWork.WarehouseZones.FindAsync(warehouseZoneId);
        if (zone == null)
        {
            return null;
        }

        var oldZone = AuditSnapshots.WarehouseZone(zone);

        zone.SetCode(dto.Code);
        zone.SetName(dto.Name);
        zone.SetTemperatureType(dto.TemperatureType);
        zone.SetPickingZone(dto.IsPickingZone);
        zone.SetWarehouse(dto.WarehouseId);

        _unitOfWork.WarehouseZones.Update(zone);

        await _auditLogService.LogChangesAsync(
            nameof(WarehouseZone),
            zone.Id,
            "Update",
            updatedBy.Id,
            oldZone,
            AuditSnapshots.WarehouseZone(zone),
            ipAddress,
            ct);

        await _unitOfWork.SaveChangesAsync(ct);
        await InvalidateAsync(CacheInvalidationMatrix.WarehouseZoneMutation, ct);

        _logger.LogInformation("Warehouse zone {WarehouseZoneId} updated by {UserId}", zone.Id, updatedBy.Id);

        return zone;
    }

    public async Task<bool> DeleteAsync(
        Guid warehouseZoneId,
        UserSnapshot deletedBy,
        string? ipAddress = null,
        CancellationToken ct = default)
    {
        var zone = await _unitOfWork.WarehouseZones.FindAsync(warehouseZoneId);
        if (zone == null)
        {
            return false;
        }

        var oldZone = AuditSnapshots.WarehouseZone(zone);

        _unitOfWork.WarehouseZones.Delete(zone);

        await _auditLogService.LogChangesAsync(
            nameof(WarehouseZone),
            zone.Id,
            "Delete",
            deletedBy.Id,
            oldZone,
            null,
            ipAddress,
            ct);

        await _unitOfWork.SaveChangesAsync(ct);
        await InvalidateAsync(CacheInvalidationMatrix.WarehouseZoneMutation, ct);

        _logger.LogInformation("Warehouse zone {WarehouseZoneId} deleted by {UserId}", zone.Id, deletedBy.Id);

        return true;
    }
    private async Task InvalidateAsync(IEnumerable<string> regions, CancellationToken ct) // Czemu w DocumentCommand Service nie ma tego warunku?
                                                                                          // Bo tam nie ma potrzeby sprawdzania transakcji, a tutaj jest to potrzebne,
                                                                                          // aby uniknąć niepotrzebnego wywoływania InvalidateRegionsAsync w przypadku aktywnej transakcji.
                                                                                          // // Czemu tutaj jest lista regionów a nie jak w DocumentCommandService CacheInvalidationMatrix? 
                                                                                          // Bo tutaj chcemy mieć możliwość przekazania dynamicznej listy regionów do unieważnienia, a nie statycznej wartości z CacheInvalidationMatrix. 
                                                                                          // Ale przecież  CommandDocumentService używa transakcji, więc dlaczego tam nie ma tego warunku? 
                                                                                          // W CommandDocumentService nie ma potrzeby sprawdzania transakcji, ponieważ unieważnianie pamięci podręcznej odbywa się po zapisaniu zmian w bazie danych, a nie w trakcie aktywnej transakcji.
                                                                                          // ale tu też unieważnianie pamięci podręcznej odbywa się po zapisaniu zmian w bazie danych, więc po co ten warunek? 
                                                                                          // odpowiedz mi na pytanie w komentarzu powyżej. Warunek sprawdzający aktywną transakcję jest tutaj, aby uniknąć wywoływania metody InvalidateRegionsAsync w przypadku, gdy istnieje aktywna transakcja. W takim przypadku unieważnianie pamięci podręcznej może być niepożądane lub nieefektywne, ponieważ zmiany w bazie danych mogą jeszcze nie być zatwierdzone. W CommandDocumentService nie ma tego warunku, ponieważ tam unieważnianie pamięci podręcznej odbywa się po zapisaniu zmian w bazie danych, a nie w trakcie aktywnej transakcji, więc nie ma potrzeby sprawdzania stanu transakcji.
                                                                                          // a czemu tam jest jeden region a tytaj cała lista regionów? W CommandDocumentService unieważnianie pamięci podręcznej odbywa się dla jednego regionu, który jest związany z konkretnym dokumentem. W WarehouseZoneCommandService unieważnianie pamięci podręcznej może dotyczyć wielu regionów, ponieważ zmiany w strefach magazynowych mogą wpływać na różne obszary systemu, dlatego przekazywana jest lista regionów do unieważnienia.

    {
        if (_unitOfWork.HasActiveTransaction)
        {
            return;
        }

        await _cacheInvalidation.InvalidateRegionsAsync(regions, ct);
    }
}
