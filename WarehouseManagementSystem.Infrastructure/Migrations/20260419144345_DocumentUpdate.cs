using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WarehouseManagementSystem.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class DocumentUpdate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "TransferStartedAt",
                table: "Documents",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "TransferStartedById",
                table: "Documents",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "DocumentSequences",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    Year = table.Column<int>(type: "int", nullable: false),
                    WarehouseId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LastNumber = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DocumentSequences", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Documents_TransferStartedById",
                table: "Documents",
                column: "TransferStartedById");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentSequences_Type_Year_WarehouseId",
                table: "DocumentSequences",
                columns: new[] { "Type", "Year", "WarehouseId" },
                unique: true,
                filter: "[WarehouseId] IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_Documents_Users_TransferStartedById",
                table: "Documents",
                column: "TransferStartedById",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Documents_Users_TransferStartedById",
                table: "Documents");

            migrationBuilder.DropTable(
                name: "DocumentSequences");

            migrationBuilder.DropIndex(
                name: "IX_Documents_TransferStartedById",
                table: "Documents");

            migrationBuilder.DropColumn(
                name: "TransferStartedAt",
                table: "Documents");

            migrationBuilder.DropColumn(
                name: "TransferStartedById",
                table: "Documents");
        }
    }
}
