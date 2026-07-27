using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using WarehouseManagementSystem.Infrastructure.Persistence;

#nullable disable

namespace WarehouseManagementSystem.Infrastructure.Migrations;

[DbContext(typeof(WarehouseManagementSystemDbContext))]
[Migration("20260722120000_AddOutboxRetrySchedule")]
public partial class AddOutboxRetrySchedule : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<DateTimeOffset>(
            name: "NextAttemptAt",
            table: "OutboxMessages",
            type: "datetimeoffset",
            nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "NextAttemptAt", table: "OutboxMessages");
    }
}
