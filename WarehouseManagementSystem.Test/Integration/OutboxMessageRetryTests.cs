using FluentAssertions;
using WarehouseManagementSystem.API.Integration;
using WarehouseManagementSystem.Infrastructure.Integration;

namespace WarehouseManagementSystem.Test.Integration;

public sealed class OutboxMessageRetryTests
{
    [Fact]
    public void Failed_publication_keeps_message_retryable()
    {
        var message = new OutboxMessage { Status = OutboxMessageStatus.Pending, RetryCount = 0 };

        OutboxMessageRetry.MarkFailed(message, new InvalidOperationException("RabbitMQ unavailable"));

        message.Status.Should().Be(OutboxMessageStatus.Failed);
        message.RetryCount.Should().Be(1);
        message.LastError.Should().Contain("unavailable");
        message.PublishedAt.Should().BeNull();
        message.NextAttemptAt.Should().NotBeNull();
    }

    [Fact]
    public void Failed_publication_is_abandoned_after_configured_limit()
    {
        var message = new OutboxMessage { Status = OutboxMessageStatus.Failed, RetryCount = 2 };

        OutboxMessageRetry.MarkFailed(message, new InvalidOperationException("Broker unavailable"), maxAttempts: 3);

        message.Status.Should().Be(OutboxMessageStatus.Abandoned);
        message.RetryCount.Should().Be(3);
        message.NextAttemptAt.Should().BeNull();
    }
}
