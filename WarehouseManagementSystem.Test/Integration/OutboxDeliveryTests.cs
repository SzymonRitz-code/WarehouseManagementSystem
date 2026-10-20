using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using RabbitMQ.Client;
using Testcontainers.MsSql;
using WarehouseManagementSystem.API.Integration;
using WarehouseManagementSystem.API.Integration.Configuration;
using WarehouseManagementSystem.Infrastructure.Integration;
using WarehouseManagementSystem.Infrastructure.Persistence;

namespace WarehouseManagementSystem.Test.Integration;

[Collection("SqlServer")]
public sealed class OutboxDeliveryTests : IAsyncLifetime
{
    private readonly MsSqlContainer _sql = new MsSqlBuilder().WithImage("mcr.microsoft.com/mssql/server:2022-latest").Build();
    public Task InitializeAsync() => _sql.StartAsync();
    public async Task DisposeAsync() => await _sql.DisposeAsync();

    private sealed class CrashBeforeCommit : SaveChangesInterceptor
    {
        public bool Crash { get; set; }
        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData,
            InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            if (Crash) throw new InvalidOperationException("Injected crash after publish before database commit");
            return ValueTask.FromResult(result);
        }
    }

    private sealed class ConcurrentShipmentSaves : SaveChangesInterceptor
    {
        private readonly TaskCompletionSource _bothReady = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private int _arrivals;
        public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData,
            InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            if (eventData.Context!.ChangeTracker.Entries<ShippingShipment>().Any(e => e.State == EntityState.Added))
            {
                if (Interlocked.Increment(ref _arrivals) == 2) _bothReady.TrySetResult();
                await _bothReady.Task.WaitAsync(TimeSpan.FromSeconds(15), cancellationToken);
            }
            return result;
        }
    }

    [Theory]
    [InlineData("before-publish", 1)]
    [InlineData("after-publish", 2)]
    [InlineData("after-commit", 1)]
    public async Task Failure_windows_preserve_at_least_once_delivery(string window, int expectedDeliveries)
    {
        var interceptor = new CrashBeforeCommit();
        var services = new ServiceCollection();
        services.AddDbContext<WarehouseManagementSystemDbContext>(o => o.UseSqlServer(_sql.GetConnectionString()).AddInterceptors(interceptor));
        using var provider = services.BuildServiceProvider();
        var id = Guid.NewGuid();
        using (var scope = provider.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<WarehouseManagementSystemDbContext>();
            await db.Database.EnsureCreatedAsync();
            db.OutboxMessages.Add(new OutboxMessage { Id = id, MessageId = id, Type = "Test", RoutingKey = "test", Payload = "{}", OccurredAt = DateTimeOffset.UtcNow });
            await db.SaveChangesAsync();
        }
        var channel = new Mock<IModel>();
        channel.SetupGet(c => c.IsOpen).Returns(true);
        channel.Setup(c => c.CreateBasicProperties()).Returns(() => new Mock<IBasicProperties>().SetupAllProperties().Object);
        var delivered = 0;
        var crash = window == "before-publish";
        channel.Setup(c => c.BasicPublish(It.IsAny<string>(), It.IsAny<string>(), true, It.IsAny<IBasicProperties>(), It.IsAny<ReadOnlyMemory<byte>>()))
            .Callback(() => { if (crash) throw new IOException("Injected broker outage"); delivered++; });
        var connection = new Mock<IConnection>();
        connection.SetupGet(c => c.IsOpen).Returns(true);
        connection.Setup(c => c.CreateModel()).Returns(channel.Object);
        var factory = new Mock<IRabbitMqConnectionFactory>();
        factory.Setup(f => f.CreateConnection()).Returns(connection.Object);
        var worker = new OutboxPublisherWorker(provider.GetRequiredService<IServiceScopeFactory>(), factory.Object,
            Mock.Of<IRabbitMqTopologyConfigurator>(), Options.Create(new MessagingOptions()), NullLogger<OutboxPublisherWorker>.Instance);
        interceptor.Crash = window == "after-publish";
        if (interceptor.Crash)
            await Assert.ThrowsAsync<InvalidOperationException>(() => worker.PublishPendingMessagesAsync(default));
        else await worker.PublishPendingMessagesAsync(default);
        interceptor.Crash = false;
        crash = false;
        using (var scope = provider.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<WarehouseManagementSystemDbContext>();
            var row = await db.OutboxMessages.SingleAsync(m => m.Id == id);
            if (window != "after-commit") row.Status.Should().NotBe(OutboxMessageStatus.Published);
            row.NextAttemptAt = null;
            await db.SaveChangesAsync();
        }
        await worker.PublishPendingMessagesAsync(default);
        delivered.Should().Be(expectedDeliveries);
        using var finalScope = provider.CreateScope();
        (await finalScope.ServiceProvider.GetRequiredService<WarehouseManagementSystemDbContext>().OutboxMessages.SingleAsync(m => m.Id == id))
            .Status.Should().Be(OutboxMessageStatus.Published);
    }

    [Fact]
    public async Task Concurrent_shipping_deliveries_commit_one_effect_and_ack_both()
    {
        var services = new ServiceCollection();
        var concurrentSaves = new ConcurrentShipmentSaves();
        services.AddDbContext<WarehouseManagementSystemDbContext>(o => o.UseSqlServer(_sql.GetConnectionString()).AddInterceptors(concurrentSaves));
        using var provider = services.BuildServiceProvider();
        using (var scope = provider.CreateScope())
            await scope.ServiceProvider.GetRequiredService<WarehouseManagementSystemDbContext>().Database.EnsureCreatedAsync();
        var consumer = new ShippingDocumentConfirmedConsumer(provider.GetRequiredService<IServiceScopeFactory>(),
            Mock.Of<IRabbitMqConnectionFactory>(), Mock.Of<IRabbitMqTopologyConfigurator>(), Options.Create(new MessagingOptions()),
            NullLogger<ShippingDocumentConfirmedConsumer>.Instance);
        var message = new WarehouseManagementSystem.Contracts.DocumentConfirmedIntegrationEvent
        {
            MessageId = Guid.NewGuid(), DocumentId = Guid.NewGuid(), CorrelationId = Guid.NewGuid(),
            DocumentNumber = "WZ/1", DocumentType = "WZ", SourceWarehouseId = Guid.NewGuid(),
            OccurredAt = DateTimeOffset.UtcNow, ConfirmedAt = DateTimeOffset.UtcNow,
            ConfirmedBy = new() { Id = Guid.NewGuid(), Name = "Operator", Email = "test@example.com" },
            Items = [new() { ProductId = Guid.NewGuid(), Quantity = 1 }]
        };
        var delivery = new RabbitMQ.Client.Events.BasicDeliverEventArgs
        {
            Body = System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(message, new System.Text.Json.JsonSerializerOptions(System.Text.Json.JsonSerializerDefaults.Web))
        };
        var results = await Task.WhenAll(consumer.HandleMessageAsync(delivery, default), consumer.HandleMessageAsync(delivery, default));
        results.Should().OnlyContain(x => x);
        // Simulate a process crash after the durable commit but before sending the original ACK.
        (await consumer.HandleMessageAsync(delivery, default)).Should().BeTrue();
        using var verifyScope = provider.CreateScope();
        var db = verifyScope.ServiceProvider.GetRequiredService<WarehouseManagementSystemDbContext>();
        (await db.ShippingShipments.CountAsync()).Should().Be(1);
        (await db.ProcessedMessages.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task Two_publishers_do_not_publish_the_same_locked_batch()
    {
        var services = new ServiceCollection();
        services.AddDbContext<WarehouseManagementSystemDbContext>(o => o.UseSqlServer(_sql.GetConnectionString()));
        using var provider = services.BuildServiceProvider();
        using (var scope = provider.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<WarehouseManagementSystemDbContext>();
            await db.Database.EnsureCreatedAsync();
            db.OutboxMessages.Add(new OutboxMessage { Id = Guid.NewGuid(), MessageId = Guid.NewGuid(), Type = "Test", RoutingKey = "test", Payload = "{}", OccurredAt = DateTimeOffset.UtcNow });
            await db.SaveChangesAsync();
        }
        var deliveries = 0;
        OutboxPublisherWorker Worker()
        {
            var channel = new Mock<IModel>();
            channel.SetupGet(c => c.IsOpen).Returns(true);
            channel.Setup(c => c.CreateBasicProperties()).Returns(() => new Mock<IBasicProperties>().SetupAllProperties().Object);
            channel.Setup(c => c.BasicPublish(It.IsAny<string>(), It.IsAny<string>(), true, It.IsAny<IBasicProperties>(), It.IsAny<ReadOnlyMemory<byte>>()))
                .Callback(() => Interlocked.Increment(ref deliveries));
            var connection = new Mock<IConnection>();
            connection.SetupGet(c => c.IsOpen).Returns(true);
            connection.Setup(c => c.CreateModel()).Returns(channel.Object);
            var factory = new Mock<IRabbitMqConnectionFactory>();
            factory.Setup(f => f.CreateConnection()).Returns(connection.Object);
            return new OutboxPublisherWorker(provider.GetRequiredService<IServiceScopeFactory>(), factory.Object,
                Mock.Of<IRabbitMqTopologyConfigurator>(), Options.Create(new MessagingOptions()), NullLogger<OutboxPublisherWorker>.Instance);
        }
        using var first = Worker();
        using var second = Worker();
        await Task.WhenAll(first.PublishPendingMessagesAsync(default), second.PublishPendingMessagesAsync(default));
        deliveries.Should().Be(1);
    }
}
