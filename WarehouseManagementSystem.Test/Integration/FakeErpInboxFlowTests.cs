using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using WarehouseManagementSystem.Contracts;
using WarehouseManagementSystem.FakeERP;

namespace WarehouseManagementSystem.Test.Integration;

public sealed class FakeErpInboxFlowTests
{
    [Fact]
    public async Task Confirmed_message_updates_one_order_and_duplicate_is_idempotent()
    {
        var options = new DbContextOptionsBuilder<ErpDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        var correlation = Guid.NewGuid();
        await using (var setup = new ErpDbContext(options))
        {
            setup.WarehouseOrders.Add(new ErpWarehouseOrder { Id = Guid.NewGuid(), ExternalOrderId = "ERP-1", CorrelationId = correlation, CreatedAt = DateTimeOffset.UtcNow });
            await setup.SaveChangesAsync();
        }
        var message = new DocumentConfirmedIntegrationEvent { MessageId = Guid.NewGuid(), CorrelationId = correlation, DocumentId = Guid.NewGuid(), DocumentNumber = "PZ/1", DocumentType = "PZ", SourceWarehouseId = Guid.NewGuid(), OccurredAt = DateTimeOffset.UtcNow, ConfirmedAt = DateTimeOffset.UtcNow, ConfirmedBy = new ConfirmedByPayload { Id = Guid.NewGuid(), Name = "test", Email = "test@example.com" } };
        await using (var db = new ErpDbContext(options)) await new DocumentConfirmedHandler(db, NullLogger<DocumentConfirmedHandler>.Instance).HandleAsync(message, default);
        await using (var duplicateDb = new ErpDbContext(options)) await new DocumentConfirmedHandler(duplicateDb, NullLogger<DocumentConfirmedHandler>.Instance).HandleAsync(message, default);
        await using var assertion = new ErpDbContext(options);
        (await assertion.WarehouseOrders.SingleAsync()).Status.Should().Be("Confirmed");
        (await assertion.ProcessedMessages.CountAsync()).Should().Be(1);
    }
}
