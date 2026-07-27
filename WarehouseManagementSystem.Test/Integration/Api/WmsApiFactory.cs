using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Testcontainers.MsSql;
using WarehouseManagementSystem.Infrastructure.Persistence;

namespace WarehouseManagementSystem.Tests.Integration.Api;

/// <summary>
/// WebApplicationFactory that boots the full WMS API against a real SQL Server Testcontainer.
/// Shared across all tests in the same collection to avoid spinning up a new container per test class.
/// </summary>
public sealed class WmsApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly MsSqlContainer _sqlContainer = new MsSqlBuilder()
        .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
        .Build();

    public async Task InitializeAsync()
    {
        await _sqlContainer.StartAsync();
    }

    public new async Task DisposeAsync()
    {
        await _sqlContainer.DisposeAsync();
        await base.DisposeAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Replace real DbContext with Testcontainer SQL Server
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<WarehouseManagementSystemDbContext>));

            if (descriptor != null)
            {
                services.Remove(descriptor);
            }

            services.AddDbContext<WarehouseManagementSystemDbContext>(options =>
                options.UseSqlServer(_sqlContainer.GetConnectionString()));

            // Remove all registered health checks (SQL Server, Redis, RabbitMQ) and replace with
            // an always-healthy no-op so /health returns 200 without requiring external services.
            services.Configure<HealthCheckServiceOptions>(opts => opts.Registrations.Clear());
        });

        builder.UseEnvironment("Testing");
    }

    public async Task<WarehouseManagementSystemDbContext> GetDbContextAsync()
    {
        var scope = Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<WarehouseManagementSystemDbContext>();
        await context.Database.MigrateAsync();
        return context;
    }
}
