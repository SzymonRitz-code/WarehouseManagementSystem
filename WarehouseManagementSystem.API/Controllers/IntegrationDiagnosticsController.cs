using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WarehouseManagementSystem.Infrastructure.Persistence;

namespace WarehouseManagementSystem.API.Controllers;

[ApiController]
[Route("api/integration/outbox")]
public sealed class IntegrationDiagnosticsController(WarehouseManagementSystemDbContext db) : ControllerBase
{
    /// <summary>Small operational view of outbox state; it deliberately is not a dashboard.</summary>
    [HttpGet]
    public async Task<ActionResult<object>> Get(CancellationToken ct)
    {
        var messages = await db.OutboxMessages.AsNoTracking()
            .OrderByDescending(x => x.OccurredAt).Take(100)
            .Select(x => new { x.MessageId, x.CorrelationId, x.Type, x.RoutingKey, x.Status, x.RetryCount, x.OccurredAt, x.PublishedAt, x.LastError })
            .ToListAsync(ct);
        var counts = messages.GroupBy(x => x.Status).ToDictionary(x => x.Key, x => x.Count());
        return Ok(new { counts, messages });
    }
}
