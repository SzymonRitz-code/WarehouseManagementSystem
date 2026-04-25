using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WarehouseManagementSystem.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class DocumentUpdateUserReferenceRemoved : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Documents_ConfirmedById",
                table: "Documents");

            migrationBuilder.DropIndex(
                name: "IX_Documents_TransferStartedById",
                table: "Documents");

            migrationBuilder.AddColumn<string>(
                name: "ConfirmedByEmail",
                table: "Documents",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ConfirmedByName",
                table: "Documents",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedByEmail",
                table: "Documents",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedByName",
                table: "Documents",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TransferStartedByEmail",
                table: "Documents",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TransferStartedByName",
                table: "Documents",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ConfirmedByEmail",
                table: "Documents");

            migrationBuilder.DropColumn(
                name: "ConfirmedByName",
                table: "Documents");

            migrationBuilder.DropColumn(
                name: "CreatedByEmail",
                table: "Documents");

            migrationBuilder.DropColumn(
                name: "CreatedByName",
                table: "Documents");

            migrationBuilder.DropColumn(
                name: "TransferStartedByEmail",
                table: "Documents");

            migrationBuilder.DropColumn(
                name: "TransferStartedByName",
                table: "Documents");

            migrationBuilder.CreateIndex(
                name: "IX_Documents_ConfirmedById",
                table: "Documents",
                column: "ConfirmedById");

            migrationBuilder.CreateIndex(
                name: "IX_Documents_TransferStartedById",
                table: "Documents",
                column: "TransferStartedById");

            migrationBuilder.AddForeignKey(
                name: "FK_Documents_Users_ConfirmedById",
                table: "Documents",
                column: "ConfirmedById",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Documents_Users_TransferStartedById",
                table: "Documents",
                column: "TransferStartedById",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
