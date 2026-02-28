using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.Reflection.Emit;
using WarehouseManagementSystem.Domain.Model.AuditDomain;
using WarehouseManagementSystem.Domain.Model.CatalogDomain;
using WarehouseManagementSystem.Domain.Model.Documents;
using WarehouseManagementSystem.Domain.Model.DocumentsDomain;
using WarehouseManagementSystem.Domain.Model.InventoryDomain;
using WarehouseManagementSystem.Domain.Model.SecurityDomain;
using WarehouseManagementSystem.Domain.Model.WarehouseDomain;

namespace WarehouseManagementSystem.Infrastructure.Persistence
{
    public class WarehouseManagementSystemDbContext : DbContext
    {
        protected WarehouseManagementSystemDbContext() { }
        public WarehouseManagementSystemDbContext(DbContextOptions options) : base(options) { }

        public DbSet<AuditLog> AuditLogs { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<Document> Documents { get; set; }
        public DbSet<DocumentItem> DocumentItems { get; set; }
        public DbSet<DocumentSequence> DocumentSequences { get; set; }
        public DbSet<ProductBatch> ProductBatches { get; set; }
        public DbSet<Stock> Stocks { get; set; }
        public DbSet<StockReservation> StockReservations { get; set; }
        public DbSet<Warehouse> Warehouses { get; set; }
        public DbSet<WarehouseZone> WarehouseZones { get; set; }
        public DbSet<User> Users { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            base.OnConfiguring(optionsBuilder);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            ConfigureWarehouse(modelBuilder);
            ConfigureWarehouseZone(modelBuilder);

            ConfigureProduct(modelBuilder);
            ConfigureProductBatch(modelBuilder);

            ConfigureStock(modelBuilder);
            ConfigureStockReservation(modelBuilder);

            ConfigureDocument(modelBuilder);
            ConfigureDocumentItem(modelBuilder);
            ConfigureDocumentSequence(modelBuilder);

            ConfigureUser(modelBuilder);
            ConfigureAuditLog(modelBuilder);

            base.OnModelCreating(modelBuilder);
        }

        // ================= INVENTORY =================
        private void ConfigureStock(ModelBuilder modelBuilder)
        {
            var builder = modelBuilder.Entity<Stock>();
            builder.HasKey(x => x.Id);

            builder.Property(x => x.QuantityTotal)
                   .HasPrecision(18, 2)
                   .IsRequired();

            builder.Property(x => x.QuantityReserved)
                   .HasPrecision(18, 2)
                   .IsRequired();

            builder.Property(x => x.RowVersion)
                   .IsRowVersion();

            builder.HasIndex(x => new { x.ProductId, x.WarehouseId, x.WarehouseZoneId, x.ProductBatchId })
                   .IsUnique();

            builder.HasOne(x => x.Product)
                   .WithMany()
                   .HasForeignKey(x => x.ProductId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.Warehouse)
                   .WithMany(w => w.Stocks)
                   .HasForeignKey(x => x.WarehouseId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.WarehouseZone)
                   .WithMany(z => z.Stocks)
                   .HasForeignKey(x => x.WarehouseZoneId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.ProductBatch)
                   .WithMany()
                   .HasForeignKey(x => x.ProductBatchId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.ToTable(t =>
                t.HasCheckConstraint("CK_Stock_PositiveQty",
                    "[QuantityTotal] >= 0 AND [QuantityReserved] >= 0"));
        }

        private void ConfigureStockReservation(ModelBuilder modelBuilder)
        {
            var builder = modelBuilder.Entity<StockReservation>();
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Quantity)
                   .HasPrecision(18, 2)
                   .IsRequired();

            builder.Property(x => x.ExpiresAt)
                   .IsRequired(false);

            builder.Property(x => x.Status)
                   .HasConversion<string>();

            builder.HasIndex(x => x.StockId);
            builder.HasIndex(x => x.ExpiresAt);
            builder.HasIndex(x => x.Status);

            builder.HasOne(x => x.Stock)
                   .WithMany(s => s.Reservations)
                   .HasForeignKey(x => x.StockId)
                   .OnDelete(DeleteBehavior.Restrict);
        }

        private void ConfigureProductBatch(ModelBuilder modelBuilder)
        {
            var builder = modelBuilder.Entity<ProductBatch>();
            builder.HasKey(x => x.Id);

            builder.Property(x => x.BatchNumber)
                   .IsRequired()
                   .HasMaxLength(50);

            builder.Property(x => x.ExpirationDate)
                   .HasColumnType("date");

            builder.Property(x => x.ManufacturedDate)
                   .HasColumnType("date");

            builder.HasOne(x => x.Product)
                   .WithMany()
                   .HasForeignKey(x => x.ProductId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(x => new { x.BatchNumber, x.ProductId });
        }

        // ================= WAREHOUSE =================
        private void ConfigureWarehouse(ModelBuilder modelBuilder)
        {
            var builder = modelBuilder.Entity<Warehouse>();
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Code)
                   .IsRequired()
                   .HasMaxLength(30);

            builder.Property(x => x.Name)
                   .IsRequired()
                   .HasMaxLength(200);

            builder.Property(x => x.Country)
                   .IsRequired()
                   .HasMaxLength(100);

            builder.Property(x => x.City)
                   .IsRequired()
                   .HasMaxLength(100);

            builder.Property(x => x.Address)
                   .IsRequired()
                   .HasMaxLength(200);

            builder.Property(x => x.IsActive)
                   .IsRequired();

            builder.Property(x => x.CreatedAt)
                   .IsRequired();

            builder.HasIndex(x => x.Code).IsUnique();
        }

        private void ConfigureWarehouseZone(ModelBuilder modelBuilder)
        {
            var builder = modelBuilder.Entity<WarehouseZone>();
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Code)
                   .IsRequired()
                   .HasMaxLength(30);

            builder.Property(x => x.Name)
                   .IsRequired()
                   .HasMaxLength(100);

            builder.Property(x => x.TemperatureType)
                   .HasConversion<string>()
                   .HasMaxLength(20)
                   .IsRequired();

            builder.HasIndex(x => new { x.WarehouseId, x.Code }).IsUnique();

            builder.HasOne(x => x.Warehouse)
                   .WithMany(w => w.Zones)
                   .HasForeignKey(x => x.WarehouseId)
                   .OnDelete(DeleteBehavior.Restrict);
        }

