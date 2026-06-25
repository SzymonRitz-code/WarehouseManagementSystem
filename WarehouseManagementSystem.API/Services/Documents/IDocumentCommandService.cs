using WarehouseManagementSystem.Domain.Enums;
using WarehouseManagementSystem.Domain.Exceptions;
using WarehouseManagementSystem.Domain.Model.DocumentsDomain;
using WarehouseManagementSystem.Domain.ValueObjects;

namespace WarehouseManagementSystem.API.Services.Documents;

/// <summary>
/// Defines operations that change warehouse document state.
/// </summary>
public interface IDocumentCommandService
{
    #region Draft Document Operations

    /// <summary>
    /// Creates a new document in draft status.
    /// </summary>
    /// <param name="type">Document type.</param>
    /// <param name="createdBy">User creating the document.</param>
    /// <param name="sourceWarehouseId">Source warehouse identifier.</param>
    /// <param name="items">Document items.</param>
    /// <param name="documentDate">Document date.</param>
    /// <param name="targetWarehouseId">Optional target warehouse identifier.</param>
    /// <param name="notes">Optional document notes.</param>
    /// <param name="ct">Operation cancellation token.</param>
    /// <returns>The created document.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="items"/> is <c>null</c>.</exception>
    /// <exception cref="ArgumentException">Thrown when the document has no items.</exception>
    /// <exception cref="OperationCanceledException">Thrown when the operation is canceled through <paramref name="ct"/>.</exception>
    Task<Document> CreateDocumentAsync(
         DocumentType type,
         UserSnapshot createdBy,
         Guid sourceWarehouseId,
         IEnumerable<DocumentItemDraft> items,
         DateTime documentDate,
         Guid? targetWarehouseId = null,
         string? notes = null,
        CancellationToken ct = default);

    /// <summary>
    /// Updates an existing draft document with its items.
    /// </summary>
    /// <param name="documentId">Identifier of the document to update.</param>
    /// <param name="updatedbyBy">User updating the document.</param>
    /// <param name="type">New document type.</param>
    /// <param name="sourceWarehouseId">Source warehouse identifier.</param>
    /// <param name="items">New document item list.</param>
    /// <param name="documentDate">Document date.</param>
    /// <param name="targetWarehouseId">Optional target warehouse identifier.</param>
    /// <param name="notes">Optional document notes.</param>
    /// <param name="ct">Operation cancellation token.</param>
    /// <returns>The updated document.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="items"/> is <c>null</c>.</exception>
    /// <exception cref="ArgumentException">Thrown when the document has no items.</exception>
    /// <exception cref="DocumentNotFoundException">Thrown when the document with the specified identifier does not exist.</exception>
    /// <exception cref="DocumentNotInDraftStateException">Thrown when the document is not in draft status and cannot be changed.</exception>
    /// <exception cref="OperationCanceledException">Thrown when the operation is canceled through <paramref name="ct"/>.</exception>
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

    #endregion

    #region Workflow Operations

    /// <summary>
    /// Confirms a document and performs the resulting stock operations.
    /// </summary>
    /// <param name="documentId">Identifier of the document to confirm.</param>
    /// <param name="confirmedBy">User confirming the document.</param>
    /// <param name="ct">Operation cancellation token.</param>
    /// <returns>A task representing the document confirmation operation.</returns>
    /// <exception cref="DocumentNotFoundException">Thrown when the document with the specified identifier does not exist.</exception>
    /// <exception cref="DocumentNotInDraftStateException">Thrown when the document is not in draft status.</exception>
    /// <exception cref="CannotConfirmEmptyDocumentException">Thrown when the document has no items.</exception>
    /// <exception cref="MissingSourceWarehouseForDocumentException">Thrown when the document requires a source warehouse but does not have one.</exception>
    /// <exception cref="MissingSourceZoneForDocumentException">Thrown when a document item requires a source zone but does not have one.</exception>
    /// <exception cref="MissingTargetWarehouseForMmDocumentException">Thrown when an MM document has no target warehouse.</exception>
    /// <exception cref="MissingTargetZoneForDocumentException">Thrown when an MM document item has no target zone.</exception>
    /// <exception cref="InsufficientStockException">Thrown when the document requires removing or moving more stock than is available.</exception>
    /// <exception cref="OperationCanceledException">Thrown when the operation is canceled through <paramref name="ct"/>.</exception>
    Task ConfirmDocumentAsync(Guid documentId, UserSnapshot confirmedBy, CancellationToken ct = default);

    /// <summary>
    /// Cancels a document and releases related active reservations when required by the document type.
    /// </summary>
    /// <param name="documentId">Identifier of the document to cancel.</param>
    /// <param name="canceledBy">User canceling the document.</param>
    /// <param name="ct">Operation cancellation token.</param>
    /// <returns>A task representing the document cancellation operation.</returns>
    /// <exception cref="DocumentNotFoundException">Thrown when the document with the specified identifier does not exist.</exception>
    /// <exception cref="DocumentAlreadyCancelledException">Thrown when the document has already been canceled.</exception>
    /// <exception cref="DocumentNotInDraftStateException">Thrown when the document is not in draft status and cannot be canceled.</exception>
    /// <exception cref="ReservationNotFoundException">Thrown when a related reservation does not exist during release.</exception>
    /// <exception cref="OperationCanceledException">Thrown when the operation is canceled through <paramref name="ct"/>.</exception>
    Task CancelDocumentAsync(Guid documentId, UserSnapshot canceledBy, CancellationToken ct = default);

    #endregion
}
