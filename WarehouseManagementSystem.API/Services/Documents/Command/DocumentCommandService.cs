using System.Data;
using WarehouseManagementSystem.API.Services.AuditLogs.Command;
using WarehouseManagementSystem.API.Services.AuditLogs;
using WarehouseManagementSystem.Domain.Enums;
using WarehouseManagementSystem.Domain.Exceptions;
using WarehouseManagementSystem.Domain.Interfaces;
using WarehouseManagementSystem.Domain.Model.DocumentsDomain;
using WarehouseManagementSystem.Domain.Services;
using WarehouseManagementSystem.Domain.ValueObjects;
using WarehouseManagementSystem.Infrastructure.Services;
using WarehouseManagementSystem.API.Services.Stocks.Command;
using Document = WarehouseManagementSystem.Domain.Model.DocumentsDomain.Document;

namespace WarehouseManagementSystem.API.Services.Documents.Command;

public class DocumentCommandService : IDocumentCommandService
{
    #region Fields and Constructor

    private readonly IUnitOfWork _unitOfWork;
    private readonly IStockCommandService _stockService;
    private readonly IDocumentNumberGenerator _numberGenerator;
    private readonly ISystemClock _clock;
    private readonly ILogger<DocumentCommandService> _logger;
    private readonly IAuditLogCommandService _auditLogService;

    public DocumentCommandService(
        IUnitOfWork unitOfWork,
        IStockCommandService stockService,
        IDocumentNumberGenerator numberGenerator,
        ISystemClock systemClock,
        ILogger<DocumentCommandService> logger,
        IAuditLogCommandService auditLogService)
    {
        _logger = logger;
        _auditLogService = auditLogService;
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _stockService = stockService ?? throw new ArgumentNullException(nameof(stockService));
        _numberGenerator = numberGenerator ?? throw new ArgumentNullException(nameof(numberGenerator));
        _clock = systemClock ?? throw new ArgumentNullException(nameof(systemClock));
    }

    #endregion

    #region Create and Update Operations

    public async Task<Document> CreateDocumentAsync(
        DocumentType type,
        UserSnapshot createdBy,
        Guid sourceWarehouseId,
        IEnumerable<DocumentItemDraft> items,
        DateTime documentDate,
        Guid? targetWarehouseId = null,
        string? notes = null,
        CancellationToken ct = default)
    {
        var drafts = NormalizeItems(items);
        if (drafts.Count == 0)
        {
            throw new ArgumentException("Document must have at least one item.", nameof(items));
        }

        _logger.LogInformation("Creating document by {UserId}", createdBy.Id);

        var document = new Document(
            documentDate: documentDate,
            type: type,
            createdByUser: createdBy,
            sourceWarehouseId: sourceWarehouseId,
            targetWarehouseId: targetWarehouseId,
            notes: notes
        );

        foreach (var draft in drafts)
        {
            var item = new DocumentItem(
                productId: draft.ProductId,
                quantity: draft.Quantity,
                productBatchId: draft.ProductBatchId,
                sourceZoneId: draft.SourceZoneId,
                targetZoneId: draft.TargetZoneId
            );

            document.AddItem(item);
        }

        _unitOfWork.Documents.Add(document);
        await _auditLogService.LogChangesAsync(
            entityName: nameof(Document),
            entityId: document.Id,
            operation: "Create",
            performedById: createdBy.Id,
            oldSnapshot: null,
            newSnapshot: AuditSnapshots.Document(document),
            ct: ct);
        await _unitOfWork.SaveChangesAsync(ct);

        _logger.LogInformation("Document {DocumentId} created by {UserId}", document.Id, createdBy.Id);

        return document;
    }

    public async Task<Document> UpdateDocumentAsync(
        Guid documentId,
        UserSnapshot updatedBy,
        DocumentType type,
        Guid sourceWarehouseId,
        List<DocumentItemDraft> items,
        DateTime documentDate,
        Guid? targetWarehouseId = null,
        string? notes = null,
        CancellationToken ct = default)
    {
        var drafts = NormalizeItems(items);
        if (drafts.Count == 0)
        {
            throw new ArgumentException("Document must have at least one item.", nameof(items));
        }

        var document = await GetDocumentWithItemsOrThrowAsync(documentId, ct);
        var oldDocument = AuditSnapshots.Document(document);
        _logger.LogInformation("Updating document {DocumentId} by {UserId}", documentId, updatedBy.Id);

        document.ChangeDate(documentDate);
        document.SetDocumentType(type);
        document.SetSourceWarehouse(sourceWarehouseId);
        document.SetTargetWarehouse(targetWarehouseId);
        document.SetNotes(notes);

        var itemsToReplace = drafts.Select(draft => new DocumentItem(
            productId: draft.ProductId,
            quantity: draft.Quantity,
            productBatchId: draft.ProductBatchId,
            sourceZoneId: draft.SourceZoneId,
            targetZoneId: draft.TargetZoneId)).ToList();

        document.ReplaceItems(itemsToReplace);

        _unitOfWork.Documents.Update(document);
        await _auditLogService.LogChangesAsync(
            entityName: nameof(Document),
            entityId: documentId,
            operation: "Update",
            performedById: updatedBy.Id,
            oldSnapshot: oldDocument,
            newSnapshot: AuditSnapshots.Document(document),
            ct: ct);
        await _unitOfWork.SaveChangesAsync(ct);

        _logger.LogInformation("Document {DocumentId} updated by {UserId}", documentId, updatedBy.Id);
        return document;
    }

    #endregion

    #region Confirm Operation

