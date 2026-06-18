using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WarehouseManagementSystem.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemoveDocumentTransferStartedBy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TransferStartedByEmail",
                table: "Documents");

            migrationBuilder.DropColumn(
                name: "TransferStartedById",
                table: "Documents");

            migrationBuilder.DropColumn(
                name: "TransferStartedByName",
                table: "Documents");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "TransferStartedByEmail",
                table: "Documents",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "TransferStartedById",
                table: "Documents",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TransferStartedByName",
                table: "Documents",
                type: "nvarchar(max)",
                nullable: true);
        }
    }
}
