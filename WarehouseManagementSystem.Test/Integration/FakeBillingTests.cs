using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using WarehouseManagementSystem.Contracts;
using WarehouseManagementSystem.FakeBilling;

namespace WarehouseManagementSystem.Test.Integration;

public sealed class FakeBillingTests
{
    [Fact]
    public async Task First_eligible_event_creates_one_billing_record()
    {
        await using var db = CreateDb();
        await Handler(db).HandleAsync(Message(), CancellationToken.None);
        (await db.FakeInvoices.CountAsync()).Should().Be(1);
        (await db.ProcessedMessages.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task Same_message_id_creates_one_billing_record()
    {
        await using var db = CreateDb();
        var message = Message();
        await Handler(db).HandleAsync(message, CancellationToken.None);
        await Handler(db).HandleAsync(message, CancellationToken.None);
        (await db.FakeInvoices.CountAsync()).Should().Be(1);
        (await db.ProcessedMessages.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task Different_message_ids_for_same_document_create_one_billing_record()
    {
        await using var db = CreateDb();
        var first = Message();
        var second = Message(documentId: first.DocumentId);
        await Handler(db).HandleAsync(first, CancellationToken.None);
        await Handler(db).HandleAsync(second, CancellationToken.None);
        (await db.FakeInvoices.CountAsync()).Should().Be(1);
        (await db.ProcessedMessages.CountAsync()).Should().Be(2);
    }

    [Fact]
    public async Task Ineligible_document_is_processed_without_invoice()
    {
        await using var db = CreateDb();
        var decision = await Handler(db).HandleAsync(Message(documentType: "PZ"), CancellationToken.None);
        decision.Should().Be(BillingDecision.Ignored);
        (await db.FakeInvoices.CountAsync()).Should().Be(0);
        (await db.ProcessedMessages.CountAsync()).Should().Be(1);
    }

    [Theory]
    [InlineData(0, true)]
    [InlineData(2, true)]
    [InlineData(3, false)]
    public void Retry_policy_stops_after_maximum_attempts(int completedRetries, bool expected) =>
        new BillingConsumerRetryPolicy(3).ShouldRetry(completedRetries).Should().Be(expected);

    private static BillingDbContext CreateDb() => new(new DbContextOptionsBuilder<BillingDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString())
        .ConfigureWarnings(x => x.Ignore(InMemoryEventId.TransactionIgnoredWarning))
        .Options);
    private static DocumentConfirmedBillingHandler Handler(BillingDbContext db) => new(db, NullLogger<DocumentConfirmedBillingHandler>.Instance);
    private static DocumentConfirmedIntegrationEvent Message(Guid? documentId = null, string documentType = "WZ") => new()
    {
        MessageId = Guid.NewGuid(),
        CorrelationId = Guid.NewGuid(),
        OccurredAt = DateTimeOffset.UtcNow,
        DocumentId = documentId ?? Guid.NewGuid(),
        DocumentNumber = "WZ/1",
        DocumentType = documentType,
        SourceWarehouseId = Guid.NewGuid(),
        ConfirmedAt = DateTimeOffset.UtcNow,
        ConfirmedBy = new ConfirmedByPayload { Id = Guid.NewGuid(), Name = "Test", Email = "test@example.com" }
    };
}
