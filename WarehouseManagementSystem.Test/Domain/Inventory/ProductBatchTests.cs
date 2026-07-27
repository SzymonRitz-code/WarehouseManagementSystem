using FluentAssertions;
using WarehouseManagementSystem.Domain.Model.InventoryDomain;
using WarehouseManagementSystem.Tests.Support;

namespace WarehouseManagementSystem.Test.Domain.InventoryDomain;
/// <summary>
/// Tests for the <see cref="ProductBatch"/> class in the Inventory domain.
/// </summary>
/// <param name="fixture">The domain test fixture used for setting up test dependencies.</param>
[Trait("Category", "Inventory_ProductBatch")]
public class ProductBatchTests(DomainTestFixture fixture) : IClassFixture<DomainTestFixture>
{
    /// <summary>
    /// Tests that the constructor of the <see cref="ProductBatch"/> class sets properties correctly.
    /// </summary>
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

    /// <summary>
    /// Tests that the <see cref="ProductBatch.SetBatchNumber(string)"/> method throws an exception when provided with an invalid batch number.
    /// </summary>
    /// <param name="invalidBatch"></param>
    [Theory]
    [ClassData(typeof(InvalidRequiredStringTestData))]
    public void SetBatchNumber_Should_Throw_On_Invalid_BatchNumber(string? invalidBatch)
    {
        var batch = CreateBatch(batchNumber: "VALID");

        Action act = () => batch.SetBatchNumber(invalidBatch!);

        act.Should().Throw<ArgumentException>()
           .WithMessage("Batch number is required.");
    }
    /// <summary>
    /// Tests that the <see cref="ProductBatch.SetBatchNumber(string)"/> method throws an exception when the batch number exceeds 50 characters.
    /// </summary>
    [Fact]
    public void SetBatchNumber_Should_Throw_When_Length_Exceeds_50()
    {
        var batch = CreateBatch(batchNumber: "VALID");
        var longBatch = new string('A', 51);

        Action act = () => batch.SetBatchNumber(longBatch);

        act.Should().Throw<ArgumentException>()
           .WithMessage("Batch number cannot exceed 50 characters.");
    }

    /// <summary>
    /// Tests that the <see cref="ProductBatch.SetManufacturingDates(DateOnly?, DateOnly?)"/> method throws an exception when the manufactured date is set in the future.
    /// </summary>
    [Fact]
    public void SetManufacturingDates_Should_Throw_If_ManufacturedDate_In_Future()
    {
        var batch = CreateBatch();
        var futureDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1));

        Action act = () => batch.SetManufacturingDates(futureDate, null);

        act.Should().Throw<ArgumentException>()
           .WithMessage("Manufactured date cannot be in the future.");
    }

    /// <summary>
    /// Tests that the <see cref="ProductBatch.SetManufacturingDates(DateOnly?, DateOnly?)"/> method throws an exception when the expiration date is set before the manufactured date.
    /// </summary>
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

    /// <summary>
    /// Tests that the <see cref="ProductBatch.IsExpired()"/> method returns true when the expiration date has passed.
    /// </summary>
    [Fact]
    public void IsExpired_Should_Return_True_When_ExpirationDate_Passed()
    {
        var expiredDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1));
        var batch = CreateBatch(expirationDate: expiredDate);

        batch.IsExpired().Should().BeTrue();
    }

    /// <summary>
    /// Tests that the <see cref="ProductBatch.IsExpired()"/> method returns false when the expiration date is in the future.
    /// </summary>
    [Fact]
    public void IsExpired_Should_Return_False_When_ExpirationDate_NotSet()
    {
        var batch = CreateBatch();

        batch.IsExpired().Should().BeFalse();
    }

    /// <summary>
    /// Tests that the <see cref="ProductBatch.ExpiresSoon(int)"/> method returns true when the expiration date is within the specified threshold.
    /// </summary>
    [Fact]
    public void ExpiresSoon_Should_Return_True_When_Within_Threshold()
    {
        var expiration = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(2));
        var batch = CreateBatch(expirationDate: expiration);

        batch.ExpiresSoon(3).Should().BeTrue();
    }

    /// <summary>
    /// Tests that the <see cref="ProductBatch.ExpiresSoon(int)"/> method returns false when the expiration date is beyond the specified threshold.
    /// </summary>
    [Fact]
    public void ExpiresSoon_Should_Return_False_When_Beyond_Threshold()
    {
        var expiration = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(10));
        var batch = CreateBatch(expirationDate: expiration);

        batch.ExpiresSoon(5).Should().BeFalse();
    }

    /// <summary>
    /// Tests that the <see cref="ProductBatch.ExpiresSoon(int)"/> method returns false when the expiration date is not set.
    /// </summary>
    [Fact]
    public void ExpiresSoon_Should_Return_False_When_ExpirationDate_NotSet()
    {
        var batch = CreateBatch();

        batch.ExpiresSoon(5).Should().BeFalse();
    }

    /// <summary>
    /// Creates a new instance of <see cref="ProductBatch"/> for testing purposes.
    /// </summary>
    /// <param name="productId">The ID of the product.</param>
    /// <param name="batchNumber">The batch number of the product batch.</param>
    /// <param name="manufacturedDate">The manufactured date of the product batch.</param>
    /// <param name="expirationDate">The expiration date of the product batch.</param>
    /// <returns>A new instance of <see cref="ProductBatch"/>.</returns>
    private ProductBatch CreateBatch(
        Guid? productId = null,
        string batchNumber = "BATCH",
        DateOnly? manufacturedDate = null,
        DateOnly? expirationDate = null)
    {
        return new(productId ?? Guid.NewGuid(), batchNumber, fixture.User, manufacturedDate, expirationDate);
    }
}
