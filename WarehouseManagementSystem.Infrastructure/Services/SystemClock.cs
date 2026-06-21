namespace WarehouseManagementSystem.Infrastructure.Services;

public class SystemClock : ISystemClock
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;

    public DateTime Now => DateTime.Now;
}
public interface ISystemClock
{
    /// <summary>
    /// Gets the current UTC time.
    /// </summary>
    /// <value>Current UTC date and time.</value>
    DateTimeOffset UtcNow { get; }

    /// <summary>
    /// Gets the current local system time.
    /// </summary>
    /// <value>Current local system date and time.</value>
    DateTime Now { get; }
}
