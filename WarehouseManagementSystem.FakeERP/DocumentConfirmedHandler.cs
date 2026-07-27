using Microsoft.EntityFrameworkCore;
using WarehouseManagementSystem.Contracts;

namespace WarehouseManagementSystem.FakeERP;

public sealed class DocumentConfirmedHandler(ErpDbContext erpDbContext, ILogger<DocumentConfirmedHandler> logger)
{
    public const string ConsumerName = "FakeERP.DocumentConfirmed";
    public async Task HandleAsync(DocumentConfirmedIntegrationEvent message, CancellationToken ct)
    {
        if (await erpDbContext.ProcessedMessages.AnyAsync(x => x.Consumer == ConsumerName && x.MessageId == message.MessageId, ct))
        {
            return;
        }

        await using var confirmationTransaction = erpDbContext.Database.IsRelational()
            ? await erpDbContext.Database.BeginTransactionAsync(ct)
            : null;

        var warehouseOrder = await erpDbContext.WarehouseOrders.SingleOrDefaultAsync(x => x.CorrelationId == message.CorrelationId, ct)
            ?? throw new InvalidOperationException($"No ERP order matches CorrelationId {message.CorrelationId}.");

        warehouseOrder.Status = "Confirmed";
        warehouseOrder.WmsDocumentId = message.DocumentId;
        warehouseOrder.ConfirmedAt = message.ConfirmedAt;

        erpDbContext.ProcessedMessages.Add(new ErpProcessedMessage
        {
            Id = Guid.NewGuid(),
            Consumer = ConsumerName,
            MessageId = message.MessageId,
            CorrelationId = message.CorrelationId,
            ProcessedAt = DateTimeOffset.UtcNow
        });
        try
        {
            await erpDbContext.SaveChangesAsync(ct); if (confirmationTransaction is not null) { await confirmationTransaction.CommitAsync(ct); }
            logger.LogInformation("ERP confirmed order {ExternalOrderId}, CorrelationId {CorrelationId}", warehouseOrder.ExternalOrderId, message.CorrelationId);
        }
        catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains("Consumer", StringComparison.OrdinalIgnoreCase) == true)
        {
            logger.LogInformation("ERP skipped concurrent duplicate {MessageId}", message.MessageId);
        }
    }
}
