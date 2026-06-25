using FluentAssertions;
using WarehouseManagementSystem.Domain.Model.InventoryDomain;
using WarehouseManagementSystem.Tests.Support;

namespace WarehouseManagementSystem.Tests.Domain.InventoryDomain
{
    /// <summary>
    /// Tests for the <see cref="ProductBatch"/> class in the Inventory domain.
    /// </summary>
    /// <param name="fixture">The test fixture for domain tests.</param>
    [Trait("Category", "Inventory_ProductBatch")]
    public class ProductBatchBehaviorTests(DomainTestFixture fixture) : IClassFixture<DomainTestFixture>
    {
        private readonly Guid _productId = Guid.NewGuid();

        /// <summary>
        /// Tests that the constructor of <see cref="ProductBatch"/> initializes the object correctly with valid parameters.
        /// </summary>
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

        /// <summary>
        /// Tests that the <see cref="ProductBatch.SetBatchNumber"/> method updates the batch number correctly when provided with a valid value.
        /// </summary>
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

        /// <summary>
        /// Tests that the <see cref="ProductBatch.SetBatchNumber"/> method throws an exception when provided with an invalid value (null or empty).
        /// </summary>
        /// <param name="value">The invalid batch number value to test.</param>
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

        /// <summary>
        /// Tests that the <see cref="ProductBatch.SetManufacturingDates"/> method updates the manufacturing and expiration dates correctly when provided with valid values.
        /// </summary>
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

        /// <summary>
        /// Tests that the <see cref="ProductBatch.SetManufacturingDates"/> method throws an exception when the manufactured date is set in the future.
        /// </summary>
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

        /// <summary>
        /// Tests that the <see cref="ProductBatch.SetManufacturingDates"/> method throws an exception when the expiration date is set before the manufactured date.
        /// </summary>
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

        /// <summary>
        /// Tests that the <see cref="ProductBatch.IsExpired"/> method returns true when the batch is expired and false when it is not expired.
        /// </summary>
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

        /// <summary>
        /// Tests that the <see cref="ProductBatch.IsExpired"/> method returns false when the batch is not expired.
        /// </summary>
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

        /// <summary>
        /// Tests that the <see cref="ProductBatch.ExpiresSoon"/> method returns true when the batch is within the specified threshold of expiration and false when it is not.
        /// </summary>
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

        /// <summary>
        /// Tests that the <see cref="ProductBatch.ExpiresSoon"/> method returns false when the batch is outside the specified threshold of expiration.
        /// </summary>
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

        /// <summary>
        /// Creates a new instance of <see cref="ProductBatch"/> with the specified parameters or default values for testing purposes.
        /// </summary>
        /// <param name="batchNumber">The batch number of the product batch.</param>
        /// <param name="manufacturedDate">The manufactured date of the product batch.</param>
        /// <param name="expirationDate">The expiration date of the product batch.</param>
        /// <returns>A new instance of <see cref="ProductBatch"/>.</returns>
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
