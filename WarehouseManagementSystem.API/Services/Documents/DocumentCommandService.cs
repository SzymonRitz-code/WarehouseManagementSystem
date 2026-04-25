using WarehouseManagementSystem.Domain.Enums;
using WarehouseManagementSystem.Domain.Interfaces;
using WarehouseManagementSystem.Domain.Model.DocumentsDomain;
using WarehouseManagementSystem.Domain.Services;
using WarehouseManagementSystem.Domain.ValueObjects;
using WarehouseManagementSystem.Infrastructure.Services;

namespace WarehouseManagementSystem.API.Services.Documents;

public class DocumentCommandService : IDocumentCommandService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IStockService _stockService;
    private readonly IDocumentNumberGenerator _numberGenerator;
    private readonly ISystemClock _clock;

    public DocumentCommandService(
        IUnitOfWork unitOfWork,
        IStockService stockService,
        IDocumentNumberGenerator numberGenerator,
        ISystemClock systemClock)
    {
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
        try
        {
            _unitOfWork.Documents.Add(document);
            await _unitOfWork.SaveChangesAsync(ct);
        }
        catch (Exception ex)
        {

        }


        return document;
    }
    public async Task<Document> UpdateDocumentAsync(
        Guid documentId,
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

        var document = await _unitOfWork.Documents.FindAsync(documentId) ?? throw new InvalidOperationException("Document not found.");

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
        await _unitOfWork.SaveChangesAsync(ct);

        return document;
    }
    [Obsolete("MVP flow. Not used in current MM document-driven process. Reserved for future workflow-based transfer execution.")]
    // TODO: Future phase - workflow-based transfer execution (MM v2)
    public async Task StartTransferAsync(Guid documentId, Guid userId)
    {
        var document = await _unitOfWork.Documents.FindAsync(documentId)
                       ?? throw new InvalidOperationException("Document not found.");
        document.StartTransfer(userId, _clock.UtcNow);

        await _unitOfWork.SaveChangesAsync();
    }
    public async Task ConfirmDocumentAsync(Guid documentId, Domain.ValueObjects.UserSnapshot confirmedBy)
    {
        var document = await _unitOfWork.Documents.GetDocumentWithItems(documentId)
                       ?? throw new InvalidOperationException("Document not found.");

        switch (document.Type)
        {
            case DocumentType.PZ: // Przyjęcie towaru
                foreach (var item in document.Items)
                {
                    await _stockService.IncreaseStockAsync(
                        productId: item.ProductId,
                        warehouseId: document.SourceWarehouseId ?? throw new InvalidOperationException("Source warehouse is required."),
                        warehouseZoneId: item.SourceZoneId ?? throw new InvalidOperationException("Source zone is required."),
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
                        warehouseId: document.SourceWarehouseId ?? throw new InvalidOperationException("Source warehouse is required."),
                        warehouseZoneId: item.SourceZoneId ?? throw new InvalidOperationException("Source zone is required."),
                        quantity: item.Quantity,
                        batchId: item.ProductBatchId
                    );
                }
                break;

            case DocumentType.MM: // Przesunięcie magazynowe
                if (document.TargetWarehouseId == null)
                    throw new InvalidOperationException("Target warehouse is required for MM.");

                foreach (var item in document.Items)
                {
                    await _stockService.MoveStockAsync(
                        productId: item.ProductId,
                        sourceWarehouseId: document.SourceWarehouseId ?? throw new InvalidOperationException("Source warehouse is required."),
                        sourceZoneId: item.SourceZoneId ?? throw new InvalidOperationException("Source zone is required."),
                        targetWarehouseId: document.TargetWarehouseId.Value,
                        targetZoneId: item.TargetZoneId ?? throw new InvalidOperationException("Target zone is required."),
                        quantity: item.Quantity,
                        batchId: item.ProductBatchId
                    );
                }
                break;
        }
        var documentNumber = await _numberGenerator.GenerateAsync(
            document.Type,
            document.SourceWarehouseId,
            document.DocumentDate); //TODO dodać testy sprawdzające czy numer jest poprawnie wygenerowany po zatwierdzeniu dokumentu
        document.SetNumber(documentNumber);
        document.Confirm(confirmedBy);

        _unitOfWork.Documents.Update(document);
        await _unitOfWork.SaveChangesAsync();
    }

    public async Task CancelDocumentAsync(Guid documentId)
    {
        var document = await _unitOfWork.Documents.FindAsync(documentId)
                       ?? throw new InvalidOperationException("Document not found.");

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
        document.Cancel();

        _unitOfWork.Documents.Update(document);
        await _unitOfWork.SaveChangesAsync();
    }


}
