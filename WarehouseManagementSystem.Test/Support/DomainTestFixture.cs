using WarehouseManagementSystem.Domain.ValueObjects;

namespace WarehouseManagementSystem.Tests.Support;

/// <summary>
/// Provides a test fixture for domain tests.
/// </summary>
public sealed class DomainTestFixture
{
    public UserSnapshot User { get; } = new(
        Guid.Parse("11111111-1111-1111-1111-111111111111"),
        "Testomir.Testowski@gmail.com",
        "Testomir");
}
