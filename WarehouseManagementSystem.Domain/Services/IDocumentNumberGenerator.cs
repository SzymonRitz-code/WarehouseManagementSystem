using WarehouseManagementSystem.Domain.Enums;

namespace WarehouseManagementSystem.Domain.Services;

/// <summary>
/// Defines operations for generating and formatting warehouse document numbers.
/// </summary>
public interface IDocumentNumberGenerator
{
    /// <summary>
    /// Generates the next document number for the type, year, and optional warehouse.
    /// </summary>
    /// <param name="type">Document type for which the number is generated.</param>
    /// <param name="warehouseId">Optional warehouse identifier used for warehouse-specific numbering.</param>
    /// <param name="documentDate">Document date that determines the numbering year.</param>
    /// <returns>The generated document number.</returns>
    /// <exception cref="InvalidOperationException">Thrown when more than one sequence matches the type, year, and warehouse.</exception>
    Task<string> GenerateAsync(
        DocumentType type,
        Guid? warehouseId,
        DateTimeOffset documentDate);

    /// <summary>
    /// Formats a document number preview without modifying the numbering sequence.
    /// </summary>
    /// <param name="type">Document type.</param>
    /// <param name="sequence">Sequence number.</param>
    /// <param name="documentDate">Document date that determines the numbering year.</param>
    /// <param name="warehouseCode">Optional warehouse code added to the number.</param>
    /// <returns>Formatted document number.</returns>
    string FormatPreview(
        DocumentType type,
        int sequence,
        DateTimeOffset documentDate,
        string? warehouseCode = null);
}
