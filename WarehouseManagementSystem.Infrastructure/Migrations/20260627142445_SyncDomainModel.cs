using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WarehouseManagementSystem.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SyncDomainModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_WarehouseZones_WarehouseId_Code",
                table: "WarehouseZones");

            migrationBuilder.DropIndex(
                name: "IX_Warehouses_Code",
                table: "Warehouses");

            migrationBuilder.DropIndex(
                name: "IX_Products_SKU",
                table: "Products");

            migrationBuilder.DropIndex(
                name: "IX_Documents_SourceWarehouseId",
                table: "Documents");

            migrationBuilder.DropIndex(
                name: "IX_DocumentItems_DocumentId",
                table: "DocumentItems");

            migrationBuilder.CreateIndex(
                name: "IX_WarehouseZones_WarehouseId_Code",
                table: "WarehouseZones",
                columns: new[] { "WarehouseId", "Code" },
                unique: true)
                .Annotation("SqlServer:Include", new[] { "Name", "TemperatureType", "IsPickingZone" });

            migrationBuilder.CreateIndex(
                name: "IX_Warehouses_Code",
                table: "Warehouses",
                column: "Code",
                unique: true)
                .Annotation("SqlServer:Include", new[] { "Name", "Country", "City", "Address", "IsActive", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Products_SKU_IsActive",
                table: "Products",
                columns: new[] { "SKU", "IsActive" },
                unique: true)
                .Annotation("SqlServer:Include", new[] { "Name", "Unit", "Weight", "Volume" });

            migrationBuilder.CreateIndex(
                name: "IX_Documents_CreatedAt",
                table: "Documents",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Documents_SourceWarehouseId",
                table: "Documents",
                column: "SourceWarehouseId")
                .Annotation("SqlServer:Include", new[] { "Number", "Type", "Status", "CreatedAt", "ConfirmedAt", "DocumentDate", "TargetWarehouseId" });

            migrationBuilder.CreateIndex(
                name: "IX_DocumentItems_DocumentId",
                table: "DocumentItems",
                column: "DocumentId")
                .Annotation("SqlServer:Include", new[] { "Quantity", "ProductId", "ProductBatchId", "SourceZoneId", "TargetZoneId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_WarehouseZones_WarehouseId_Code",
                table: "WarehouseZones");

            migrationBuilder.DropIndex(
                name: "IX_Warehouses_Code",
                table: "Warehouses");

            migrationBuilder.DropIndex(
                name: "IX_Products_SKU_IsActive",
                table: "Products");

            migrationBuilder.DropIndex(
                name: "IX_Documents_CreatedAt",
                table: "Documents");

            migrationBuilder.DropIndex(
                name: "IX_Documents_SourceWarehouseId",
                table: "Documents");

            migrationBuilder.DropIndex(
                name: "IX_DocumentItems_DocumentId",
                table: "DocumentItems");

            migrationBuilder.CreateIndex(
                name: "IX_WarehouseZones_WarehouseId_Code",
                table: "WarehouseZones",
                columns: new[] { "WarehouseId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Warehouses_Code",
                table: "Warehouses",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Products_SKU",
                table: "Products",
                column: "SKU",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Documents_SourceWarehouseId",
                table: "Documents",
                column: "SourceWarehouseId");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentItems_DocumentId",
                table: "DocumentItems",
                column: "DocumentId");
        }
    }
}
