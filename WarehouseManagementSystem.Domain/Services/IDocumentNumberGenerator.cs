using WarehouseManagementSystem.Domain.Enums;

namespace WarehouseManagementSystem.Domain.Services;

public interface IDocumentNumberGenerator
{
    Task<string> GenerateAsync(DocumentType type);

    Task<string> GenerateAsync(
        DocumentType type,
        Guid? warehouseId,
        DateTime documentDate);

    string FormatPreview(
        DocumentType type,
        int sequence,
        DateTime documentDate,
        string? warehouseCode = null);
}
