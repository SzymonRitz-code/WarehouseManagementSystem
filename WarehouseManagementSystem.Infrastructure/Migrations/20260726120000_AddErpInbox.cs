using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable
namespace WarehouseManagementSystem.Infrastructure.Migrations;

public partial class AddErpInbox : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(name: "InboxMessages", columns: table => new
        {
            Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
            MessageId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
            Consumer = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
            MessageType = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
            CorrelationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
            ReceivedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
            ProcessedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
            Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
            LastError = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
            RetryCount = table.Column<int>(type: "int", nullable: false)
        }, constraints: table => table.PrimaryKey("PK_InboxMessages", x => x.Id));
        migrationBuilder.CreateTable(name: "ErpOrderImports", columns: table => new
        {
            Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
            ExternalOrderId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
            WmsDocumentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
            CorrelationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
            PayloadFingerprint = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
            CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
        }, constraints: table => table.PrimaryKey("PK_ErpOrderImports", x => x.Id));
        migrationBuilder.CreateIndex(name: "IX_InboxMessages_Consumer_MessageId", table: "InboxMessages", columns: new[] { "Consumer", "MessageId" }, unique: true);
        migrationBuilder.CreateIndex(name: "IX_ErpOrderImports_ExternalOrderId", table: "ErpOrderImports", column: "ExternalOrderId", unique: true);
        migrationBuilder.CreateIndex(name: "IX_ErpOrderImports_WmsDocumentId", table: "ErpOrderImports", column: "WmsDocumentId", unique: true);
    }
    protected override void Down(MigrationBuilder migrationBuilder) { migrationBuilder.DropTable("InboxMessages"); migrationBuilder.DropTable("ErpOrderImports"); }
}
