using WarehouseManagementSystem.API.DTO;
using WarehouseManagementSystem.Domain.Enums;
using WarehouseManagementSystem.Domain.Model.DocumentsDomain;
using WarehouseManagementSystem.Domain.Model.InventoryDomain;

namespace WarehouseManagementSystem.API.Services.Queries;

public interface IDocumentQueryService
{
    /// <summary>
    /// Pobiera stronę dokumentów z sortowaniem i filtrami.
    /// </summary>
    Task<PagedResult<DocumentListDto>> GetDocumentsPageAsync(DocumentListQuery query, CancellationToken ct = default);
    /// <summary>
    /// Pobiera dokument po Id wraz z pozycjami.
    /// </summary>
    Task<Document?> GetByIdAsync(Guid documentId, CancellationToken ct = default);

    /// <summary>
    /// Pobiera dokument po numerze.
    /// </summary>
    Task<Document?> GetByNumberAsync(string number, CancellationToken ct = default);

    /// <summary>
    /// Pobiera dokumenty wg typu i statusu.
    /// </summary>
    Task<IReadOnlyList<Document>> GetByTypeAndStatusAsync(
        DocumentType type,
        DocumentStatus status,
        CancellationToken ct = default);

    /// <summary>
    /// Pobiera dokumenty w statusie Draft (np. do pracy operatora).
    /// </summary>
    Task<IReadOnlyList<Document>> GetDraftsAsync(CancellationToken ct = default);

    /// <summary>
    /// Pobiera dokumenty wymagające potwierdzenia.
    /// </summary>
    [Obsolete]
    Task<IReadOnlyList<Document>> GetPendingConfirmationAsync(CancellationToken ct = default);

    /// <summary>
    /// Pobiera dokumenty wymagające akcji.
    /// </summary>
    Task<IReadOnlyList<DocumentListDto>> GetPendingDocumentsAsync(CancellationToken ct = default);
    /// <summary>
    /// Pobiera dokumenty powiązane z magazynem.
    /// Uwzględnia Source i Target warehouse.
    /// </summary>
    Task<IReadOnlyList<Document>> GetByWarehouseAsync(
        Guid warehouseId,
        CancellationToken ct = default);

    /// <summary>
    /// Pobiera aktywne rezerwacje przypisane do dokumentu.
    /// </summary>
    Task<IReadOnlyList<StockReservation>> GetActiveReservationsAsync(
        Guid documentId,
        CancellationToken ct = default);

    /// <summary>
    /// Sprawdza czy dokument posiada aktywne rezerwacje.
    /// </summary>
    Task<bool> HasActiveReservationsAsync(
        Guid documentId,
        CancellationToken ct = default);

    /// <summary>
    /// Pobiera ostatnie dokumenty (np. dashboard / historia).
    /// </summary>
    Task<IReadOnlyList<Document>> GetRecentAsync(
        int take,
        CancellationToken ct = default);
    Task<IReadOnlyList<Document>> GetPagedAsync(
        int page,
        int pageSize,
        CancellationToken ct = default);

}
