using WarehouseManagementSystem.Domain.Enums;
using WarehouseManagementSystem.Domain.Model.DocumentsDomain;
using WarehouseManagementSystem.Domain.Model.InventoryDomain;
using WarehouseManagementSystem.Domain.ValueObjects;

namespace WarehouseManagementSystem.API.Services.Documents;

/// <summary>
/// Centralny serwis do zarządzania dokumentami magazynowymi.
/// Obsługuje wszystkie typy dokumentów: PZ, WZ, MM.
/// </summary>
public interface IDocumentCommandService
{
    /// <summary>
    /// Tworzy nowy dokument magazynowy.
    /// </summary>
    /// <param name="type">Typ dokumentu (PZ, WZ, MM)</param>
    /// <param name="createdById">Osoba dodająca  (w przypadku MM)</param>
    /// <param name="sourceWarehouseId">Magazyn docelowy lub źródłowy (w przypadku MM)</param>
    /// <param name="items">Pozycje dokumentu (Stock)</param>
    /// <param name="documentDate">Data dokumentu</param>
    /// <param name="targetWarehouseId">Docelowy magazyn w przypadku MM</param>
    /// <param name="notes">Notatki</param>
    /// <returns>Utworzony dokument domenowy</returns>
    Task<Document> CreateDocumentAsync(
         DocumentType type,
         Guid createdById,
         Guid sourceWarehouseId,
         IEnumerable<DocumentItemDraft> items,
         DateTime documentDate,
         Guid? targetWarehouseId = null,
         string? notes = null,
        CancellationToken ct = default);

    /// <summary>
    /// Potwierdza dokument.
    /// </summary>
    Task ConfirmDocumentAsync(Guid documentId, Guid confirmedById);

    /// <summary>
    /// Anuluje dokument.
    /// </summary>
    Task CancelDocumentAsync(Guid documentId);
} 