using FluentAssertions;
using WarehouseManagementSystem.Domain.Enums;
using WarehouseManagementSystem.Domain.Model.Documents;

namespace WarehouseManagementSystem.Tests.Domain.DocumentsDomain;

/// <summary>
/// Tests for the <see cref="DocumentSequence"/> class in the Documents domain.
/// </summary>
public class DocumentSequenceTests
{
    /// <summary>
    /// Tests that the properties of the DocumentSequence class can be set and retrieved correctly.
    /// </summary>
    [Fact]
    public void Should_Set_All_Properties_Correctly()
    {
        // Arrange
        var id = Guid.NewGuid();
        var type = DocumentType.PZ;
        var year = 2026;
        var warehouseId = Guid.NewGuid();
        var lastNumber = 123;

        // Act
        var sequence = new DocumentSequence
        {
            Id = id,
            Type = type,
            Year = year,
            WarehouseId = warehouseId,
            LastNumber = lastNumber
        };

        // Assert
        sequence.Id.Should().Be(id);
        sequence.Type.Should().Be(type);
        sequence.Year.Should().Be(year);
        sequence.WarehouseId.Should().Be(warehouseId);
        sequence.LastNumber.Should().Be(lastNumber);
    }
    /// <summary>
    /// Tests that the DocumentSequence class can accept a null value for the WarehouseId property.
    /// </summary>
    [Fact]
    public void Should_Accept_Null_WarehouseId()
    {
        // Arrange
        var sequence = new DocumentSequence
        {
            Id = Guid.NewGuid(),
            Type = DocumentType.WZ,
            Year = 2026,
            WarehouseId = null,
            LastNumber = 0
        };

        // Act & Assert
        sequence.WarehouseId.Should().BeNull();
        sequence.LastNumber.Should().Be(0);
    }
}