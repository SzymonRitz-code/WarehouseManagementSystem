using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using WarehouseManagementSystem.Infrastructure.Persistence;

namespace WarehouseManagementSystem.Tests.Integration.Api;

/// <summary>
/// WebApplicationFactory that boots the full WMS API against the SQL Server supplied by the test fixture.
/// </summary>
public sealed class WmsApiFactory(string connectionString) : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // The API's production background services connect to RabbitMQ and perform scheduled work.
            // Integration tests exercise HTTP endpoints only, so running them would stop the test host
            // when those external services are unavailable on the test runner.
            services.RemoveAll<IHostedService>();

            // Replace real DbContext with Testcontainer SQL Server
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<WarehouseManagementSystemDbContext>));

            if (descriptor != null)
            {
                services.Remove(descriptor);
            }

            services.AddDbContext<WarehouseManagementSystemDbContext>(options =>
                options.UseSqlServer(connectionString));

            // Remove all registered health checks (SQL Server, Redis, RabbitMQ) and replace with
            // an always-healthy no-op so /health returns 200 without requiring external services.
            services.Configure<HealthCheckServiceOptions>(opts => opts.Registrations.Clear());
        });

        builder.UseEnvironment("Testing");
    }
}
