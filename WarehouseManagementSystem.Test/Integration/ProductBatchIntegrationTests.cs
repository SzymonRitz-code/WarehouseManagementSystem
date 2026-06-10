using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Moq;
using WarehouseManagementSystem.API.Services.User;
using WarehouseManagementSystem.Domain.Enums;
using WarehouseManagementSystem.Domain.Model.CatalogDomain;
using WarehouseManagementSystem.Domain.Model.InventoryDomain;
using WarehouseManagementSystem.Domain.ValueObjects;

namespace WarehouseManagementSystem.Tests.Integration.InventoryDomain
{
    public class ProductBatchIntegrationTests
    {
        private readonly Guid _productId = Guid.NewGuid();
        private readonly Mock<IUserService> _userServiceMock = new Mock<IUserService>();

        public ProductBatchIntegrationTests()
        {
            _userServiceMock.Setup(s => s.GetUser(It.IsAny<HttpContext>()))
                .Returns(new UserSnapshot(Guid.Parse("11111111-1111-1111-1111-111111111111"), "Testomir.Testowski@gmail.com", "Testomir"));
        }

        private Product CreateProduct()
        {
            return new Product(
                sku: "PRD001",
                name: "Test Product",
                unit: UnitOfMeasure.Piece,
                requiresBatch: true,
                createdByUser: _userServiceMock.Object.GetUser(new DefaultHttpContext())
                );
        }

        private ProductBatch CreateBatch(
            string batchNumber = "BATCH01",
            DateOnly? manufacturedDate = null,
            DateOnly? expirationDate = null)
        {
            var product = CreateProduct();

            return new ProductBatch(
                _productId,
                batchNumber,
                _userServiceMock.Object.GetUser(new DefaultHttpContext()),
                manufacturedDate ?? DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1)),
                expirationDate ?? DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(6))
            );
        }

        [Fact]
        public void Constructor_ShouldInitializeBatchCorrectly()
        {
            var batch = CreateBatch();

            batch.Id.Should().NotBeEmpty();
            batch.ProductId.Should().Be(_productId);
            batch.BatchNumber.Should().Be("BATCH01");

            batch.ManufacturedDate.Should().HaveValue();
            batch.ExpirationDate.Should().HaveValue();
            batch.ManufacturedDate.Value.Should().BeBefore(batch.ExpirationDate.Value);

            batch.CreatedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(1));
        }

        [Fact]
        public void SetBatchNumber_ShouldUpdateValue_WhenValid()
        {
            var batch = CreateBatch();
            batch.SetBatchNumber("NEWBATCH");
            batch.BatchNumber.Should().Be("NEWBATCH");
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData(" ")]
        public void SetBatchNumber_ShouldThrow_WhenInvalid(string value)
        {
            var batch = CreateBatch();
            Action act = () => batch.SetBatchNumber(value);
            act.Should().Throw<ArgumentException>().WithMessage("*required*");
        }

        [Fact]
        public void SetManufacturingDates_ShouldUpdateDates_WhenValid()
        {
            var batch = CreateBatch();
            var mDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-2));
            var eDate = DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(1));

            batch.SetManufacturingDates(mDate, eDate);

            batch.ManufacturedDate.Should().Be(mDate);
            batch.ExpirationDate.Should().Be(eDate);
        }

        [Fact]
        public void SetManufacturingDates_ShouldThrow_WhenManufacturedInFuture()
        {
            var batch = CreateBatch();
            var futureDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1));

            Action act = () => batch.SetManufacturingDates(futureDate, null);
            act.Should().Throw<ArgumentException>().WithMessage("*future*");
        }

        [Fact]
        public void SetManufacturingDates_ShouldThrow_WhenExpirationBeforeManufactured()
        {
            var batch = CreateBatch();
            var mDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1));
            var eDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-2));

            Action act = () => batch.SetManufacturingDates(mDate, eDate);
            act.Should().Throw<ArgumentException>().WithMessage("*earlier than manufactured*");
        }

        [Fact]
        public void IsExpired_ShouldReturnTrue_WhenExpired()
        {
            var batch = CreateBatch(
                expirationDate: DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1)));

            batch.IsExpired().Should().BeTrue();
        }

        [Fact]
        public void IsExpired_ShouldReturnFalse_WhenNotExpired()
        {
            var batch = CreateBatch(
                expirationDate: DateOnly.FromDateTime(DateTime.UtcNow.AddDays(10)));

            batch.IsExpired().Should().BeFalse();
        }

        [Fact]
        public void ExpiresSoon_ShouldReturnTrue_WhenWithinThreshold()
        {
            var batch = CreateBatch(
                expirationDate: DateOnly.FromDateTime(DateTime.UtcNow.AddDays(3)));

            batch.ExpiresSoon(5).Should().BeTrue();
        }

        [Fact]
        public void ExpiresSoon_ShouldReturnFalse_WhenOutsideThreshold()
        {
            var batch = CreateBatch(
                expirationDate: DateOnly.FromDateTime(DateTime.UtcNow.AddDays(10)));

            batch.ExpiresSoon(5).Should().BeFalse();
        }
    }
}