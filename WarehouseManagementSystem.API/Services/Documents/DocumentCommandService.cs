using WarehouseManagementSystem.Domain.Enums;
using WarehouseManagementSystem.Domain.Interfaces;
using WarehouseManagementSystem.Domain.Model.DocumentsDomain;
using WarehouseManagementSystem.Domain.Services;
using WarehouseManagementSystem.Domain.ValueObjects;

namespace WarehouseManagementSystem.API.Services.Documents;

public class DocumentCommandService : IDocumentCommandService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IStockService _stockService;
    private readonly IStockReservationService _reservationService;
    private readonly IDocumentNumberGenerator _numberGenerator;

    public DocumentCommandService(
        IUnitOfWork unitOfWork,
        IStockService stockService,
        IStockReservationService reservationService,
        IDocumentNumberGenerator numberGenerator)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _stockService = stockService ?? throw new ArgumentNullException(nameof(stockService));
        _reservationService = reservationService ?? throw new ArgumentNullException(nameof(reservationService));
        _numberGenerator = numberGenerator ?? throw new ArgumentNullException(nameof(numberGenerator));
    }

    public async Task<Document> CreateDocumentAsync(
        DocumentType type,
        Guid createdById,
        Guid sourceWarehouseId,
        IEnumerable<DocumentItemDraft> items,
        DateTime documentDate,
        Guid? targetWarehouseId = null,
        string? notes = null,
        CancellationToken ct = default)
    {
        if (items == null || !items.Any())
            throw new ArgumentException("Document must have at least one item.", nameof(items));

        var documentNumber = await _numberGenerator.GenerateAsync(
            type,
            sourceWarehouseId,
            documentDate);

        var document = new Document(
            number: documentNumber,
            documentDate: documentDate,
            type: type,
            createdById: createdById,
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
        await _unitOfWork.SaveChangesAsync(ct);

        return document;
    }

    public async Task ConfirmDocumentAsync(Guid documentId, Guid confirmedById)
    {
        var document = await _unitOfWork.Documents.FindAsync(documentId)
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
        document.Confirm(confirmedById);

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
                var reservations = await _unitOfWork.StockReservations
                                        .GetActiveReservationsByDocumentIdAsync(document.Id);

                foreach (var reservation in reservations)
                {
                    // Używamy serwisu do zwolnienia rezerwacji
                    await _reservationService.ReleaseReservationAsync(reservation.Id);
                }
                break;

            case DocumentType.MM: // Przesunięcie magazynowe – jeśli rezerwacje były
                var mmReservations = await _unitOfWork.StockReservations
                                        .GetActiveReservationsByDocumentIdAsync(document.Id);

                foreach (var reservation in mmReservations)
                {
                    await _reservationService.ReleaseReservationAsync(reservation.Id);
                }
                break;

                // PZ – przyjęcia zwykle nie mają rezerwacji, więc nic do zwolnienia
        }

        // Zmiana statusu dokumentu na anulowany
        document.Cancel(); // zakładam, że masz metodę domenową Cancel()

        _unitOfWork.Documents.Update(document);
        await _unitOfWork.SaveChangesAsync();
    }
}
