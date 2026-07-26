using Microsoft.EntityFrameworkCore;
using WarehouseManagementSystem.Contracts;

namespace WarehouseManagementSystem.FakeBilling;

public sealed class DocumentConfirmedBillingHandler(BillingDbContext db, ILogger<DocumentConfirmedBillingHandler> logger)
{
    public const string ConsumerName = "FakeBilling.DocumentConfirmed";

    public async Task<BillingDecision> HandleAsync(DocumentConfirmedIntegrationEvent message, CancellationToken ct)
    {
        if (await db.ProcessedMessages.AnyAsync(x => x.Consumer == ConsumerName && x.MessageId == message.MessageId, ct))
        {
            logger.LogInformation("FakeBilling decision duplicate. MessageId {MessageId}, CorrelationId {CorrelationId}, SourceDocumentId {SourceDocumentId}", message.MessageId, message.CorrelationId, message.DocumentId);
            return BillingDecision.Duplicate;
        }

        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        if (message.DocumentType != "WZ")
        {
            db.ProcessedMessages.Add(Processed(message));
            await db.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
            logger.LogInformation("FakeBilling decision ignored. MessageId {MessageId}, CorrelationId {CorrelationId}, SourceDocumentId {SourceDocumentId}", message.MessageId, message.CorrelationId, message.DocumentId);
            return BillingDecision.Ignored;
        }

        if (await db.FakeInvoices.AnyAsync(x => x.SourceDocumentId == message.DocumentId, ct))
        {
            db.ProcessedMessages.Add(Processed(message));
            await db.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
            logger.LogInformation("FakeBilling decision duplicate. MessageId {MessageId}, CorrelationId {CorrelationId}, SourceDocumentId {SourceDocumentId}", message.MessageId, message.CorrelationId, message.DocumentId);
            return BillingDecision.Duplicate;
        }

        db.FakeInvoices.Add(new FakeInvoice
        {
            Id = Guid.NewGuid(), SourceDocumentId = message.DocumentId, SourceDocumentNumber = message.DocumentNumber,
            SourceMessageId = message.MessageId, CorrelationId = message.CorrelationId, CreatedAt = DateTimeOffset.UtcNow,
            InvoiceNumber = $"FB/{message.DocumentNumber}"
        });
        db.ProcessedMessages.Add(Processed(message));
        try
        {
            await db.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
            logger.LogInformation("FakeBilling decision created. MessageId {MessageId}, CorrelationId {CorrelationId}, SourceDocumentId {SourceDocumentId}", message.MessageId, message.CorrelationId, message.DocumentId);
            return BillingDecision.Created;
        }
        catch (DbUpdateException ex) when (BillingConflict.IsDuplicate(ex))
        {
            await transaction.RollbackAsync(ct);
            db.ChangeTracker.Clear();
            logger.LogInformation("FakeBilling decision duplicate. MessageId {MessageId}, CorrelationId {CorrelationId}, SourceDocumentId {SourceDocumentId}", message.MessageId, message.CorrelationId, message.DocumentId);
            return BillingDecision.Duplicate;
        }
    }

    private static ProcessedMessage Processed(DocumentConfirmedIntegrationEvent message) => new()
    {
        Id = Guid.NewGuid(), Consumer = ConsumerName, MessageId = message.MessageId,
        MessageType = nameof(DocumentConfirmedIntegrationEvent), CorrelationId = message.CorrelationId, ProcessedAt = DateTimeOffset.UtcNow
    };
}

public enum BillingDecision { Created, Duplicate, Ignored }

public static class BillingConflict
{
    public static bool IsDuplicate(DbUpdateException exception) =>
        exception.InnerException?.Message.Contains("UNIQUE constraint failed", StringComparison.OrdinalIgnoreCase) == true ||
        exception.InnerException?.Message.Contains("IX_", StringComparison.OrdinalIgnoreCase) == true;
}
