using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using WarehouseManagementSystem.API.Services.Documents.Command;
using WarehouseManagementSystem.Contracts;
using WarehouseManagementSystem.Domain.Enums;
using WarehouseManagementSystem.Domain.ValueObjects;
using WarehouseManagementSystem.Infrastructure.Integration;
using WarehouseManagementSystem.Infrastructure.Persistence;

namespace WarehouseManagementSystem.API.Integration;

/// <summary>Application-facing Inbox handler. Its transaction contains the Inbox marker, import map and WMS document.</summary>
public sealed class ErpDocumentCreateHandler(
    WarehouseManagementSystemDbContext db,
    IDocumentCommandService documents,
    ILogger<ErpDocumentCreateHandler> logger)
{
    public const string ConsumerName = "Wms.ErpDocumentCreate";

    public async Task HandleAsync(CreateWarehouseDocumentCommand command, CancellationToken ct)
    {
        if (await db.InboxMessages.AnyAsync(x => x.Consumer == ConsumerName && x.MessageId == command.MessageId, ct))
        {
            logger.LogInformation("WMS Inbox skipped duplicate MessageId {MessageId}, CorrelationId {CorrelationId}", command.MessageId, command.CorrelationId);
            return;
        }

        var fingerprint = Fingerprint(command);
        var existingImport = await db.ErpOrderImports.SingleOrDefaultAsync(x => x.ExternalOrderId == command.ExternalOrderId, ct);
        if (existingImport is not null)
        {
            if (!string.Equals(existingImport.PayloadFingerprint, fingerprint, StringComparison.Ordinal))
                throw new PermanentIntegrationException($"ERP order '{command.ExternalOrderId}' conflicts with the already imported payload.");

            db.InboxMessages.Add(ProcessedInbox(command));
            await db.SaveChangesAsync(ct);
            logger.LogInformation("WMS Inbox treated ExternalOrderId {ExternalOrderId} as business duplicate, CorrelationId {CorrelationId}", command.ExternalOrderId, command.CorrelationId);
            return;
        }

        if (!Enum.TryParse<DocumentType>(command.DocumentType, ignoreCase: true, out var documentType))
            throw new PermanentIntegrationException($"Unsupported WMS document type '{command.DocumentType}'.");
        if (string.IsNullOrWhiteSpace(command.ExternalOrderId) || command.Items.Count == 0)
            throw new PermanentIntegrationException("ExternalOrderId and at least one item are required.");

        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        try
        {
            // This deliberately calls the existing application service rather than adding EF entities directly.
            var document = await documents.CreateDocumentAsync(
                documentType,
                new UserSnapshot(Guid.Empty, "erp@integration.local", "ERP integration"),
                command.SourceWarehouseId,
                command.Items.Select(x => new DocumentItemDraft(x.ProductId, x.Quantity, x.ProductBatchId, x.SourceZoneId, x.TargetZoneId)),
                command.DocumentDate,
                command.TargetWarehouseId,
                command.Notes,
                ct);

            db.ErpOrderImports.Add(new ErpOrderImport
            {
                Id = Guid.NewGuid(),
                ExternalOrderId = command.ExternalOrderId,
                WmsDocumentId = document.Id,
                CorrelationId = command.CorrelationId,
                PayloadFingerprint = fingerprint,
                CreatedAt = DateTimeOffset.UtcNow
            });
            db.InboxMessages.Add(ProcessedInbox(command));
            await db.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
            logger.LogInformation("WMS created document {DocumentId} for ERP order {ExternalOrderId}, CorrelationId {CorrelationId}", document.Id, command.ExternalOrderId, command.CorrelationId);
        }
        catch (DbUpdateException ex) when (IsDuplicate(ex))
        {
            // A concurrent delivery won the unique index race. It has already committed the business effect.
            logger.LogInformation("WMS Inbox skipped concurrently handled MessageId {MessageId}", command.MessageId);
        }
    }

    private static InboxMessage ProcessedInbox(CreateWarehouseDocumentCommand command) => new()
    {
        Id = Guid.NewGuid(),
        MessageId = command.MessageId,
        Consumer = ConsumerName,
        MessageType = nameof(CreateWarehouseDocumentCommand),
        CorrelationId = command.CorrelationId,
        ReceivedAt = DateTimeOffset.UtcNow,
        ProcessedAt = DateTimeOffset.UtcNow,
        Status = "Processed"
    };

    private static string Fingerprint(CreateWarehouseDocumentCommand command) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new
        { command.ExternalOrderId, command.DocumentType, command.SourceWarehouseId, command.TargetWarehouseId, command.DocumentDate, command.Notes, command.Items }))));

    private static bool IsDuplicate(DbUpdateException exception) => exception.InnerException?.Message.Contains("IX_InboxMessages_Consumer_MessageId", StringComparison.OrdinalIgnoreCase) == true ||
        exception.InnerException?.Message.Contains("ExternalOrderId", StringComparison.OrdinalIgnoreCase) == true;
}

public sealed class PermanentIntegrationException(string message) : Exception(message);
