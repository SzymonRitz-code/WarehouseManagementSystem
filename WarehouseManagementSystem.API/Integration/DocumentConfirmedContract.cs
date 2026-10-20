using WarehouseManagementSystem.Contracts;

namespace WarehouseManagementSystem.API.Integration;

/// <summary>Rejects syntactically valid JSON that cannot represent a supported document confirmation.</summary>
public static class DocumentConfirmedContract
{
    /// <summary>Validates identifiers, schema, document type, actor and positive item quantities before any write.</summary>
    public static void Validate(DocumentConfirmedIntegrationEvent message)
    {
        if (message.SchemaVersion != 1 || message.AggregateVersion != 1 ||
            message.MessageId == Guid.Empty || message.DocumentId == Guid.Empty ||
            message.SourceWarehouseId == Guid.Empty || message.CorrelationId == Guid.Empty ||
            string.IsNullOrWhiteSpace(message.DocumentNumber) || message.DocumentNumber.Length > 50 ||
            message.DocumentType is not ("PZ" or "WZ" or "MM" or "ADJ") ||
            message.OccurredAt == default || message.ConfirmedAt == default ||
            message.ConfirmedBy is null || message.ConfirmedBy.Id == Guid.Empty ||
            string.IsNullOrWhiteSpace(message.ConfirmedBy.Name) || string.IsNullOrWhiteSpace(message.ConfirmedBy.Email) ||
            message.TargetWarehouseId == Guid.Empty ||
            (message.DocumentType == "MM" && (message.TargetWarehouseId is null || message.TargetWarehouseId == message.SourceWarehouseId)) ||
            message.Items is null || message.Items.Count == 0 ||
            message.Items.Any(x => x is null || x.ProductId == Guid.Empty || x.Quantity <= 0 ||
                x.ProductBatchId == Guid.Empty || x.SourceZoneId == Guid.Empty || x.TargetZoneId == Guid.Empty))
            throw new PermanentIntegrationException("Invalid or unsupported DocumentConfirmed contract.");
    }
}
