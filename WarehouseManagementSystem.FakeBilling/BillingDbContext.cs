using Microsoft.EntityFrameworkCore;

namespace WarehouseManagementSystem.FakeBilling;

public sealed class BillingDbContext(DbContextOptions<BillingDbContext> options) : DbContext(options)
{
    public DbSet<FakeInvoice> FakeInvoices => Set<FakeInvoice>();
    public DbSet<ProcessedMessage> ProcessedMessages => Set<ProcessedMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<FakeInvoice>(entity =>
        {
            entity.ToTable("FakeInvoices");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.SourceDocumentNumber).HasMaxLength(50).IsRequired();
            entity.Property(x => x.Status).HasMaxLength(30).IsRequired();
            entity.Property(x => x.InvoiceNumber).HasMaxLength(50);
            entity.HasIndex(x => x.SourceDocumentId).IsUnique();
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

public sealed class FakeInvoice
{
    public Guid Id { get; set; }
    public Guid SourceDocumentId { get; set; }
    public string SourceDocumentNumber { get; set; } = null!;
    public Guid SourceMessageId { get; set; }
    public Guid CorrelationId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public string Status { get; set; } = "Created";
    public string? InvoiceNumber { get; set; }
}

public sealed class ProcessedMessage
{
    public Guid Id { get; set; }
    public Guid MessageId { get; set; }
    public string Consumer { get; set; } = null!;
    public string MessageType { get; set; } = null!;
    public Guid CorrelationId { get; set; }
    public DateTimeOffset ProcessedAt { get; set; }
}
