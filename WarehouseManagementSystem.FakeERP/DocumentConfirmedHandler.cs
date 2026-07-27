using Microsoft.EntityFrameworkCore;
using WarehouseManagementSystem.Contracts;

namespace WarehouseManagementSystem.FakeERP;

public sealed class DocumentConfirmedHandler(ErpDbContext db, ILogger<DocumentConfirmedHandler> logger)
{
    public const string ConsumerName = "FakeERP.DocumentConfirmed";
    public async Task HandleAsync(DocumentConfirmedIntegrationEvent message, CancellationToken ct)
    {
        if (await db.ProcessedMessages.AnyAsync(x => x.Consumer == ConsumerName && x.MessageId == message.MessageId, ct)) return;
        await using var tx = db.Database.IsRelational() ? await db.Database.BeginTransactionAsync(ct) : null;
        var order = await db.WarehouseOrders.SingleOrDefaultAsync(x => x.CorrelationId == message.CorrelationId, ct)
            ?? throw new InvalidOperationException($"No ERP order matches CorrelationId {message.CorrelationId}.");
        order.Status = "Confirmed"; order.WmsDocumentId = message.DocumentId; order.ConfirmedAt = message.ConfirmedAt;
        db.ProcessedMessages.Add(new ErpProcessedMessage { Id = Guid.NewGuid(), Consumer = ConsumerName, MessageId = message.MessageId, CorrelationId = message.CorrelationId, ProcessedAt = DateTimeOffset.UtcNow });
        try { await db.SaveChangesAsync(ct); if (tx is not null) await tx.CommitAsync(ct); logger.LogInformation("ERP confirmed order {ExternalOrderId}, CorrelationId {CorrelationId}", order.ExternalOrderId, message.CorrelationId); }
        catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains("Consumer", StringComparison.OrdinalIgnoreCase) == true) { logger.LogInformation("ERP skipped concurrent duplicate {MessageId}", message.MessageId); }
    }
}
