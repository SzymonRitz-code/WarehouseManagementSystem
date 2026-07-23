using Microsoft.EntityFrameworkCore;
using WarehouseManagementSystem.API.Caching;
using WarehouseManagementSystem.API.Extensions;
using WarehouseManagementSystem.API.Integration;
using WarehouseManagementSystem.API.Services.AuditLogs.Command;
using WarehouseManagementSystem.API.Services.AuditLogs.Query;
using WarehouseManagementSystem.API.Services.Documents.Command;
using WarehouseManagementSystem.API.Services.Documents.Query;
using WarehouseManagementSystem.API.Services.ProductBatches.Command;
using WarehouseManagementSystem.API.Services.ProductBatches.Query;
using WarehouseManagementSystem.API.Services.Products.Command;
using WarehouseManagementSystem.API.Services.Products.Query;
using WarehouseManagementSystem.API.Services.Seed;
using WarehouseManagementSystem.API.Services.Stocks.Command;
using WarehouseManagementSystem.API.Services.Stocks.Query;
using WarehouseManagementSystem.API.Services.User;
using WarehouseManagementSystem.API.Services.Warehouses.Command;
using WarehouseManagementSystem.API.Services.Warehouses.Query;
using WarehouseManagementSystem.Domain.Interfaces;
using WarehouseManagementSystem.Domain.Services;
using WarehouseManagementSystem.Infrastructure.Persistence;
using WarehouseManagementSystem.Infrastructure.Services;

namespace WarehouseManagementSystem.API.ServiceExtensions;

public static class InfrastructureExtensions
{
    public static IServiceCollection AddWmsInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration,
        IWebHostEnvironment environment)
    {
        services.AddDbContext<WarehouseManagementSystemDbContext>(options =>
        {
            options.UseSqlServer(configuration.GetConnectionString("WarehouseManagementSystemConnection"));

            if (environment.IsDevelopment())
            {
                options.EnableSensitiveDataLogging()
                       .EnableDetailedErrors();
            }
        });

        var redisOptions = configuration.GetSection(RedisCacheOptions.SectionName).Get<RedisCacheOptions>()
                           ?? new RedisCacheOptions();

        services.Configure<RedisCacheOptions>(configuration.GetSection(RedisCacheOptions.SectionName));
        services.AddDistributedMemoryCache();

        if (redisOptions.Enabled)
        {
            services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration = redisOptions.ConnectionString;
                options.InstanceName = redisOptions.InstancePrefix + ":";
            });
        }

        services.AddSingleton<ICacheKeyBuilder, CacheKeyBuilder>();
        services.AddSingleton<ICacheRegionGenerationStore, DistributedCacheRegionGenerationStore>();
        services.AddScoped<IQueryCacheService, QueryCacheService>();
        services.AddScoped<ICacheInvalidationService, CacheInvalidationService>();

        return services;
    }

    public static IServiceCollection AddWmsApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<IAuditLogCommandService, AuditLogCommandService>();
        services.AddScoped<IAuditLogQueryService, AuditLogQueryService>();
        services.AddScoped<IDocumentCommandService, DocumentCommandService>();
        services.AddScoped<IDocumentQueryService, DocumentQueryService>();
        services.AddScoped<IProductCommandService, ProductCommandService>();
        services.AddScoped<IProductQueryService, ProductQueryService>();
        services.AddScoped<IWarehouseCommandService, WarehouseCommandService>();
        services.AddScoped<IWarehouseZoneCommandService, WarehouseZoneCommandService>();
        services.AddScoped<IWarehouseQueryService, WarehouseQueryService>();
        services.AddScoped<IStockCommandService, StockCommandService>();
        services.AddScoped<IStockQueryService, StockQueryService>();
        services.AddScoped<IStockReservationService, StockReservationService>();
        services.AddScoped<IProductBatchQueryService, ProductBatchQueryService>();
        services.AddScoped<IProductBatchCommandService, ProductBatchCommandService>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IIntegrationOutbox, IntegrationOutbox>();

        services.AddTransient<IDocumentNumberGenerator, DocumentNumberGenerator>();

        services.AddHostedService<DatabaseSeedingHostedService>();
        services.AddHostedService<ReservationExpirationJob>();
        services.AddHostedService<OutboxPublisherWorker>();

        services.AddSingleton<ISystemClock, SystemClock>();
        services.AddSingleton<IUserService, UserService>();

        services.AddAutoMapper(cfg => cfg.AddWmsMappings());

        return services;
    }
}
