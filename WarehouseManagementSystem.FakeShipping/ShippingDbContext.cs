using Microsoft.EntityFrameworkCore;

namespace WarehouseManagementSystem.FakeShipping;

public sealed class ShippingDbContext(DbContextOptions<ShippingDbContext> options) : DbContext(options)
{
    public DbSet<FakeShipment> Shipments => Set<FakeShipment>();
    public DbSet<ProcessedMessage> ProcessedMessages => Set<ProcessedMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<FakeShipment>(entity =>
        {
            entity.ToTable("FakeShipments");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.DocumentNumber).HasMaxLength(50).IsRequired();
            entity.Property(x => x.Status).HasMaxLength(30).IsRequired();
            entity.HasIndex(x => x.MessageId).IsUnique();
        });
        modelBuilder.Entity<ProcessedMessage>(entity =>
        {
            entity.ToTable("ProcessedMessages");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Consumer).HasMaxLength(100).IsRequired();
            entity.Property(x => x.MessageType).HasMaxLength(200).IsRequired();
            entity.HasIndex(x => new { x.Consumer, x.MessageId }).IsUnique();
        });
    }
}

public sealed class FakeShipment
{
    public Guid Id { get; set; }
    public Guid MessageId { get; set; }
    public Guid DocumentId { get; set; }
    public string DocumentNumber { get; set; } = null!;
    public Guid? CorrelationId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public string Status { get; set; } = "Requested";
}

public sealed class ProcessedMessage
{
    public Guid Id { get; set; }
    public Guid MessageId { get; set; }
    public string Consumer { get; set; } = null!;
    public string MessageType { get; set; } = null!;
    public Guid? CorrelationId { get; set; }
    public DateTimeOffset ProcessedAt { get; set; }
}
