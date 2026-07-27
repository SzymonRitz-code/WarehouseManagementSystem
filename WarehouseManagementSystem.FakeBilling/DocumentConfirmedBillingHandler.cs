using Microsoft.EntityFrameworkCore;
using WarehouseManagementSystem.Contracts;

namespace WarehouseManagementSystem.FakeBilling;

public sealed class DocumentConfirmedBillingHandler(BillingDbContext billingDbContext, ILogger<DocumentConfirmedBillingHandler> logger)
{
    public const string ConsumerName = "FakeBilling.DocumentConfirmed";

    public async Task<BillingDecision> HandleAsync(DocumentConfirmedIntegrationEvent message, CancellationToken ct)
    {
        if (await billingDbContext.ProcessedMessages.AnyAsync(x => x.Consumer == ConsumerName && x.MessageId == message.MessageId, ct))
        {
            logger.LogInformation(
                "FakeBilling decision duplicate. MessageId {MessageId}, CorrelationId {CorrelationId}, SourceDocumentId {SourceDocumentId}",
                message.MessageId, message.CorrelationId, message.DocumentId);
            return BillingDecision.Duplicate;
        }

        await using var billingTransaction = await billingDbContext.Database.BeginTransactionAsync(ct);
        if (message.DocumentType != "WZ")
        {
            billingDbContext.ProcessedMessages.Add(Processed(message));
            await billingDbContext.SaveChangesAsync(ct);
            await billingTransaction.CommitAsync(ct);
            logger.LogInformation(
                "FakeBilling decision ignored. MessageId {MessageId}, CorrelationId {CorrelationId}, SourceDocumentId {SourceDocumentId}",
                message.MessageId, message.CorrelationId, message.DocumentId);
            return BillingDecision.Ignored;
        }

        if (await billingDbContext.FakeInvoices.AnyAsync(x => x.SourceDocumentId == message.DocumentId, ct))
        {
            billingDbContext.ProcessedMessages.Add(Processed(message));
            await billingDbContext.SaveChangesAsync(ct);
            await billingTransaction.CommitAsync(ct);
            logger.LogInformation(
                "FakeBilling decision duplicate. MessageId {MessageId}, CorrelationId {CorrelationId}, SourceDocumentId {SourceDocumentId}",
                message.MessageId, message.CorrelationId, message.DocumentId);
            return BillingDecision.Duplicate;
        }

        billingDbContext.FakeInvoices.Add(new FakeInvoice
        {
            Id = Guid.NewGuid(),
            SourceDocumentId = message.DocumentId,
            SourceDocumentNumber = message.DocumentNumber,
            SourceMessageId = message.MessageId,
            CorrelationId = message.CorrelationId,
            CreatedAt = DateTimeOffset.UtcNow,
            InvoiceNumber = $"FB/{message.DocumentNumber}"
        });
        billingDbContext.ProcessedMessages.Add(Processed(message));
        try
        {
            await billingDbContext.SaveChangesAsync(ct);
            await billingTransaction.CommitAsync(ct);
            logger.LogInformation(
                "FakeBilling decision created. MessageId {MessageId}, CorrelationId {CorrelationId}, SourceDocumentId {SourceDocumentId}",
                message.MessageId, message.CorrelationId, message.DocumentId);
            return BillingDecision.Created;
        }
        catch (DbUpdateException ex) when (BillingConflict.IsDuplicate(ex))
        {
            await billingTransaction.RollbackAsync(ct);
            billingDbContext.ChangeTracker.Clear();
            logger.LogInformation(
                "FakeBilling decision duplicate. MessageId {MessageId}, CorrelationId {CorrelationId}, SourceDocumentId {SourceDocumentId}",
                message.MessageId, message.CorrelationId, message.DocumentId);
            return BillingDecision.Duplicate;
        }
    }

    private static ProcessedMessage Processed(DocumentConfirmedIntegrationEvent message)
    {
        return new()
        {
            Id = Guid.NewGuid(),
            Consumer = ConsumerName,
            MessageId = message.MessageId,
            MessageType = nameof(DocumentConfirmedIntegrationEvent),
            CorrelationId = message.CorrelationId,
            ProcessedAt = DateTimeOffset.UtcNow
        };
    }
}

public enum BillingDecision { Created, Duplicate, Ignored }

public static class BillingConflict
{
    public static bool IsDuplicate(DbUpdateException exception)
    {
        return
            exception.InnerException?
                .Message.Contains("UNIQUE constraint failed", StringComparison.OrdinalIgnoreCase) == true
        ||
            exception.InnerException?
                .Message.Contains("IX_", StringComparison.OrdinalIgnoreCase) == true;
    }
}
