using System;
using WarehouseManagementSystem.Domain.Enums;
using WarehouseManagementSystem.Domain.Model.DocumentsDomain;
using Xunit;
using FluentAssertions;

namespace WarehouseManagementSystem.Tests.Domain.Model.DocumentsDomain
{
    public class DocumentItemTests
    {
        [Fact]
        public void Constructor_Should_Set_Properties_Correctly()
        {
            // Arrange
            var productId = Guid.NewGuid();
            var quantity = 10m;
            var batchId = Guid.NewGuid();
            var sourceZoneId = Guid.NewGuid();
            var targetZoneId = Guid.NewGuid();

            // Act
            var item = new DocumentItem(productId, quantity, batchId, sourceZoneId, targetZoneId);

            // Assert
            item.Id.Should().NotBe(Guid.Empty);
            item.ProductId.Should().Be(productId);
            item.Quantity.Should().Be(quantity);
            item.ProductBatchId.Should().Be(batchId);
            item.SourceZoneId.Should().Be(sourceZoneId);
            item.TargetZoneId.Should().Be(targetZoneId);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public void SetQuantity_Should_Throw_On_NonPositive(decimal invalidQuantity)
        {
            var item = new DocumentItem(Guid.NewGuid(), 1);

            Action act = () => item.SetQuantity(invalidQuantity);

            act.Should().Throw<ArgumentException>()
               .WithMessage("Quantity must be greater than zero.");
        }

        [Fact]
        public void IncreaseQuantity_Should_Work_Correctly()
        {
            var item = new DocumentItem(Guid.NewGuid(), 5);

            item.IncreaseQuantity(3);

            item.Quantity.Should().Be(8);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public void IncreaseQuantity_Should_Throw_On_NonPositive(decimal invalidValue)
        {
            var item = new DocumentItem(Guid.NewGuid(), 5);

            Action act = () => item.IncreaseQuantity(invalidValue);

            act.Should().Throw<ArgumentException>()
               .WithMessage("Increase value must be greater than zero.");
        }

        [Fact]
        public void DecreaseQuantity_Should_Work_Correctly()
        {
            var item = new DocumentItem(Guid.NewGuid(), 5);

            item.DecreaseQuantity(3);

            item.Quantity.Should().Be(2);
        }

        [Fact]
        public void DecreaseQuantity_Should_Throw_When_Result_ZeroOrNegative()
        {
            var item = new DocumentItem(Guid.NewGuid(), 5);

            Action act = () => item.DecreaseQuantity(5);

            act.Should().Throw<InvalidOperationException>()
               .WithMessage("Quantity cannot be zero or negative.");
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public void DecreaseQuantity_Should_Throw_On_NonPositive(decimal invalidValue)
        {
            var item = new DocumentItem(Guid.NewGuid(), 5);

            Action act = () => item.DecreaseQuantity(invalidValue);

            act.Should().Throw<ArgumentException>()
               .WithMessage("Decrease value must be greater than zero.");
        }

        [Fact]
        public void SetProduct_Should_Throw_On_EmptyGuid()
        {
            var item = new DocumentItem(Guid.NewGuid(), 5);

            Action act = () => item.SetProduct(Guid.Empty);

            act.Should().Throw<ArgumentException>()
               .WithMessage("ProductId cannot be empty.");
        }

        [Fact]
        public void AssignBatch_Should_Set_ProductBatchId()
        {
            var item = new DocumentItem(Guid.NewGuid(), 5);
            var batchId = Guid.NewGuid();

            item.AssignBatch(batchId);

            item.ProductBatchId.Should().Be(batchId);
        }

        [Fact]
        public void SetSourceZone_Should_Set_SourceZoneId()
        {
            var item = new DocumentItem(Guid.NewGuid(), 5);
            var zoneId = Guid.NewGuid();

            item.SetSourceZone(zoneId);

            item.SourceZoneId.Should().Be(zoneId);
        }

        [Fact]
        public void SetTargetZone_Should_Set_TargetZoneId()
        {
            var item = new DocumentItem(Guid.NewGuid(), 5);
            var zoneId = Guid.NewGuid();

            item.SetTargetZone(zoneId);

            item.TargetZoneId.Should().Be(zoneId);
        }

        [Fact]
        public void ValidateForDocumentType_Should_Throw_When_TargetZone_Missing_For_PZ()
        {
            var item = new DocumentItem(Guid.NewGuid(), 5);
            item.SetSourceZone(Guid.NewGuid());

            Action act = () => item.ValidateForDocumentType(DocumentType.PZ);

            act.Should().Throw<InvalidOperationException>()
               .WithMessage("PZ requires target zone.");
        }

        [Fact]
        public void ValidateForDocumentType_Should_Throw_When_SourceZone_Missing_For_WZ()
        {
            var item = new DocumentItem(Guid.NewGuid(), 5);
            item.SetTargetZone(Guid.NewGuid());

            Action act = () => item.ValidateForDocumentType(DocumentType.WZ);

            act.Should().Throw<InvalidOperationException>()
               .WithMessage("WZ requires source zone.");
        }

        [Fact]
        public void ValidateForDocumentType_Should_Throw_When_SourceOrTarget_Missing_For_MM()
        {
            var item = new DocumentItem(Guid.NewGuid(), 5);
            item.SetSourceZone(Guid.NewGuid()); // brak target

            Action act1 = () => item.ValidateForDocumentType(DocumentType.MM);
            act1.Should().Throw<InvalidOperationException>()
                .WithMessage("MM requires both source and target zones.");

            var item2 = new DocumentItem(Guid.NewGuid(), 5);
            item2.SetTargetZone(Guid.NewGuid()); // brak source

            Action act2 = () => item2.ValidateForDocumentType(DocumentType.MM);
            act2.Should().Throw<InvalidOperationException>()
                .WithMessage("MM requires both source and target zones.");
        }

        [Fact]
        public void ValidateForDocumentType_Should_Not_Throw_For_ADJ()
        {
            var item = new DocumentItem(Guid.NewGuid(), 5);

            Action act = () => item.ValidateForDocumentType(DocumentType.ADJ);

            act.Should().NotThrow();
        }
    }
}