        // ================= CATALOG =================
        private void ConfigureProduct(ModelBuilder modelBuilder)
        {
            var builder = modelBuilder.Entity<Product>();
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Name)
                   .IsRequired()
                   .HasMaxLength(200);

            builder.Property(x => x.SKU)
                   .IsRequired()
                   .HasMaxLength(50);

            builder.HasIndex(x => x.SKU).IsUnique();
        }

        // ================= DOCUMENTS =================
        private void ConfigureDocument(ModelBuilder modelBuilder)
        {
            var builder = modelBuilder.Entity<Document>();
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Number)
                   .IsRequired()
                   .HasMaxLength(50);

            builder.HasIndex(x => x.Number).IsUnique();

            // Enum stored as string
            builder.Property(x => x.Type)
                   .HasConversion<string>()
                   .IsRequired()
                   .HasMaxLength(20);

            builder.Property(x => x.Status)
                   .HasConversion<string>()
                   .IsRequired()
                   .HasMaxLength(20);

            builder.Property(x => x.DocumentDate)
                   .HasColumnType("date")
                   .IsRequired();

            builder.Property(x => x.CreatedAt)
                   .IsRequired()
                   .HasColumnType("datetimeoffset");

            builder.Property(x => x.ConfirmedAt)
                   .IsRequired(false)
                   .HasColumnType("datetimeoffset");

            builder.Property(x => x.TransferStartedAt)
                   .IsRequired(false)
                   .HasColumnType("datetimeoffset");

            builder.Property(x => x.RowVersion)
                   .IsRowVersion();

