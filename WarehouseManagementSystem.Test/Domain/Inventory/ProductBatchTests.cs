using FluentAssertions;
using WarehouseManagementSystem.Domain.Model.InventoryDomain;
using WarehouseManagementSystem.Tests.Support;

namespace WarehouseManagementSystem.Test.Domain.InventoryDomain;

[Trait("Category", "Inventory_ProductBatch")]
public class ProductBatchTests(DomainTestFixture fixture) : IClassFixture<DomainTestFixture>
{

    [Fact]
    public void Constructor_Should_Set_Properties_Correctly()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var batchNumber = "BATCH-001";
        var manufacturedDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1));
        var expirationDate = DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(6));

        // Act
        var batch = CreateBatch(productId, batchNumber, manufacturedDate, expirationDate);

        // Assert
        batch.Id.Should().NotBe(Guid.Empty);
        batch.ProductId.Should().Be(productId);
        batch.BatchNumber.Should().Be(batchNumber);
        batch.ManufacturedDate.Should().Be(manufacturedDate);
        batch.ExpirationDate.Should().Be(expirationDate);
        batch.CreatedAt.Should().BeOnOrBefore(DateTimeOffset.UtcNow);
    }

    [Theory]
    [ClassData(typeof(InvalidRequiredStringTestData))]
    public void SetBatchNumber_Should_Throw_On_Invalid_BatchNumber(string? invalidBatch)
    {
        var batch = CreateBatch(batchNumber: "VALID");

        Action act = () => batch.SetBatchNumber(invalidBatch!);

        act.Should().Throw<ArgumentException>()
           .WithMessage("Batch number is required.");
    }

    [Fact]
    public void SetBatchNumber_Should_Throw_When_Length_Exceeds_50()
    {
        var batch = CreateBatch(batchNumber: "VALID");
        var longBatch = new string('A', 51);

        Action act = () => batch.SetBatchNumber(longBatch);

        act.Should().Throw<ArgumentException>()
           .WithMessage("Batch number cannot exceed 50 characters.");
    }

    [Fact]
    public void SetManufacturingDates_Should_Throw_If_ManufacturedDate_In_Future()
    {
        var batch = CreateBatch();
        var futureDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1));

        Action act = () => batch.SetManufacturingDates(futureDate, null);

        act.Should().Throw<ArgumentException>()
           .WithMessage("Manufactured date cannot be in the future.");
    }

    [Fact]
    public void SetManufacturingDates_Should_Throw_If_Expiration_Before_Manufactured()
    {
        var batch = CreateBatch();
        var manufacturedDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1));
        var expirationDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-2));

        Action act = () => batch.SetManufacturingDates(manufacturedDate, expirationDate);

        act.Should().Throw<ArgumentException>()
           .WithMessage("Expiration date cannot be earlier than manufactured date.");
    }

    [Fact]
    public void IsExpired_Should_Return_True_When_ExpirationDate_Passed()
    {
        var expiredDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1));
        var batch = CreateBatch(expirationDate: expiredDate);

        batch.IsExpired().Should().BeTrue();
    }

    [Fact]
    public void IsExpired_Should_Return_False_When_ExpirationDate_NotSet()
    {
        var batch = CreateBatch();

        batch.IsExpired().Should().BeFalse();
    }

    [Fact]
    public void ExpiresSoon_Should_Return_True_When_Within_Threshold()
    {
        var expiration = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(2));
        var batch = CreateBatch(expirationDate: expiration);

        batch.ExpiresSoon(3).Should().BeTrue();
    }

    [Fact]
    public void ExpiresSoon_Should_Return_False_When_Beyond_Threshold()
    {
        var expiration = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(10));
        var batch = CreateBatch(expirationDate: expiration);

        batch.ExpiresSoon(5).Should().BeFalse();
    }

    [Fact]
    public void ExpiresSoon_Should_Return_False_When_ExpirationDate_NotSet()
    {
        var batch = CreateBatch();

        batch.ExpiresSoon(5).Should().BeFalse();
    }

    private ProductBatch CreateBatch(
        Guid? productId = null,
        string batchNumber = "BATCH",
        DateOnly? manufacturedDate = null,
        DateOnly? expirationDate = null)
        => new(productId ?? Guid.NewGuid(), batchNumber, fixture.User, manufacturedDate, expirationDate);
}