    public async Task ConfirmDocumentAsync(Guid documentId, UserSnapshot confirmedBy, CancellationToken ct = default)
    {
        _logger.LogInformation("Confirming document {DocumentId} by {UserId}", documentId, confirmedBy.Id);

        var document = await GetDocumentWithItemsOrThrowAsync(documentId, ct);
        var oldDocument = AuditSnapshots.Document(document);

        // The confirmation transaction starts here because this is the first point where the command
        // changes business state. Stock mutations, document number allocation, document confirmation
        // and audit logging must commit as one unit. If these operations were saved independently,
        // a failure after stock movement but before document confirmation could leave inventory changed
        // while the document still looked pending, and two concurrent confirmations could observe the
        // same document-number sequence before either write was committed.
        await using var transaction = await _unitOfWork.BeginTransactionAsync(IsolationLevel.Serializable, ct);

        switch (document.Type)
        {
            case DocumentType.PZ:
                foreach (var item in document.Items)
                {
                    await _stockService.IncreaseStockAsync(
                        productId: item.ProductId,
                        warehouseId: document.SourceWarehouseId ?? throw new MissingSourceWarehouseForDocumentException(document.Id),
                        warehouseZoneId: item.SourceZoneId ?? throw new MissingSourceZoneForDocumentException(document.Id),
                        quantity: item.Quantity,
                        batchId: item.ProductBatchId,
                        ct: ct);
                }

                break;

            case DocumentType.WZ:
                foreach (var item in document.Items)
                {
                    await _stockService.DecreaseStockAsync(
                        productId: item.ProductId,
                        warehouseId: document.SourceWarehouseId ?? throw new MissingSourceWarehouseForDocumentException(document.Id),
                        warehouseZoneId: item.SourceZoneId ?? throw new MissingSourceZoneForDocumentException(document.Id),
                        quantity: item.Quantity,
                        batchId: item.ProductBatchId,
                        ct: ct);
                }

                break;

            case DocumentType.MM:
                if (document.TargetWarehouseId == null)
                {
                    throw new MissingTargetWarehouseForMmDocumentException(document.Id);
                }

                foreach (var item in document.Items)
                {
                    await _stockService.MoveStockAsync(
                        productId: item.ProductId,
                        sourceWarehouseId: document.SourceWarehouseId ?? throw new MissingSourceWarehouseForDocumentException(document.Id),
                        sourceZoneId: item.SourceZoneId ?? throw new MissingSourceZoneForDocumentException(document.Id),
                        targetWarehouseId: document.TargetWarehouseId.Value,
                        targetZoneId: item.TargetZoneId ?? throw new MissingTargetZoneForDocumentException(document.Id),
                        quantity: item.Quantity,
                        batchId: item.ProductBatchId,
                        ct: ct);
                }

                break;
        }

        var documentNumber = await _numberGenerator.GenerateAsync(
            document.Type,
            document.SourceWarehouseId,
            document.DocumentDate);

        document.SetNumber(documentNumber);
        document.Confirm(confirmedBy);

        _unitOfWork.Documents.Update(document);
        await _auditLogService.LogChangesAsync(
            entityName: nameof(Document),
            entityId: documentId,
            operation: "Confirm",
            performedById: confirmedBy.Id,
            oldSnapshot: oldDocument,
            newSnapshot: AuditSnapshots.Document(document),
            ct: ct);
        await _unitOfWork.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        _logger.LogInformation("Document {DocumentId} confirmed by {UserId}", documentId, confirmedBy.Id);
    }

    #endregion

    #region Cancel Operation

    public async Task CancelDocumentAsync(Guid documentId, UserSnapshot canceledBy, CancellationToken ct = default)
    {
        var document = await GetDocumentOrThrowAsync(documentId, ct);
        _logger.LogInformation("Canceling document {DocumentId} by {UserId}", documentId, canceledBy.Id);
        var oldDocument = AuditSnapshots.Document(document);

        await using var transaction = await _unitOfWork.BeginTransactionAsync(IsolationLevel.Serializable, ct);

        switch (document.Type)
        {
            case DocumentType.WZ:
                var reservations = await _unitOfWork.Stocks.GetActiveReservationsByDocumentIdAsync(document.Id);

                foreach (var reservation in reservations)
                {
                    await _stockService.ReleaseReservationAsync(reservation.StockId, reservation.Id, ct);
                }

                break;

            case DocumentType.MM:
                var mmReservations = await _unitOfWork.Stocks.GetActiveReservationsByDocumentIdAsync(document.Id);

                foreach (var reservation in mmReservations)
                {
                    await _stockService.ReleaseReservationAsync(reservation.StockId, reservation.Id, ct);
                }

                break;
        }

        document.Cancel(canceledBy);

        _unitOfWork.Documents.Update(document);
        await _auditLogService.LogChangesAsync(
            entityName: nameof(Document),
            entityId: documentId,
            operation: "Cancel",
            performedById: canceledBy.Id,
            oldSnapshot: oldDocument,
            newSnapshot: AuditSnapshots.Document(document),
            ct: ct);
        await _unitOfWork.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        _logger.LogInformation("Document {DocumentId} canceled by {UserId}", documentId, canceledBy.Id);
    }

    #endregion

    #region Helper Methods

    private async Task<Document> GetDocumentWithItemsOrThrowAsync(Guid documentId, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        return await _unitOfWork.Documents.GetDocumentWithItems(documentId)
               ?? throw new DocumentNotFoundException(documentId);
    }

    private async Task<Document> GetDocumentOrThrowAsync(Guid documentId, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        return await _unitOfWork.Documents.FindAsync(documentId)
               ?? throw new DocumentNotFoundException(documentId);
    }

    private static List<DocumentItemDraft> NormalizeItems(IEnumerable<DocumentItemDraft> items)
    {
        return items?.ToList() ?? throw new ArgumentNullException(nameof(items));
    }

    #endregion
}
