using System.Text;
using System.Text.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using WarehouseManagementSystem.API.Integration;
using WarehouseManagementSystem.API.Integration.Configuration;
using WarehouseManagementSystem.Contracts;
using WarehouseManagementSystem.Infrastructure.Persistence;

namespace WarehouseManagementSystem.Test.Integration;

public class MessagingCompletionTests
{
    private static DocumentConfirmedIntegrationEvent Valid() => new()
    {
        MessageId = Guid.NewGuid(), DocumentId = Guid.NewGuid(), CorrelationId = Guid.NewGuid(),
        SourceWarehouseId = Guid.NewGuid(), DocumentNumber = "WZ/1", DocumentType = "WZ",
        OccurredAt = DateTimeOffset.UtcNow, ConfirmedAt = DateTimeOffset.UtcNow,
        ConfirmedBy = new() { Id = Guid.NewGuid(), Name = "Operator", Email = "operator@example.com" },
        Items = [new() { ProductId = Guid.NewGuid(), Quantity = 1 }]
    };

    [Fact]
    public void Supported_confirmation_passes_validation() => DocumentConfirmedContract.Validate(Valid());

    [Theory]
    [InlineData("{}")]
    [InlineData("{\"schemaVersion\":2}")]
    [InlineData("{\"aggregateVersion\":2}")]
    public void Incomplete_or_unsupported_contract_is_permanent_failure(string json)
    {
        var message = JsonSerializer.Deserialize<DocumentConfirmedIntegrationEvent>(json, new JsonSerializerOptions(JsonSerializerDefaults.Web))!;
        Action act = () => DocumentConfirmedContract.Validate(message);
        act.Should().Throw<PermanentIntegrationException>();
    }

    [Fact]
    public void Nonpositive_item_is_rejected()
    {
        var json = JsonSerializer.Serialize(Valid()).Replace("\"Quantity\":1", "\"Quantity\":0");
        Action act = () => DocumentConfirmedContract.Validate(JsonSerializer.Deserialize<DocumentConfirmedIntegrationEvent>(json)!);
        act.Should().Throw<PermanentIntegrationException>();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Retry_copy_requires_confirmation_and_a_route(bool returned)
    {
        var channel = new Mock<IModel>();
        channel.Setup(c => c.BasicPublish("retry", "key", true, It.IsAny<IBasicProperties>(), It.IsAny<ReadOnlyMemory<byte>>()))
            .Callback(() => { if (returned) channel.Raise(c => c.BasicReturn += null, new BasicReturnEventArgs()); });
        if (!returned) channel.Setup(c => c.WaitForConfirmsOrDie(It.IsAny<TimeSpan>())).Throws(new TimeoutException());
        Action act = () => ConfirmedMessagePublisher.Publish(channel.Object, "retry", "key", Mock.Of<IBasicProperties>(), ReadOnlyMemory<byte>.Empty, TimeSpan.FromSeconds(1));
        act.Should().Throw<Exception>();
    }

    [Fact]
    public async Task Redelivery_after_commit_before_ack_has_one_business_effect()
    {
        var services = new ServiceCollection();
        var name = Guid.NewGuid().ToString();
        services.AddDbContext<WarehouseManagementSystemDbContext>(o => o.UseInMemoryDatabase(name));
        using var provider = services.BuildServiceProvider();
        var consumer = new ShippingDocumentConfirmedConsumer(provider.GetRequiredService<IServiceScopeFactory>(),
            Mock.Of<IRabbitMqConnectionFactory>(), Mock.Of<IRabbitMqTopologyConfigurator>(), Options.Create(new MessagingOptions()),
            NullLogger<ShippingDocumentConfirmedConsumer>.Instance);
        var message = Valid();
        var delivery = new BasicDeliverEventArgs { Body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(message, new JsonSerializerOptions(JsonSerializerDefaults.Web))) };
        (await consumer.HandleMessageAsync(delivery, default)).Should().BeTrue();
        (await consumer.HandleMessageAsync(delivery, default)).Should().BeTrue();
        using var scope = provider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<WarehouseManagementSystemDbContext>();
        (await db.ShippingShipments.CountAsync()).Should().Be(1);
        (await db.ProcessedMessages.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task Reconciliation_repairs_missing_projection_and_is_idempotent()
    {
        var services = new ServiceCollection();
        var name = Guid.NewGuid().ToString();
        services.AddDbContext<WarehouseManagementSystemDbContext>(o => o.UseInMemoryDatabase(name));
        using var provider = services.BuildServiceProvider();
        var document = new WarehouseManagementSystem.Domain.Model.DocumentsDomain.Document(DateTime.UtcNow,
            WarehouseManagementSystem.Domain.Enums.DocumentType.WZ,
            new WarehouseManagementSystem.Domain.ValueObjects.UserSnapshot(Guid.NewGuid(), "operator@example.com", "Operator"), Guid.NewGuid());
        document.SetNumber("WZ/repair");
        document.AddItem(new WarehouseManagementSystem.Domain.Model.DocumentsDomain.DocumentItem(Guid.NewGuid(), 1));
        document.Confirm(new WarehouseManagementSystem.Domain.ValueObjects.UserSnapshot(Guid.NewGuid(), "operator@example.com", "Operator"));
        using (var scope = provider.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<WarehouseManagementSystemDbContext>();
            db.Documents.Add(document);
            db.Entry(document).Property(x => x.RowVersion).CurrentValue = [1];
            await db.SaveChangesAsync();
        }
        var reconciler = new ShippingProjectionReconciler(provider.GetRequiredService<IServiceScopeFactory>(),
            Options.Create(new MessagingOptions()), NullLogger<ShippingProjectionReconciler>.Instance);
        (await reconciler.ReconcileAsync(default)).Should().Be(1);
        (await reconciler.ReconcileAsync(default)).Should().Be(0);
    }

    [Fact]
    public void Retry_backoff_increases_and_is_bounded()
    {
        var message = new WarehouseManagementSystem.Infrastructure.Integration.OutboxMessage { RetryCount = 5 };
        var now = DateTimeOffset.UtcNow;
        OutboxMessageRetry.MarkFailed(message, new IOException(), maxAttempts: 100, retryDelaySeconds: 30);
        (message.NextAttemptAt!.Value - now).TotalSeconds.Should().BeInRange(768, 1153);
        message.RetryCount = 50;
        OutboxMessageRetry.MarkFailed(message, new IOException(), maxAttempts: 100, retryDelaySeconds: 30);
        (message.NextAttemptAt!.Value - DateTimeOffset.UtcNow).TotalSeconds.Should().BeInRange(2879, 4321);
    }
}
