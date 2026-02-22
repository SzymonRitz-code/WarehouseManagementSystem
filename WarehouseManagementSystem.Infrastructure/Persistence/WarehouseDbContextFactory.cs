using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace WarehouseManagementSystem.Infrastructure.Persistence
{
    internal class WarehouseDbContextFactory : IDesignTimeDbContextFactory<WarehouseManagementSystemDbContext>
    {
        public WarehouseManagementSystemDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<WarehouseManagementSystemDbContext>();

            // Tutaj podajesz connection string do swojej bazy
            optionsBuilder.UseSqlServer("Server=DESKTOP-1209KTE\\SQLEXPRESS;Database=WarehouseDb;Trusted_Connection=True;TrustServerCertificate=True;");

            return new WarehouseManagementSystemDbContext(optionsBuilder.Options);
        }
    }
}