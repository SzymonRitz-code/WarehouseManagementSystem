namespace WarehouseManagementSystem.Infrastructure.Services;

public class SystemClock : ISystemClock
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;

    public DateTime Now => DateTime.Now;
}
public interface ISystemClock
{
    /// <summary>
    /// Zwraca aktualny czas UTC
    /// </summary>
    DateTimeOffset UtcNow { get; }

    /// <summary>
    /// Zwraca aktualny czas lokalny
    /// </summary>
    DateTime Now { get; }
}