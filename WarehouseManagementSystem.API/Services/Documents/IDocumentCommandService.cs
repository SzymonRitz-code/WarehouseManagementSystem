using Microsoft.AspNetCore.Http.HttpResults;
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
    /// <param name="createdBy">Osoba dodająca  (w przypadku MM)</param>
    /// <param name="sourceWarehouseId">Magazyn docelowy lub źródłowy (w przypadku MM)</param>
    /// <param name="items">Pozycje dokumentu (Stock)</param>
    /// <param name="documentDate">Data dokumentu</param>
    /// <param name="targetWarehouseId">Docelowy magazyn w przypadku MM</param>
    /// <param name="notes">Notatki</param>
    /// <returns>Utworzony dokument domenowy</returns>
    Task<Document> CreateDocumentAsync(
         DocumentType type,
         Domain.ValueObjects.UserSnapshot createdBy,
         Guid sourceWarehouseId,
         IEnumerable<DocumentItemDraft> items,
         DateTime documentDate,
         Guid? targetWarehouseId = null,
         string? notes = null,
        CancellationToken ct = default);

    /// <summary>
    /// Aktualizuje dokument dla stanu Draft
    /// </summary>
    /// <param name="documentId"></param>
    /// <param name="type"></param>
    /// <param name="sourceWarehouseId"></param>
    /// <param name="items"></param>
    /// <param name="documentDate"></param>
    /// <param name="targetWarehouseId"></param>
    /// <param name="notes"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    Task<Document> UpdateDocumentAsync(
        Guid documentId,
        UserSnapshot updatedbyBy,
        DocumentType type,
        Guid sourceWarehouseId,
        List<DocumentItemDraft> items,
        DateTime documentDate,
        Guid? targetWarehouseId,
        string? notes,
        CancellationToken ct = default);

    /// <summary>
    /// Rozpoczyna transwer.
    /// </summary>
    /// <param name="documentId"></param>
    /// <param name="userId"></param>
    Task StartTransferAsync(Guid documentId, Guid userId);

    /// <summary>
    /// Potwierdza dokument.
    /// </summary>
    Task ConfirmDocumentAsync(Guid documentId, UserSnapshot confirmedBy, CancellationToken ct = default);

    /// <summary>
    /// Anuluje dokument.
    /// </summary>
    Task CancelDocumentAsync(Guid documentId, UserSnapshot canceledBy, CancellationToken ct = default);

}