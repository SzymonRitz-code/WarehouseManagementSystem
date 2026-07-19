using Microsoft.EntityFrameworkCore;
using WarehouseManagementSystem.API.Integration.Contracts;

namespace WarehouseManagementSystem.FakeShipping;

public sealed class DocumentConfirmedHandler(ShippingDbContext db, ILogger<DocumentConfirmedHandler> logger)
{
    public const string ConsumerName = "FakeShipping.DocumentConfirmed";

    public async Task HandleAsync(DocumentConfirmedIntegrationEvent message, CancellationToken ct)
    {
        if (await db.ProcessedMessages.AnyAsync(x => x.Consumer == ConsumerName && x.MessageId == message.MessageId, ct))
        {
            logger.LogInformation("FakeShipping skipped duplicate MessageId {MessageId}, CorrelationId {CorrelationId}", message.MessageId, message.CorrelationId);
            return;
        }

        db.Shipments.Add(new FakeShipment
        {
            Id = Guid.NewGuid(), MessageId = message.MessageId, DocumentId = message.DocumentId,
            DocumentNumber = message.DocumentNumber, CorrelationId = message.CorrelationId,
            CreatedAt = DateTimeOffset.UtcNow
        });
        db.ProcessedMessages.Add(new ProcessedMessage
        {
            Id = Guid.NewGuid(), Consumer = ConsumerName, MessageId = message.MessageId,
            MessageType = nameof(DocumentConfirmedIntegrationEvent), CorrelationId = message.CorrelationId,
            ProcessedAt = DateTimeOffset.UtcNow
        });

        await db.SaveChangesAsync(ct);
        logger.LogInformation("FakeShipping created shipment for MessageId {MessageId}, CorrelationId {CorrelationId}", message.MessageId, message.CorrelationId);
    }
}
