using System;
using WarehouseManagementSystem.Domain.Enums;
using WarehouseManagementSystem.Domain.Model.CatalogDomain;
using Xunit;
using FluentAssertions;

namespace WarehouseManagementSystem.Tests.Domain.Model.CatalogDomain
{
    public class ProductTests
    {
        [Fact]
        public void Constructor_Should_Set_Properties_Correctly()
        {
            // Arrange
            var sku = "abc123";
            var name = "Test Product";
            var unit = UnitOfMeasure.Piece;
            var requiresBatch = true;
            decimal weight = 1.5m;
            decimal volume = 0.75m;
            var description = "  Some description  ";

            // Act
            var product = new Product(sku, name, unit, requiresBatch, weight, volume, description);

            // Assert
            product.Id.Should().NotBe(Guid.Empty);
            product.SKU.Should().Be("ABC123"); // uppercase
            product.Name.Should().Be("Test Product");
            product.Unit.Should().Be(unit);
            product.RequiresBatch.Should().BeTrue();
            product.IsActive.Should().BeTrue();
            product.Weight.Should().Be(weight);
            product.Volume.Should().Be(volume);
            product.Description.Should().Be("Some description"); // trimmed
            product.CreatedAt.Should().BeOnOrBefore(DateTimeOffset.UtcNow);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void SetSku_Should_Throw_On_Invalid_Value(string invalidSku)
        {
            // Arrange
            var product = new Product("SKU1", "Name", UnitOfMeasure.Piece, false);

            // Act
            Action act = () => product.SetSku(invalidSku);

            // Assert
            act.Should().Throw<ArgumentException>()
               .WithMessage("SKU cannot be empty.");
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void SetName_Should_Throw_On_Invalid_Value(string invalidName)
        {
            var product = new Product("SKU1", "Name", UnitOfMeasure.Piece, false);

            Action act = () => product.SetName(invalidName);

            act.Should().Throw<ArgumentException>()
               .WithMessage("Name is required.");
        }

        [Fact]
        public void SetWeight_Should_Throw_On_Negative_Value()
        {
            var product = new Product("SKU1", "Name", UnitOfMeasure.Piece, false);

            Action act = () => product.SetWeight(-1);

            act.Should().Throw<ArgumentException>()
               .WithMessage("Weight cannot be negative.");
        }

        [Fact]
        public void SetVolume_Should_Throw_On_Negative_Value()
        {
            var product = new Product("SKU1", "Name", UnitOfMeasure.Piece, false);

            Action act = () => product.SetVolume(-0.5m);

            act.Should().Throw<ArgumentException>()
               .WithMessage("Volume cannot be negative.");
        }

        [Fact]
        public void SetDescription_Should_Trim_And_Handle_Null()
        {
            var product = new Product("SKU1", "Name", UnitOfMeasure.Piece, false);

            product.SetDescription("  Test Desc  ");
            product.Description.Should().Be("Test Desc");

            product.SetDescription(null);
            product.Description.Should().BeNull();

            product.SetDescription("   ");
            product.Description.Should().BeNull();
        }

        [Fact]
        public void Activate_Should_Set_IsActive_True()
        {
            var product = new Product("SKU1", "Name", UnitOfMeasure.Piece, false);
            product.Deactivate();

            product.IsActive.Should().BeFalse();
            product.Activate();
            product.IsActive.Should().BeTrue();
        }

        [Fact]
        public void Deactivate_Should_Set_IsActive_False()
        {
            //Arrange
            var product = new Product("SKU1", "Name", UnitOfMeasure.Piece, false);

            // Act
            product.Deactivate();

            // Assert
            product.IsActive.Should().BeFalse();
        }

        [Fact]
        public void RequireBatchTracking_Should_Set_RequiresBatch_True()
        {
            var product = new Product("SKU1", "Name", UnitOfMeasure.Piece, false);

            product.RequiresBatch.Should().BeFalse();
            product.RequireBatchTracking();
            product.RequiresBatch.Should().BeTrue();
        }

        [Fact]
        public void DisableBatchTracking_Should_Set_RequiresBatch_False()
        {
            var product = new Product("SKU1", "Name", UnitOfMeasure.Piece, true);

            product.RequiresBatch.Should().BeTrue();
            product.DisableBatchTracking();
            product.RequiresBatch.Should().BeFalse();
        }
    }
}