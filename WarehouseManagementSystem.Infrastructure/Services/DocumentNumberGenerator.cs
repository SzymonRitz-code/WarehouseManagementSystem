using Microsoft.EntityFrameworkCore;
using WarehouseManagementSystem.Domain.Enums;
using WarehouseManagementSystem.Domain.Model.Documents;
using WarehouseManagementSystem.Domain.Services;
using WarehouseManagementSystem.Infrastructure.Persistence;

namespace WarehouseManagementSystem.Infrastructure.Services
{
    public class DocumentNumberGenerator : IDocumentNumberGenerator
    {
        private readonly WarehouseManagementSystemDbContext _context;
        private readonly ISystemClock clock;

        public DocumentNumberGenerator(WarehouseManagementSystemDbContext context, ISystemClock clock)
        {
            _context = context;
            this.clock = clock;
        }

        public async Task<string> GenerateAsync(DocumentType type)
            => await GenerateAsync(type, null, clock.UtcNow);

        public async Task<string> GenerateAsync(
            DocumentType type,
            Guid? warehouseId,
            DateTimeOffset documentDate)
        {
            var year = documentDate.Year;

            var sequence = await _context.DocumentSequences
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

            await _context.SaveChangesAsync();

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
}
