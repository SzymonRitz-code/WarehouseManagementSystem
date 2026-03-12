using WarehouseManagementSystem.Domain.Enums;

namespace WarehouseManagementSystem.Domain.Services;

public interface IDocumentNumberGenerator
{
    Task<string> GenerateAsync(
        DocumentType type,
        Guid? warehouseId,
        DateTimeOffset documentDate);

    string FormatPreview(
        DocumentType type,
        int sequence,
        DateTimeOffset documentDate,
        string? warehouseCode = null);
}