            // Relacje do użytkownika
            builder.HasOne(x => x.CreatedBy)
                   .WithMany()
                   .HasForeignKey(x => x.CreatedById)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.ConfirmedBy)
                   .WithMany()
                   .HasForeignKey(x => x.ConfirmedById)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.TransferStartedBy)
                   .WithMany()
                   .HasForeignKey(x => x.TransferStartedById)
                   .OnDelete(DeleteBehavior.Restrict);

            // Relacje do magazynów
            builder.HasOne(x => x.SourceWarehouse)
                   .WithMany(w => w.SourceDocuments)
                   .HasForeignKey(x => x.SourceWarehouseId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.TargetWarehouse)
                   .WithMany(w => w.TargetDocuments)
                   .HasForeignKey(x => x.TargetWarehouseId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasMany(x => x.Items)
                   .WithOne(i => i.Document)
                   .HasForeignKey(i => i.DocumentId)
                   .OnDelete(DeleteBehavior.Cascade);

            // Indeksy pomocnicze
            builder.HasIndex(x => x.DocumentDate);
            builder.HasIndex(x => x.SourceWarehouseId);
            builder.HasIndex(x => x.TargetWarehouseId);
        }

        private void ConfigureDocumentItem(ModelBuilder modelBuilder)
        {
            var builder = modelBuilder.Entity<DocumentItem>();
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Quantity)
                   .HasPrecision(18, 2)
                   .IsRequired();

            builder.HasOne(x => x.Product)
                   .WithMany()
                   .HasForeignKey(x => x.ProductId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.ProductBatch)
                   .WithMany()
                   .HasForeignKey(x => x.ProductBatchId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.SourceZone)
                   .WithMany()
                   .HasForeignKey(x => x.SourceZoneId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.TargetZone)
                   .WithMany()
                   .HasForeignKey(x => x.TargetZoneId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(x => x.DocumentId);
            builder.HasIndex(x => x.ProductId);
            builder.HasIndex(x => x.ProductBatchId);
            builder.HasIndex(x => x.SourceZoneId);
            builder.HasIndex(x => x.TargetZoneId);
        }
        public void ConfigureDocumentSequence(ModelBuilder modelBuilder)
        {
            var builder = modelBuilder.Entity<DocumentSequence>();
            builder.ToTable("DocumentSequences");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Type)
                .IsRequired();

            builder.Property(x => x.Year)
                .IsRequired();

            builder.Property(x => x.LastNumber)
                .IsRequired();

            builder.HasIndex(x => new { x.Type, x.Year, x.WarehouseId })
                .IsUnique();
        }

        // ================= SECURITY =================
        private void ConfigureUser(ModelBuilder modelBuilder)
        {
            var builder = modelBuilder.Entity<User>();
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Email)
                   .IsRequired()
                   .HasMaxLength(255);

            builder.Property(x => x.Name)
                   .IsRequired(false)
                   .HasMaxLength(200);

            builder.HasIndex(x => x.Email).IsUnique();
        }

        // ================= AUDIT =================
        private void ConfigureAuditLog(ModelBuilder modelBuilder)
        {
            var builder = modelBuilder.Entity<AuditLog>();
            builder.HasKey(x => x.Id);

            builder.Property(x => x.EntityName)
                   .IsRequired()
                   .HasMaxLength(200);

            builder.Property(x => x.EntityId)
                   .IsRequired();

            builder.Property(x => x.Operation)
                   .IsRequired()
                   .HasMaxLength(50);

            builder.Property(x => x.OldValues)
                   .HasColumnType("nvarchar(max)");

            builder.Property(x => x.NewValues)
                   .HasColumnType("nvarchar(max)");

            builder.Property(x => x.PerformedAt)
                   .IsRequired()
                   .HasColumnType("datetimeoffset");

            builder.Property(x => x.IpAddress)
                   .HasMaxLength(50)
                   .IsRequired(false);

            builder.HasOne(x => x.PerformedBy)
                   .WithMany()
                   .HasForeignKey(x => x.PerformedById)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(x => new { x.EntityName, x.EntityId });
        }
    }
}