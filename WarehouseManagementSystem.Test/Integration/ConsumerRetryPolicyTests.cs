using FluentAssertions;
using WarehouseManagementSystem.API.Integration;

namespace WarehouseManagementSystem.Test.Integration;

public sealed class ConsumerRetryPolicyTests
{
    [Theory]
    [InlineData(0, true)]
    [InlineData(2, true)]
    [InlineData(3, false)]
    public void Retry_is_limited(int completedRetries, bool expected)
    {
        new ConsumerRetryPolicy(3).ShouldRetry(completedRetries).Should().Be(expected);
    }

    [Fact]
    public void Retry_count_is_read_from_message_headers()
    {
        var headers = new Dictionary<string, object> { [ConsumerRetryPolicy.RetryCountHeader] = 2 };

        ConsumerRetryPolicy.GetRetryCount(headers).Should().Be(2);
    }
}
