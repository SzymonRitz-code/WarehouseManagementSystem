using FluentAssertions;
using WarehouseManagementSystem.Domain.Enums;
using WarehouseManagementSystem.Domain.Model.CatalogDomain;
using WarehouseManagementSystem.Tests.Support;

namespace WarehouseManagementSystem.Tests.Domain.CatalogDomain;

[Trait("Category", "Catalog_Product")]
public class ProductTests(DomainTestFixture fixture) : IClassFixture<DomainTestFixture>
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
        var product = CreateProduct(sku, name, unit, requiresBatch, weight, volume, description);

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
    [ClassData(typeof(InvalidRequiredStringTestData))]
    public void SetSku_Should_Throw_On_Invalid_Value(string? invalidSku)
    {
        // Arrange
        var product = CreateProduct();

        // Act
        Action act = () => product.SetSku(invalidSku!);

        // Assert
        act.Should().Throw<ArgumentException>()
           .WithMessage("SKU cannot be empty.");
    }

    [Theory]
    [ClassData(typeof(InvalidRequiredStringTestData))]
    public void SetName_Should_Throw_On_Invalid_Value(string? invalidName)
    {
        var product = CreateProduct();

        Action act = () => product.SetName(invalidName!);

        act.Should().Throw<ArgumentException>()
           .WithMessage("Name is required.");
    }

    [Fact]
    public void SetWeight_Should_Throw_On_Negative_Value()
    {
        var product = CreateProduct();

        Action act = () => product.SetWeight(-1);

        act.Should().Throw<ArgumentException>()
           .WithMessage("Weight cannot be negative.");
    }

    [Fact]
    public void SetVolume_Should_Throw_On_Negative_Value()
    {
        var product = CreateProduct();

        Action act = () => product.SetVolume(-0.5m);

        act.Should().Throw<ArgumentException>()
           .WithMessage("Volume cannot be negative.");
    }

    [Fact]
    public void SetDescription_Should_Trim_And_Handle_Null()
    {
        var product = CreateProduct();

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
        var product = CreateProduct();
        product.Deactivate();

        product.IsActive.Should().BeFalse();
        product.Activate();
        product.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Deactivate_Should_Set_IsActive_False()
    {
        //Arrange
        var product = CreateProduct();

        // Act
        product.Deactivate();

        // Assert
        product.IsActive.Should().BeFalse();
    }

    [Fact]
    public void RequireBatchTracking_Should_Set_RequiresBatch_True()
    {
        var product = CreateProduct();

        product.RequiresBatch.Should().BeFalse();
        product.RequireBatchTracking();
        product.RequiresBatch.Should().BeTrue();
    }

    [Fact]
    public void DisableBatchTracking_Should_Set_RequiresBatch_False()
    {
        var product = CreateProduct(requiresBatch: true);

        product.RequiresBatch.Should().BeTrue();
        product.DisableBatchTracking();
        product.RequiresBatch.Should().BeFalse();
    }

    private Product CreateProduct(
        string sku = "SKU1",
        string name = "Name",
        UnitOfMeasure unit = UnitOfMeasure.Piece,
        bool requiresBatch = false,
        decimal weight = 1.5m,
        decimal volume = 0.75m,
        string? description = "Description")
        => new(sku, name, unit, requiresBatch, fixture.User, weight, volume, description);
}
