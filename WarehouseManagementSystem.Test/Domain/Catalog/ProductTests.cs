using FluentAssertions;
using WarehouseManagementSystem.Domain.Enums;
using WarehouseManagementSystem.Domain.Model.CatalogDomain;
using WarehouseManagementSystem.Tests.Support;

namespace WarehouseManagementSystem.Tests.Domain.CatalogDomain;

/// <summary>
/// Tests for the <see cref="Product"/> class in the Catalog domain.
/// </summary>
/// <param name="fixture"></param>
[Trait("Category", "Catalog_Product")]
public class ProductTests(DomainTestFixture fixture) : IClassFixture<DomainTestFixture>
{
    /// <summary>
    /// Tests that the constructor of the <see cref="Product"/> class sets properties correctly.
    /// </summary>
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
    /// <summary>
    /// Tests that the SetSku method throws an ArgumentException when provided with an invalid SKU value.
    /// </summary>
    /// <param name="invalidSku">The invalid SKU value.</param>
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
    /// <summary>
    /// Tests that the SetName method throws an ArgumentException when provided with an invalid Name value.
    /// </summary>
    /// <param name="invalidName">The invalid Name value.</param>
    [Theory]
    [ClassData(typeof(InvalidRequiredStringTestData))]
    public void SetName_Should_Throw_On_Invalid_Value(string? invalidName)
    {
        var product = CreateProduct();

        Action act = () => product.SetName(invalidName!);

        act.Should().Throw<ArgumentException>()
           .WithMessage("Name is required.");
    }
    /// <summary>
    /// Tests that the SetWeight method throws an ArgumentException when provided with a negative weight value.
    /// </summary>
    [Fact]
    public void SetWeight_Should_Throw_On_Negative_Value()
    {
        var product = CreateProduct();

        Action act = () => product.SetWeight(-1);

        act.Should().Throw<ArgumentException>()
           .WithMessage("Weight cannot be negative.");
    }
    /// <summary>
    /// Tests that the SetVolume method throws an ArgumentException when provided with a negative volume value.
    /// </summary>
    [Fact]
    public void SetVolume_Should_Throw_On_Negative_Value()
    {
        var product = CreateProduct();

        Action act = () => product.SetVolume(-0.5m);

        act.Should().Throw<ArgumentException>()
           .WithMessage("Volume cannot be negative.");
    }
    /// <summary>
    /// Tests that the SetDescription method trims whitespace and handles null values correctly.
    /// </summary>
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
    /// <summary>
    /// Tests that the Activate method sets the IsActive property to true.
    /// </summary>
    [Fact]
    public void Activate_Should_Set_IsActive_True()
    {
        var product = CreateProduct();
        product.Deactivate();

        product.IsActive.Should().BeFalse();
        product.Activate();
        product.IsActive.Should().BeTrue();
    }
    /// <summary>
    /// Tests that the Deactivate method sets the IsActive property to false.
    /// </summary>
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
    /// <summary>
    /// Tests that the RequireBatchTracking method sets the RequiresBatch property to true.
    /// </summary>
    [Fact]
    public void RequireBatchTracking_Should_Set_RequiresBatch_True()
    {
        var product = CreateProduct();

        product.RequiresBatch.Should().BeFalse();
        product.RequireBatchTracking();
        product.RequiresBatch.Should().BeTrue();
    }
    /// <summary>
    /// Tests that the DisableBatchTracking method sets the RequiresBatch property to false.
    /// </summary>
    [Fact]
    public void DisableBatchTracking_Should_Set_RequiresBatch_False()
    {
        var product = CreateProduct(requiresBatch: true);

        product.RequiresBatch.Should().BeTrue();
        product.DisableBatchTracking();
        product.RequiresBatch.Should().BeFalse();
    }
    /// <summary>
    /// Creates a new instance of the <see cref="Product"/> class with the specified parameters.
    /// </summary>
    /// <param name="sku">The SKU of the product.</param>
    /// <param name="name">The name of the product.</param>
    /// <param name="unit">The unit of measure for the product.</param>
    /// <param name="requiresBatch">Indicates whether the product requires batch tracking.</param>
    /// <param name="weight">The weight of the product.</param>
    /// <param name="volume">The volume of the product.</param>
    /// <param name="description">The description of the product.</param>
    /// <returns>A new instance of the <see cref="Product"/> class.</returns>
    private Product CreateProduct(
        string sku = "SKU1",
        string name = "Name",
        UnitOfMeasure unit = UnitOfMeasure.Piece,
        bool requiresBatch = false,
        decimal weight = 1.5m,
        decimal volume = 0.75m,
        string? description = "Description")
    {
        return new(sku, name, unit, requiresBatch, fixture.User, weight, volume, description);
    }
}
