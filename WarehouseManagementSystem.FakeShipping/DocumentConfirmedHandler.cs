using Microsoft.EntityFrameworkCore;
using WarehouseManagementSystem.Contracts;

namespace WarehouseManagementSystem.FakeShipping;

public sealed class DocumentConfirmedHandler(ShippingDbContext shippingDbContext, ILogger<DocumentConfirmedHandler> logger)
{
    public const string ConsumerName = "FakeShipping.DocumentConfirmed";

    public async Task HandleAsync(DocumentConfirmedIntegrationEvent message, CancellationToken ct)
    {
        if (await shippingDbContext.ProcessedMessages.AnyAsync(x => x.Consumer == ConsumerName && x.MessageId == message.MessageId, ct))
        {
            logger.LogInformation("FakeShipping skipped duplicate MessageId {MessageId}, CorrelationId {CorrelationId}", message.MessageId, message.CorrelationId);
            return;
        }

        shippingDbContext.Shipments.Add(new FakeShipment
        {
            Id = Guid.NewGuid(),
            MessageId = message.MessageId,
            DocumentId = message.DocumentId,
            DocumentNumber = message.DocumentNumber,
            CorrelationId = message.CorrelationId,
            CreatedAt = DateTimeOffset.UtcNow
        });
        shippingDbContext.ProcessedMessages.Add(new ProcessedMessage
        {
            Id = Guid.NewGuid(),
            Consumer = ConsumerName,
            MessageId = message.MessageId,
            MessageType = nameof(DocumentConfirmedIntegrationEvent),
            CorrelationId = message.CorrelationId,
            ProcessedAt = DateTimeOffset.UtcNow
        });

        try
        {
            await shippingDbContext.SaveChangesAsync(ct);
            logger.LogInformation("FakeShipping created shipment for MessageId {MessageId}, CorrelationId {CorrelationId}", message.MessageId, message.CorrelationId);
        }
        catch (DbUpdateException ex) when (ProcessedMessageConflict.IsDuplicate(ex))
        {
            // Another consumer instance committed the same MessageId after our initial read.
            // The database constraint is the final idempotency guard; treating this as handled is safe.
            logger.LogInformation("FakeShipping skipped concurrently processed duplicate MessageId {MessageId}", message.MessageId);
        }
    }
}

public static class ProcessedMessageConflict
{
    public static bool IsDuplicate(DbUpdateException exception)
    {
        return 
            exception.InnerException?
                .Message.Contains("IX_ProcessedMessages_Consumer_MessageId", StringComparison.OrdinalIgnoreCase) == true 
            ||
            exception.InnerException?
                .Message.Contains("ProcessedMessages.Consumer, ProcessedMessages.MessageId", StringComparison.OrdinalIgnoreCase) == true;
    }
}
