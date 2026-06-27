using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace WarehouseManagementSystem.Infrastructure.Persistence;

internal class WarehouseDbContextFactory : IDesignTimeDbContextFactory<WarehouseManagementSystemDbContext>
{
    public WarehouseManagementSystemDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<WarehouseManagementSystemDbContext>();

        // Tutaj podajesz connection string do swojej bazy
        optionsBuilder.UseSqlServer("Server=localhost,14333;Database=WarehouseDb;User Id=sa;Password=Wms_dev_password123!;TrustServerCertificate=True;");

        return new WarehouseManagementSystemDbContext(optionsBuilder.Options);
    }
}
