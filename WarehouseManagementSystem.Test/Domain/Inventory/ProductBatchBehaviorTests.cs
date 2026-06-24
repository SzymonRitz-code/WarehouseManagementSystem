using FluentAssertions;
using WarehouseManagementSystem.Domain.Model.InventoryDomain;
using WarehouseManagementSystem.Tests.Support;

namespace WarehouseManagementSystem.Tests.Domain.InventoryDomain
{
    [Trait("Category", "Inventory_ProductBatch")]
    public class ProductBatchBehaviorTests(DomainTestFixture fixture) : IClassFixture<DomainTestFixture>
    {
        private readonly Guid _productId = Guid.NewGuid();

        [Fact]
        public void Constructor_ShouldInitializeBatchCorrectly()
        {
            // Arrange
            var batch = CreateBatch();

            // Act
            var isExpired = batch.IsExpired();

            // Assert
            batch.Id.Should().NotBeEmpty();
            batch.ProductId.Should().Be(_productId);
            batch.BatchNumber.Should().Be("BATCH01");
            batch.ManufacturedDate.Should().HaveValue();
            batch.ExpirationDate.Should().HaveValue();
            batch.ManufacturedDate.Value.Should().BeBefore(batch.ExpirationDate.Value);
            batch.CreatedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(1));
            isExpired.Should().BeFalse();
        }

        [Fact]
        public void SetBatchNumber_ShouldUpdateValue_WhenValid()
        {
            // Arrange
            var batch = CreateBatch();

            // Act
            batch.SetBatchNumber("NEWBATCH");

            // Assert
            batch.BatchNumber.Should().Be("NEWBATCH");
        }

        [Theory]
        [ClassData(typeof(InvalidRequiredStringTestData))]
        public void SetBatchNumber_ShouldThrow_WhenInvalid(string? value)
        {
            // Arrange
            var batch = CreateBatch();

            // Act
            Action act = () => batch.SetBatchNumber(value!);

            // Assert
            act.Should().Throw<ArgumentException>().WithMessage("*required*");
        }

        [Fact]
        public void SetManufacturingDates_ShouldUpdateDates_WhenValid()
        {
            // Arrange
            var batch = CreateBatch();
            var mDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-2));
            var eDate = DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(1));

            // Act
            batch.SetManufacturingDates(mDate, eDate);

            // Assert
            batch.ManufacturedDate.Should().Be(mDate);
            batch.ExpirationDate.Should().Be(eDate);
        }

        [Fact]
        public void SetManufacturingDates_ShouldThrow_WhenManufacturedInFuture()
        {
            // Arrange
            var batch = CreateBatch();
            var futureDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1));

            // Act
            Action act = () => batch.SetManufacturingDates(futureDate, null);

            // Assert
            act.Should().Throw<ArgumentException>().WithMessage("*future*");
        }

        [Fact]
        public void SetManufacturingDates_ShouldThrow_WhenExpirationBeforeManufactured()
        {
            // Arrange
            var batch = CreateBatch();
            var mDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1));
            var eDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-2));

            // Act
            Action act = () => batch.SetManufacturingDates(mDate, eDate);

            // Assert
            act.Should().Throw<ArgumentException>().WithMessage("*earlier than manufactured*");
        }

        [Fact]
        public void IsExpired_ShouldReturnTrue_WhenExpired()
        {
            // Arrange
            var batch = CreateBatch(
                expirationDate: DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1)));

            // Act
            var isExpired = batch.IsExpired();

            // Assert
            isExpired.Should().BeTrue();
        }

        [Fact]
        public void IsExpired_ShouldReturnFalse_WhenNotExpired()
        {
            // Arrange
            var batch = CreateBatch(
                expirationDate: DateOnly.FromDateTime(DateTime.UtcNow.AddDays(10)));

            // Act
            var isExpired = batch.IsExpired();

            // Assert
            isExpired.Should().BeFalse();
        }

        [Fact]
        public void ExpiresSoon_ShouldReturnTrue_WhenWithinThreshold()
        {
            // Arrange
            var batch = CreateBatch(
                expirationDate: DateOnly.FromDateTime(DateTime.UtcNow.AddDays(3)));

            // Act
            var expiresSoon = batch.ExpiresSoon(5);

            // Assert
            expiresSoon.Should().BeTrue();
        }

        [Fact]
        public void ExpiresSoon_ShouldReturnFalse_WhenOutsideThreshold()
        {
            // Arrange
            var batch = CreateBatch(
                expirationDate: DateOnly.FromDateTime(DateTime.UtcNow.AddDays(10)));

            // Act
            var expiresSoon = batch.ExpiresSoon(5);

            // Assert
            expiresSoon.Should().BeFalse();
        }

        private ProductBatch CreateBatch(
            string batchNumber = "BATCH01",
            DateOnly? manufacturedDate = null,
            DateOnly? expirationDate = null)
            => new(
                _productId,
                batchNumber,
                fixture.User,
                manufacturedDate ?? DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1)),
                expirationDate ?? DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(6)));
    }
}
