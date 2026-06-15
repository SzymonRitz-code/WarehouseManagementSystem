using Microsoft.AspNetCore.Http.HttpResults;
using WarehouseManagementSystem.Domain.Enums;
using WarehouseManagementSystem.Domain.Model.DocumentsDomain;
using WarehouseManagementSystem.Domain.Model.InventoryDomain;
using WarehouseManagementSystem.Domain.ValueObjects;

namespace WarehouseManagementSystem.API.Services.Documents;

/// <summary>
/// Service responsible for handling document commands such as creating, updating, confirming and canceling documents.
/// </summary>
public interface IDocumentCommandService
{
    /// <summary>
    /// Creates a new document in Draft status. 
    /// The document can be later updated, confirmed or cancelled.
    /// </summary>
    /// <param name="type">Document type</param>
    /// <param name="createdBy">User who created the document</param>
    /// <param name="sourceWarehouseId">Source warehouse ID</param>
    /// <param name="items">Document items</param>
    /// <param name="documentDate">Document date</param>
    /// <param name="targetWarehouseId">Target warehouse ID</param>
    /// <param name="notes">Document notes</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Returns the created document</returns>
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
    /// Updates an existing document. Only documents in Draft status can be updated.
    /// </summary>
    /// <param name="documentId">document ID</param>
    /// <param name="updatedbyBy">user who updated the document</param>
    /// <param name="type">document type</param>
    /// <param name="sourceWarehouseId">source warehouse ID</param>
    /// <param name="items">document items</param>
    /// <param name="documentDate">document date</param>
    /// <param name="targetWarehouseId">target warehouse ID</param>
    /// <param name="notes">document notes</param>
    /// <param name="ct">cancellation token</param>
    /// <returns>Returns updated document</returns>
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
    /// Confirms the document. Only documents in Draft status can be confirmed. 
    /// Confirmation changes the document status to Confirmed and triggers inventory updates.
    /// </summary>
    /// <param name="documentId">Document ID</param>
    /// <param name="confirmedBy">User who confirmed the document</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns></returns>
    Task ConfirmDocumentAsync(Guid documentId, UserSnapshot confirmedBy, CancellationToken ct = default);

    /// <summary>
    /// Cancels the document. Only documents in Draft status can be canceled. 
    /// Cancellation changes the document status to Canceled and prevents any further operations on the document.
    /// </summary>
    /// <param name="documentId">Document ID</param>
    /// <param name="canceledBy">User who canceled the document</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns></returns>
    Task CancelDocumentAsync(Guid documentId, UserSnapshot canceledBy, CancellationToken ct = default);

}
