using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using WarehouseManagementSystem.API.Integration.Configuration;
using WarehouseManagementSystem.Infrastructure.Integration;
using WarehouseManagementSystem.Infrastructure.Persistence;

namespace WarehouseManagementSystem.API.Integration;

/// <summary>Repairs missing local shipping projections from confirmed documents without replaying external side effects.</summary>
public sealed class ShippingProjectionReconciler(IServiceScopeFactory scopes, IOptions<MessagingOptions> options,
    ILogger<ShippingProjectionReconciler> logger) : BackgroundService
{
    /// <summary>Checks the optional local projection periodically; external FakeShipping owns its own database.</summary>
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        if (!options.Value.Enabled || !options.Value.LocalShippingProjectionEnabled) return;
        while (!ct.IsCancellationRequested)
        {
            try { await ReconcileAsync(ct); }
            catch (OperationCanceledException) when (ct.IsCancellationRequested) { return; }
            catch (Exception ex) { logger.LogError(ex, "Shipping projection reconciliation failed."); }
            try { await Task.Delay(TimeSpan.FromMinutes(5), ct); }
            catch (OperationCanceledException) when (ct.IsCancellationRequested) { return; }
        }
    }

    /// <summary>
    /// Rebuilds at most 100 missing rows from the authoritative document snapshot. Existing rows and
    /// external shipments are untouched. Unique document keys arbitrate concurrent repair and delivery.
    /// </summary>
    public async Task<int> ReconcileAsync(CancellationToken ct)
    {
        using var scope = scopes.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<WarehouseManagementSystemDbContext>();
        var documents = await db.Documents.AsNoTracking()
            .Where(d => d.ConfirmedAt != null && d.SourceWarehouseId != null &&
                !db.ShippingShipments.Any(s => s.DocumentId == d.Id))
            .OrderBy(d => d.ConfirmedAt).Take(100).ToListAsync(ct);
        var repaired = 0;
        foreach (var document in documents)
        {
            var shipment = new ShippingShipment
            {
                Id = Guid.NewGuid(), DocumentId = document.Id, DocumentNumber = document.Number!,
                DocumentType = document.Type.ToString(), SourceWarehouseId = document.SourceWarehouseId!.Value,
                TargetWarehouseId = document.TargetWarehouseId, MessageId = Guid.NewGuid(),
                CorrelationId = document.Id, RequestedAt = document.ConfirmedAt!.Value,
                CreatedAt = DateTimeOffset.UtcNow, Status = "Requested"
            };
            db.ShippingShipments.Add(shipment);
            try { await db.SaveChangesAsync(ct); repaired++; }
            catch (DbUpdateException)
            {
                db.Entry(shipment).State = EntityState.Detached;
                if (!await db.ShippingShipments.AnyAsync(s => s.DocumentId == document.Id, ct)) throw;
            }
        }
        if (repaired > 0) logger.LogWarning("Rebuilt {Count} missing local shipping projections.", repaired);
        return repaired;
    }
}
