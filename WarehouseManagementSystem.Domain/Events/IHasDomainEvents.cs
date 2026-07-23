namespace WarehouseManagementSystem.Domain.Events;

/// <summary>
/// Implemented by aggregate roots that produce domain events.
/// Application layer (UnitOfWork.SaveChangesAsync) collects and dispatches events after commit.
/// </summary>
public interface IHasDomainEvents
{
    /// <summary>
    /// Returns the events raised by this aggregate since the last commit.
    /// </summary>
    IReadOnlyCollection<IDomainEvent> DomainEvents { get; }

    /// <summary>
    /// Clears all raised events after they have been dispatched.
    /// Called by the infrastructure layer once events are handled.
    /// </summary>
    void ClearDomainEvents();
}
