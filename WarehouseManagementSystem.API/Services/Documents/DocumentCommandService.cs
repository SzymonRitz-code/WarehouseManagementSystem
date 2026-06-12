using Microsoft.CodeAnalysis;
using WarehouseManagementSystem.API.Services.AuditLogs;
using WarehouseManagementSystem.Domain.Enums;
using WarehouseManagementSystem.Domain.Exceptions;
using WarehouseManagementSystem.Domain.Interfaces;
using WarehouseManagementSystem.Domain.Model.DocumentsDomain;
using WarehouseManagementSystem.Domain.Services;
using WarehouseManagementSystem.Domain.ValueObjects;
using WarehouseManagementSystem.Infrastructure.Services;
using Document = WarehouseManagementSystem.Domain.Model.DocumentsDomain.Document;

namespace WarehouseManagementSystem.API.Services.Documents;

public class DocumentCommandService : IDocumentCommandService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IStockService _stockService;
    private readonly IDocumentNumberGenerator _numberGenerator;
    private readonly ISystemClock _clock;
    private readonly ILogger<DocumentCommandService> _logger;
    private readonly IAuditLogService _auditLogService;

    public DocumentCommandService(
        IUnitOfWork unitOfWork,
        IStockService stockService,
        IDocumentNumberGenerator numberGenerator,
        ISystemClock systemClock,
        ILogger<DocumentCommandService> logger,
        IAuditLogService auditLogService)
    {
        _logger = logger;
        _auditLogService = auditLogService;
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _stockService = stockService ?? throw new ArgumentNullException(nameof(stockService));
        _numberGenerator = numberGenerator ?? throw new ArgumentNullException(nameof(numberGenerator));
        this._clock = systemClock;
    }

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
        if (items == null || !items.Any())
            throw new ArgumentException("Document must have at least one item.", nameof(items));

        _logger.LogInformation("Creating document by {UserId}", createdBy.Id);

        var document = new Document(
            documentDate: documentDate,
            type: type,
            createdByUser: createdBy,
            sourceWarehouseId: sourceWarehouseId,
            targetWarehouseId: targetWarehouseId,
            notes: notes
        );

        foreach (var draft in items)
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
        if (items == null || !items.Any())
            throw new ArgumentException("Document must have at least one item.", nameof(items));

        var document = await _unitOfWork.Documents.GetDocumentWithItems(documentId) ?? throw new InvalidOperationException("Document not found.");
        var oldDocument = AuditSnapshots.Document(document);
        _logger.LogInformation("Updating document {DocumentId} by {UserId}", documentId, updatedBy.Id);

        document.ChangeDate(documentDate);
        document.SetDocumentType(type);
        document.SetSourceWarehouse(sourceWarehouseId);
        document.SetTargetWarehouse(targetWarehouseId);
        document.SetNotes(notes);

        var itemsToReplace = items.Select(draft => new DocumentItem(
                productId: draft.ProductId,
                quantity: draft.Quantity,
                productBatchId: draft.ProductBatchId,
                sourceZoneId: draft.SourceZoneId,
                targetZoneId: draft.TargetZoneId
            )).ToList();

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
    [Obsolete("MVP flow. Not used in current MM document-driven process. Reserved for future workflow-based transfer execution.")]
    // TODO: Future phase - workflow-based transfer execution (MM v2)
    public async Task StartTransferAsync(Guid documentId, Guid userId)
    {
        var document = await _unitOfWork.Documents.FindAsync(documentId)
                       ?? throw new InvalidOperationException("Document not found.");
        var oldDocument = AuditSnapshots.Document(document);
        document.StartTransfer(userId, _clock.UtcNow);
        await _auditLogService.LogChangesAsync(
            entityName: nameof(Document),
            entityId: documentId,
            operation: "StartTransfer",
            performedById: userId,
            oldSnapshot: oldDocument,
            newSnapshot: AuditSnapshots.Document(document));

        await _unitOfWork.SaveChangesAsync();
    }

    public async Task ConfirmDocumentAsync(Guid documentId, UserSnapshot confirmedBy, CancellationToken ct = default)
    {
        _logger.LogInformation("Confirming document {DocumentId} by {UserId}", documentId, confirmedBy.Id);

        var document = await _unitOfWork.Documents.GetDocumentWithItems(documentId)
                       ?? throw new InvalidOperationException("Document not found.");
        var oldDocument = AuditSnapshots.Document(document);
        switch (document.Type)
        {
            case DocumentType.PZ: // Przyjęcie towaru
                foreach (var item in document.Items)
                {
                    await _stockService.IncreaseStockAsync(
                        productId: item.ProductId,
                        warehouseId: document.SourceWarehouseId ?? throw new MissingSourceWarehouseForDocumentException(document.Id),
                        warehouseZoneId: item.SourceZoneId ?? throw new MissingSourceZoneForDocumentException(document.Id),
                        quantity: item.Quantity,
                        batchId: item.ProductBatchId
                    );
                }
                break;

            case DocumentType.WZ: // Wydanie towaru
                foreach (var item in document.Items)
                {
                    await _stockService.DecreaseStockAsync(
                        productId: item.ProductId,
                        warehouseId: document.SourceWarehouseId ?? throw new MissingSourceWarehouseForDocumentException(document.Id),
                        warehouseZoneId: item.SourceZoneId ?? throw new MissingSourceZoneForDocumentException(document.Id),
                        quantity: item.Quantity,
                        batchId: item.ProductBatchId
                    );
                }
                break;

            case DocumentType.MM: // Przesunięcie magazynowe
                if (document.TargetWarehouseId == null)
                    throw new MissingTargetWarehouseForMmDocumentException(document.Id);

                foreach (var item in document.Items)
                {
                    await _stockService.MoveStockAsync(
                        productId: item.ProductId,
                        sourceWarehouseId: document.SourceWarehouseId ?? throw new MissingSourceWarehouseForDocumentException(document.Id),
                        sourceZoneId: item.SourceZoneId ?? throw new MissingSourceZoneForDocumentException(document.Id),
                        targetWarehouseId: document.TargetWarehouseId.Value,
                        targetZoneId: item.TargetZoneId ?? throw new MissingTargetZoneForDocumentException(document.Id),
                        quantity: item.Quantity,
                        batchId: item.ProductBatchId
                    );
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
        await _unitOfWork.SaveChangesAsync();
        _logger.LogInformation("Document {DocumentId} confirmed by {UserId}", documentId, confirmedBy.Id);
    }

    public async Task CancelDocumentAsync(Guid documentId, UserSnapshot canceledBy, CancellationToken ct = default)
    {
        var document = await _unitOfWork.Documents.FindAsync(documentId)
                       ?? throw new InvalidOperationException("Document not found.");
        _logger.LogInformation("Canceling document {DocumentId} by {UserId}", documentId, canceledBy.Id);
        var oldDocument = AuditSnapshots.Document(document);
        switch (document.Type)
        {
            case DocumentType.WZ: // Wydanie towaru – mogły być rezerwacje
                var reservations = await _unitOfWork.Stocks.GetActiveReservationsByDocumentIdAsync(document.Id);

                foreach (var reservation in reservations)
                {
                    // Używamy serwisu do zwolnienia rezerwacji
                    await _stockService.ReleaseReservationAsync(reservation.StockId, reservation.Id);
                }
                break;

            case DocumentType.MM: // Przesunięcie magazynowe – jeśli rezerwacje były
                var mmReservations = await _unitOfWork.Stocks.GetActiveReservationsByDocumentIdAsync(document.Id);

                foreach (var reservation in mmReservations)
                {
                    await _stockService.ReleaseReservationAsync(reservation.StockId, reservation.Id);
                }
                break;

                // PZ – przyjęcia zwykle nie mają rezerwacji, więc nic do zwolnienia
        }

        // Zmiana statusu dokumentu na anulowany
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
        await _unitOfWork.SaveChangesAsync();
        _logger.LogInformation("Document {DocumentId} canceled by {UserId}", documentId, canceledBy.Id);
    }


}
