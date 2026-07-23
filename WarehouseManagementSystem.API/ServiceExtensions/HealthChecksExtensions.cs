using WarehouseManagementSystem.API.Caching;
using WarehouseManagementSystem.API.Integration.Configuration;

namespace WarehouseManagementSystem.API.ServiceExtensions;

public static class HealthChecksExtensions
{
    public static IServiceCollection AddWmsHealthChecks(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var redisOptions = configuration.GetSection(RedisCacheOptions.SectionName).Get<RedisCacheOptions>()
                           ?? new RedisCacheOptions();

        var messagingOptions = configuration.GetSection(MessagingOptions.SectionName).Get<MessagingOptions>()
                               ?? new MessagingOptions();

        var connectionString = configuration.GetConnectionString("WarehouseManagementSystemConnection")!;

        var healthChecks = services.AddHealthChecks()
            .AddSqlServer(connectionString, name: "sql-server", tags: ["db", "infrastructure"]);

        if (redisOptions.Enabled)
        {
            healthChecks.AddCheck<RedisHealthCheck>("redis", tags: ["cache", "infrastructure"]);
        }

        if (messagingOptions.Enabled)
        {
            healthChecks.AddRabbitMQ(
                rabbitConnectionString: $"amqp://{messagingOptions.RabbitMq.UserName}:{messagingOptions.RabbitMq.Password}@{messagingOptions.RabbitMq.HostName}:{messagingOptions.RabbitMq.Port}/{Uri.EscapeDataString(messagingOptions.RabbitMq.VirtualHost)}",
                name: "rabbitmq",
                tags: ["messaging", "infrastructure"]);
        }

        return services;
    }
}
