using WarehouseManagementSystem.Domain.ValueObjects;

namespace WarehouseManagementSystem.Tests.Support;

public sealed class DomainTestFixture
{
    public UserSnapshot User { get; } = new(
        Guid.Parse("11111111-1111-1111-1111-111111111111"),
        "Testomir.Testowski@gmail.com",
        "Testomir");
}
