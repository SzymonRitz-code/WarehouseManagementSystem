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
    }
}
