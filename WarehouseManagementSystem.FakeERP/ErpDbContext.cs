using Microsoft.EntityFrameworkCore;

namespace WarehouseManagementSystem.FakeERP;

public sealed class ErpDbContext(DbContextOptions<ErpDbContext> options) : DbContext(options)
{
    public DbSet<ErpWarehouseOrder> WarehouseOrders => Set<ErpWarehouseOrder>();
    public DbSet<ErpOutboxMessage> OutboxMessages => Set<ErpOutboxMessage>();
    public DbSet<ErpProcessedMessage> ProcessedMessages => Set<ErpProcessedMessage>();
    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<ErpWarehouseOrder>(e => { e.HasKey(x => x.Id); e.Property(x => x.ExternalOrderId).HasMaxLength(100).IsRequired(); e.Property(x => x.Status).HasMaxLength(30).IsRequired(); e.HasIndex(x => x.ExternalOrderId).IsUnique(); });
        b.Entity<ErpOutboxMessage>(e => { e.HasKey(x => x.Id); e.Property(x => x.Payload).IsRequired(); e.Property(x => x.Status).HasMaxLength(30).IsRequired(); e.HasIndex(x => x.MessageId).IsUnique(); });
        b.Entity<ErpProcessedMessage>(e => { e.HasKey(x => x.Id); e.Property(x => x.Consumer).HasMaxLength(100).IsRequired(); e.HasIndex(x => new { x.Consumer, x.MessageId }).IsUnique(); });
    }
}
public sealed class ErpWarehouseOrder { public Guid Id { get; set; } public string ExternalOrderId { get; set; } = null!; public Guid CorrelationId { get; set; } public string Status { get; set; } = "Pending"; public Guid? WmsDocumentId { get; set; } public DateTimeOffset CreatedAt { get; set; } public DateTimeOffset? ConfirmedAt { get; set; } }
public sealed class ErpOutboxMessage { public Guid Id { get; set; } public Guid MessageId { get; set; } public Guid CorrelationId { get; set; } public string Payload { get; set; } = null!; public DateTimeOffset OccurredAt { get; set; } public string Status { get; set; } = "Pending"; public int RetryCount { get; set; } public DateTimeOffset? NextAttemptAt { get; set; } public string? LastError { get; set; } public DateTimeOffset? PublishedAt { get; set; } }
public sealed class ErpProcessedMessage { public Guid Id { get; set; } public Guid MessageId { get; set; } public string Consumer { get; set; } = null!; public Guid CorrelationId { get; set; } public DateTimeOffset ProcessedAt { get; set; } }
