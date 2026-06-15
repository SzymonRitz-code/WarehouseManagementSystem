using Microsoft.EntityFrameworkCore;
using WarehouseManagementSystem.Domain.Enums;
using WarehouseManagementSystem.Domain.Model.Documents;
using WarehouseManagementSystem.Domain.Services;
using WarehouseManagementSystem.Infrastructure.Persistence;

namespace WarehouseManagementSystem.Infrastructure.Services;

public class DocumentNumberGenerator : IDocumentNumberGenerator
{
    private readonly WarehouseManagementSystemDbContext _context;
    private readonly ISystemClock clock;

    public DocumentNumberGenerator(WarehouseManagementSystemDbContext context, ISystemClock clock)
    {
        _context = context;
        this.clock = clock;
    }

    public async Task<string> GenerateAsync(
        DocumentType type,
        Guid? warehouseId,
        DateTimeOffset documentDate)
    {
        var year = documentDate.Year;

        // This generator intentionally does not call SaveChangesAsync. Document numbers are allocated
        // inside DocumentCommandService.ConfirmDocumentAsync, which opens a Serializable transaction.
        // That isolation level makes concurrent confirmations for the same type/year/warehouse wait
        // on the same sequence row or key range before incrementing LastNumber. Without that outer
        // transaction two requests could read the same LastNumber, generate duplicate numbers, or
        // commit a sequence increment even though the document confirmation later failed.
        var sequence = _context.DocumentSequences.Local
            .SingleOrDefault(x =>
                x.Type == type &&
                x.Year == year &&
                x.WarehouseId == warehouseId);

        sequence ??= await _context.DocumentSequences
            .SingleOrDefaultAsync(x =>
                x.Type == type &&
                x.Year == year &&
                x.WarehouseId == warehouseId);

        if (sequence == null)
        {
            sequence = new DocumentSequence
            {
                Id = Guid.NewGuid(),
                Type = type,
                Year = year,
                WarehouseId = warehouseId,
                LastNumber = 0
            };

            _context.Add(sequence);
        }

        sequence.LastNumber++;

        return FormatPreview(type, sequence.LastNumber, documentDate);
    }

    public string FormatPreview(
        DocumentType type,
        int sequence,
        DateTimeOffset documentDate,
        string? warehouseCode = null)
    {
        var year = documentDate.Year;

        var prefix = warehouseCode == null
            ? $"{type}"
            : $"{type}/{warehouseCode}";

        return $"{prefix}/{year}/{sequence:D6}";
    }
}
