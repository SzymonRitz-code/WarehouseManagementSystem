using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using WarehouseManagementSystem.Domain.Enums;
using WarehouseManagementSystem.Domain.Model.Documents;
using WarehouseManagementSystem.Infrastructure.Persistence;
using WarehouseManagementSystem.Infrastructure.Services;

namespace WarehouseManagementSystem.Tests.Services;

public class DocumentNumberGeneratorTests
{
    private WarehouseManagementSystemDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<WarehouseManagementSystemDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new WarehouseManagementSystemDbContext(options);
    }

    private DocumentNumberGenerator CreateGenerator(WarehouseManagementSystemDbContext context)
    {
        var clock = new FakeClock();
        return new DocumentNumberGenerator(context, clock);
    }

    /// <summary>
    /// Verifies that GenerateAsync creates a new document sequence when it does not exist.
    /// </summary>
    [Fact]
    public async Task GenerateAsync_ShouldCreateSequence_WhenSequenceDoesNotExist()
    {
        var context = CreateContext();
        var generator = CreateGenerator(context);

        var warehouseId = Guid.NewGuid();
        var date = new DateTimeOffset(2025, 3, 8, 0, 0, 0, TimeSpan.Zero);

        var number = await generator.GenerateAsync(DocumentType.PZ, warehouseId, date);
        await context.SaveChangesAsync();

        number.Should().Be("PZ/2025/000001");

        var sequence = await context.DocumentSequences.SingleAsync();

        sequence.LastNumber.Should().Be(1);
        sequence.WarehouseId.Should().Be(warehouseId);
        sequence.Year.Should().Be(date.Year);
    }

    /// <summary>
    /// Verifies that GenerateAsync increments existing document sequence number correctly.
    /// </summary>
    [Fact]
    public async Task GenerateAsync_ShouldIncrementSequence_WhenSequenceExists()
    {
        var context = CreateContext();
        var warehouseId = Guid.NewGuid();

        context.DocumentSequences.Add(new DocumentSequence
        {
            Id = Guid.NewGuid(),
            Type = DocumentType.PZ,
            Year = 2025,
            WarehouseId = warehouseId,
            LastNumber = 5
        });

        await context.SaveChangesAsync();

        var generator = CreateGenerator(context);
        var date = new DateTimeOffset(2025, 3, 8, 0, 0, 0, TimeSpan.Zero);

        var number = await generator.GenerateAsync(DocumentType.PZ, warehouseId, date);
        await context.SaveChangesAsync();

        number.Should().Be("PZ/2025/000006");

        var sequence = await context.DocumentSequences.SingleAsync();
        sequence.LastNumber.Should().Be(6);
    }

    /// <summary>
    /// Verifies that GenerateAsync creates separate sequences for different warehouses with independent numbering.
    /// </summary>
    [Fact]
    public async Task GenerateAsync_ShouldCreateSeparateSequences_ForDifferentWarehouses()
    {
        var context = CreateContext();
        var generator = CreateGenerator(context);

        var warehouse1 = Guid.NewGuid();
        var warehouse2 = Guid.NewGuid();
        var date = new DateTimeOffset(2025, 3, 8, 0, 0, 0, TimeSpan.Zero);

        var number1 = await generator.GenerateAsync(DocumentType.PZ, warehouse1, date);
        var number2 = await generator.GenerateAsync(DocumentType.PZ, warehouse2, date);
        await context.SaveChangesAsync();

        number1.Should().Be("PZ/2025/000001");
        number2.Should().Be("PZ/2025/000001");

        var sequences = context.DocumentSequences.ToList();
        sequences.Count.Should().Be(2);
    }

    /// <summary>
    /// Verifies that GenerateAsync creates a new sequence when year changes, resetting numbering to 1.
    /// </summary>
    [Fact]
    public async Task GenerateAsync_ShouldCreateNewSequence_ForNewYear()
    {
        var context = CreateContext();
        var warehouseId = Guid.NewGuid();

        context.DocumentSequences.Add(new DocumentSequence
        {
            Id = Guid.NewGuid(),
            Type = DocumentType.PZ,
            Year = 2025,
            WarehouseId = warehouseId,
            LastNumber = 10
        });

        await context.SaveChangesAsync();

        var generator = CreateGenerator(context);
        var date = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

        var number = await generator.GenerateAsync(DocumentType.PZ, warehouseId, date);
        await context.SaveChangesAsync();

        number.Should().Be("PZ/2026/000001");

        var sequences = context.DocumentSequences.ToList();
        sequences.Count.Should().Be(2);
    }

    /// <summary>
    /// Verifies that GenerateAsync uses a global sequence when warehouse ID is null.
    /// </summary>
    [Fact]
    public async Task GenerateAsync_ShouldUseGlobalSequence_WhenWarehouseIsNull()
    {
        var context = CreateContext();
        var generator = CreateGenerator(context);

        var date = new DateTimeOffset(2025, 3, 8, 0, 0, 0, TimeSpan.Zero);

        var number1 = await generator.GenerateAsync(DocumentType.PZ, null, date);
        var number2 = await generator.GenerateAsync(DocumentType.PZ, null, date);

        number1.Should().Be("PZ/2025/000001");
        number2.Should().Be("PZ/2025/000002");
    }

    /// <summary>
    /// Verifies that FormatPreview formats document number without warehouse code.
    /// </summary>
    [Fact]
    public void FormatPreview_ShouldFormatWithoutWarehouseCode()
    {
        var context = CreateContext();
        var generator = CreateGenerator(context);

        var date = new DateTimeOffset(2025, 3, 8, 0, 0, 0, TimeSpan.Zero);

        var result = generator.FormatPreview(DocumentType.PZ, 42, date);

        result.Should().Be("PZ/2025/000042");
    }

    /// <summary>
    /// Verifies that FormatPreview formats document number with warehouse code.
    /// </summary>
    [Fact]
    public void FormatPreview_ShouldFormatWithWarehouseCode()
    {
        var context = CreateContext();
        var generator = CreateGenerator(context);

        var date = new DateTimeOffset(2025, 3, 8, 0, 0, 0, TimeSpan.Zero);

        var result = generator.FormatPreview(DocumentType.PZ, 42, date, "WH1");

        result.Should().Be("PZ/WH1/2025/000042");
    }

    private class FakeClock : ISystemClock
    {
        public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
        public DateTime Now => DateTime.Now;
    }
}
