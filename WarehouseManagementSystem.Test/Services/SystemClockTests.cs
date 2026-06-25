using FluentAssertions;
using WarehouseManagementSystem.Infrastructure.Services;

namespace WarehouseManagementSystem.Tests.Services
{
    public class SystemClockTests
    {
        /// <summary>
        /// Verifies that UtcNow returns the current UTC time with accuracy within 1 second.
        /// </summary>
        [Fact]
        public void UtcNow_ShouldReturnCurrentUtcDateTimeOffset()
        {
            // Arrange
            var clock = new SystemClock();

            // Act
            var result = clock.UtcNow;
            var now = DateTimeOffset.UtcNow;

            // Assert
            // Sprawdzamy, że różnica nie jest większa niż np. 1 sekunda
            (result - now).Duration().Should().BeLessThanOrEqualTo(TimeSpan.FromSeconds(1));
        }

        /// <summary>
        /// Verifies that UtcNow returns different, increasing values on subsequent calls.
        /// </summary>
        [Fact]
        public void UtcNow_ShouldReturnDifferentValues_OnSubsequentCalls()
        {
            var clock = new SystemClock();

            var first = clock.UtcNow;
            System.Threading.Thread.Sleep(10); // 10ms przerwy
            var second = clock.UtcNow;

            second.Should().BeAfter(first);
        }
    }
}