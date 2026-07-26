using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using WarehouseManagementSystem.Contracts;
using WarehouseManagementSystem.FakeShipping;

namespace WarehouseManagementSystem.Test.Integration;

public sealed class FakeShippingTests
{
    [Fact]
    public async Task Duplicate_event_creates_only_one_shipment()
    {
        var options = new DbContextOptionsBuilder<ShippingDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        await using var db = new ShippingDbContext(options);
        var handler = new DocumentConfirmedHandler(db, NullLogger<DocumentConfirmedHandler>.Instance);
        var message = new DocumentConfirmedIntegrationEvent
        {
            MessageId = Guid.NewGuid(), CorrelationId = Guid.NewGuid(), OccurredAt = DateTimeOffset.UtcNow,
            DocumentId = Guid.NewGuid(), DocumentNumber = "WZ/1", DocumentType = "WZ", SourceWarehouseId = Guid.NewGuid(),
            ConfirmedAt = DateTimeOffset.UtcNow, ConfirmedBy = new ConfirmedByPayload { Id = Guid.NewGuid(), Name = "Test", Email = "test@example.com" }
        };

        await handler.HandleAsync(message, CancellationToken.None);
        await handler.HandleAsync(message, CancellationToken.None);

        (await db.Shipments.CountAsync()).Should().Be(1);
        (await db.ProcessedMessages.CountAsync()).Should().Be(1);
    }
}
