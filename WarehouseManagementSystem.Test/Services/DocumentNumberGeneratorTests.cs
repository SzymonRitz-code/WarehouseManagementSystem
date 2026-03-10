using Microsoft.EntityFrameworkCore;
using WarehouseManagementSystem.Domain.Enums;
using WarehouseManagementSystem.Domain.Model.Documents;
using WarehouseManagementSystem.Infrastructure.Persistence;
using WarehouseManagementSystem.Infrastructure.Services;
using Xunit;

namespace WarehouseManagementSystem.Tests.Infrastructure.Services;

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

    [Fact]
    public async Task GenerateAsync_ShouldCreateSequence_WhenSequenceDoesNotExist()
    {
        var context = CreateContext();
        var generator = CreateGenerator(context);

        var warehouseId = Guid.NewGuid();
        var date = new DateTimeOffset(new DateTime(2025, 3, 8));

        var number = await generator.GenerateAsync(DocumentType.PZ, warehouseId, date);

        Assert.Equal("PZ/2025/000001", number);

        var sequence = await context.DocumentSequences.SingleAsync();

        Assert.Equal(1, sequence.LastNumber);
        Assert.Equal(warehouseId, sequence.WarehouseId);
        Assert.Equal(2025, sequence.Year);
    }

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

        var date = new DateTimeOffset(new DateTime(2025, 3, 8));

        var number = await generator.GenerateAsync(DocumentType.PZ, warehouseId, date);

        Assert.Equal("PZ/2025/000006", number);

        var sequence = await context.DocumentSequences.SingleAsync();
        Assert.Equal(6, sequence.LastNumber);
    }

    [Fact]
    public async Task GenerateAsync_ShouldCreateSeparateSequences_ForDifferentWarehouses()
    {
        var context = CreateContext();
        var generator = CreateGenerator(context);

        var warehouse1 = Guid.NewGuid();
        var warehouse2 = Guid.NewGuid();

        var date = new DateTimeOffset(new DateTime(2025, 3, 8));

        var number1 = await generator.GenerateAsync(DocumentType.PZ, warehouse1, date);
        var number2 = await generator.GenerateAsync(DocumentType.PZ, warehouse2, date);

        Assert.Equal("PZ/2025/000001", number1);
        Assert.Equal("PZ/2025/000001", number2);

        var sequences = context.DocumentSequences.ToList();

        Assert.Equal(2, sequences.Count);
    }

    [Fact]
    public void FormatPreview_ShouldFormatWithoutWarehouseCode()
    {
        var context = CreateContext();
        var generator = CreateGenerator(context);

        var date = new DateTimeOffset(new DateTime(2025, 3, 8));

        var result = generator.FormatPreview(DocumentType.PZ, 42, date);

        Assert.Equal("PZ/2025/000042", result);
    }

    [Fact]
    public void FormatPreview_ShouldFormatWithWarehouseCode()
    {
        var context = CreateContext();
        var generator = CreateGenerator(context);

        var date = new DateTimeOffset(new DateTime(2025, 3, 8));

        var result = generator.FormatPreview(DocumentType.PZ, 42, date, "WH1");

        Assert.Equal("PZ/WH1/2025/000042", result);
    }

    private class FakeClock : WarehouseManagementSystem.Infrastructure.Services.ISystemClock
    {
        public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;

        public DateTime Now => DateTime.Now;
    }
}