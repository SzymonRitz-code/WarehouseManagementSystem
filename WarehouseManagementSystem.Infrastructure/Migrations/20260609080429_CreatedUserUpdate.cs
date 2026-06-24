using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WarehouseManagementSystem.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class CreatedUserUpdate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "CreatedBy",
                table: "StockReservations",
                newName: "CreatedById");

            migrationBuilder.AddColumn<string>(
                name: "CreatedByEmail",
                table: "WarehouseZones",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "CreatedById",
                table: "WarehouseZones",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<string>(
                name: "CreatedByName",
                table: "WarehouseZones",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "CreatedByEmail",
                table: "Warehouses",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "CreatedById",
                table: "Warehouses",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<string>(
                name: "CreatedByName",
                table: "Warehouses",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "CreatedByEmail",
                table: "StockReservations",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "CreatedByName",
                table: "StockReservations",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "CreatedByEmail",
                table: "Products",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "CreatedById",
                table: "Products",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<string>(
                name: "CreatedByName",
                table: "Products",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "CreatedByEmail",
                table: "ProductBatches",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "CreatedById",
                table: "ProductBatches",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<string>(
                name: "CreatedByName",
                table: "ProductBatches",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CreatedByEmail",
                table: "WarehouseZones");

            migrationBuilder.DropColumn(
                name: "CreatedById",
                table: "WarehouseZones");

            migrationBuilder.DropColumn(
                name: "CreatedByName",
                table: "WarehouseZones");

            migrationBuilder.DropColumn(
                name: "CreatedByEmail",
                table: "Warehouses");

            migrationBuilder.DropColumn(
                name: "CreatedById",
                table: "Warehouses");

            migrationBuilder.DropColumn(
                name: "CreatedByName",
                table: "Warehouses");

            migrationBuilder.DropColumn(
                name: "CreatedByEmail",
                table: "StockReservations");

            migrationBuilder.DropColumn(
                name: "CreatedByName",
                table: "StockReservations");

            migrationBuilder.DropColumn(
                name: "CreatedByEmail",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "CreatedById",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "CreatedByName",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "CreatedByEmail",
                table: "ProductBatches");

            migrationBuilder.DropColumn(
                name: "CreatedById",
                table: "ProductBatches");

            migrationBuilder.DropColumn(
                name: "CreatedByName",
                table: "ProductBatches");

            migrationBuilder.RenameColumn(
                name: "CreatedById",
                table: "StockReservations",
                newName: "CreatedBy");
        }
    }
}
