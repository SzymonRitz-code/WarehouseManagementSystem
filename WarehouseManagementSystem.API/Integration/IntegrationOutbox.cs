using System.Text.Json;
using WarehouseManagementSystem.Infrastructure.Integration;
using WarehouseManagementSystem.Infrastructure.Persistence;

namespace WarehouseManagementSystem.API.Integration;

public class IntegrationOutbox : IIntegrationOutbox
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);
    private readonly WarehouseManagementSystemDbContext _dbContext;

    public IntegrationOutbox(WarehouseManagementSystemDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public void Add<TMessage>(Guid messageId, Guid? correlationId, string routingKey, TMessage message, DateTimeOffset occurredAt)
    {
        // PUBLISHER INTENT: to jeszcze nie jest publish do brokera, tylko zapis intencji publikacji do Outbox.
        // Wpis do Outboxa jest zapisywany przez ten sam DbContext co zmiana domenowa.
        // To jest sedno transactional boundary: stan biznesowy i "intencja publikacji"
        // lądują w jednej transakcji SQL, a sam publish dzieje się dopiero asynchronicznie.
        var outboxMessage = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            MessageId = messageId,
            CorrelationId = correlationId,
            Type = typeof(TMessage).Name,
            RoutingKey = routingKey,
            Payload = JsonSerializer.Serialize(message, SerializerOptions),
            OccurredAt = occurredAt,
            Status = OutboxMessageStatus.Pending
        };

        _dbContext.OutboxMessages.Add(outboxMessage);
    }
}
