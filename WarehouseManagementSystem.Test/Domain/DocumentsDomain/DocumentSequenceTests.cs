using FluentAssertions;
using System;
using WarehouseManagementSystem.Domain.Enums;
using WarehouseManagementSystem.Domain.Model.Documents;
using WarehouseManagementSystem.Domain.Model.DocumentsDomain;
using Xunit;

namespace WarehouseManagementSystem.Tests.Domain.DocumentsDomain;

public class DocumentSequenceTests
{
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