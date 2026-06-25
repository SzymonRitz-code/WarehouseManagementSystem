using WarehouseManagementSystem.API.DTO;
using WarehouseManagementSystem.Domain.Enums;
using WarehouseManagementSystem.Domain.Model.DocumentsDomain;
using WarehouseManagementSystem.Domain.Model.InventoryDomain;

namespace WarehouseManagementSystem.API.Services.Queries;

/// <summary>
/// Defines warehouse document read operations.
/// </summary>
public interface IDocumentQueryService
{
    #region Document List and Lookup Queries

    /// <summary>
    /// Gets a paginated list of documents using the provided filters.
    /// </summary>
    /// <param name="query">Filtering, sorting, and pagination parameters for documents.</param>
    /// <param name="ct">Operation cancellation token.</param>
    /// <returns>Paginated document list.</returns>
    /// <exception cref="OperationCanceledException">Thrown when the operation is canceled through <paramref name="ct"/>.</exception>
    Task<PagedResult<DocumentListDto>> GetDocumentsPageAsync(DocumentListQuery query, CancellationToken ct = default);

    /// <summary>
    /// Gets a document by identifier with related data required by the details view.
    /// </summary>
    /// <param name="documentId">Document identifier.</param>
    /// <param name="ct">Operation cancellation token.</param>
    /// <returns>Document, or <c>null</c> if it does not exist.</returns>
    /// <exception cref="OperationCanceledException">Thrown when the operation is canceled through <paramref name="ct"/>.</exception>
    Task<Document?> GetByIdAsync(Guid documentId, CancellationToken ct = default);

    /// <summary>
    /// Gets a document by number.
    /// </summary>
    /// <param name="number">Document number.</param>
    /// <param name="ct">Operation cancellation token.</param>
    /// <returns>Document, or <c>null</c> if a document with the specified number does not exist.</returns>
    /// <exception cref="OperationCanceledException">Thrown when the operation is canceled through <paramref name="ct"/>.</exception>
    Task<Document?> GetByNumberAsync(string number, CancellationToken ct = default);

    #endregion

    #region Status and Workflow Queries

    /// <summary>
    /// Gets documents matching the specified type and status.
    /// </summary>
    /// <param name="type">Document type.</param>
    /// <param name="status">Document status.</param>
    /// <param name="ct">Operation cancellation token.</param>
    /// <returns>List of documents with the specified type and status.</returns>
    /// <exception cref="OperationCanceledException">Thrown when the operation is canceled through <paramref name="ct"/>.</exception>
    Task<IReadOnlyList<Document>> GetByTypeAndStatusAsync(
        DocumentType type,
        DocumentStatus status,
        CancellationToken ct = default);

    /// <summary>
    /// Gets documents in draft status.
    /// </summary>
    /// <param name="ct">Operation cancellation token.</param>
    /// <returns>List of documents in Draft status.</returns>
    /// <exception cref="OperationCanceledException">Thrown when the operation is canceled through <paramref name="ct"/>.</exception>
    Task<IReadOnlyList<Document>> GetDraftsAsync(CancellationToken ct = default);

    /// <summary>
    /// Gets documents pending confirmation.
    /// </summary>
    /// <param name="ct">Operation cancellation token.</param>
    /// <returns>List of documents pending confirmation.</returns>
    /// <exception cref="OperationCanceledException">Thrown when the operation is canceled through <paramref name="ct"/>.</exception>
    [Obsolete]
    Task<IReadOnlyList<Document>> GetPendingConfirmationAsync(CancellationToken ct = default);

    /// <summary>
    /// Gets documents requiring operator action.
    /// </summary>
    /// <param name="ct">Operation cancellation token.</param>
    /// <returns>List of documents requiring action.</returns>
    /// <exception cref="OperationCanceledException">Thrown when the operation is canceled through <paramref name="ct"/>.</exception>
    Task<IReadOnlyList<DocumentListDto>> GetPendingDocumentsAsync(CancellationToken ct = default);

    #endregion

    #region Warehouse and Reservation Queries

    /// <summary>
    /// Gets documents related to a warehouse as either source or target warehouse.
    /// </summary>
    /// <param name="warehouseId">Warehouse identifier.</param>
    /// <param name="ct">Operation cancellation token.</param>
    /// <returns>List of documents related to the warehouse.</returns>
    /// <exception cref="OperationCanceledException">Thrown when the operation is canceled through <paramref name="ct"/>.</exception>
    Task<IReadOnlyList<Document>> GetByWarehouseAsync(
        Guid warehouseId,
        CancellationToken ct = default);

    /// <summary>
    /// Gets active reservations related to a document.
    /// </summary>
    /// <param name="documentId">Document identifier.</param>
    /// <param name="ct">Operation cancellation token.</param>
    /// <returns>List of active reservations related to the document.</returns>
    /// <exception cref="OperationCanceledException">Thrown when the operation is canceled through <paramref name="ct"/>.</exception>
    Task<IReadOnlyList<StockReservation>> GetActiveReservationsAsync(
        Guid documentId,
        CancellationToken ct = default);

    /// <summary>
    /// Checks whether a document has active reservations.
    /// </summary>
    /// <param name="documentId">Document identifier.</param>
    /// <param name="ct">Operation cancellation token.</param>
    /// <returns><c>true</c> when the document has active reservations; otherwise, <c>false</c>.</returns>
    /// <exception cref="OperationCanceledException">Thrown when the operation is canceled through <paramref name="ct"/>.</exception>
    Task<bool> HasActiveReservationsAsync(
        Guid documentId,
        CancellationToken ct = default);

    #endregion

    #region Recent and Paged Queries

    /// <summary>
    /// Gets recent documents.
    /// </summary>
    /// <param name="take">Maximum number of documents to retrieve.</param>
    /// <param name="ct">Operation cancellation token.</param>
    /// <returns>List of recent documents limited by the <paramref name="take"/> parameter.</returns>
    /// <exception cref="OperationCanceledException">Thrown when the operation is canceled through <paramref name="ct"/>.</exception>
    Task<IReadOnlyList<Document>> GetRecentAsync(
        int take,
        CancellationToken ct = default);

    /// <summary>
    /// Gets documents using simple pagination.
    /// </summary>
    /// <param name="page">Page number starting from 1.</param>
    /// <param name="pageSize">Number of items per page.</param>
    /// <param name="ct">Operation cancellation token.</param>
    /// <returns>List of documents for the specified page.</returns>
    /// <exception cref="OperationCanceledException">Thrown when the operation is canceled through <paramref name="ct"/>.</exception>
    Task<IReadOnlyList<Document>> GetPagedAsync(
        int page,
        int pageSize,
        CancellationToken ct = default);

    #endregion
}
