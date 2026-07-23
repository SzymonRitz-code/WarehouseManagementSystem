using WarehouseManagementSystem.API.Integration;
using WarehouseManagementSystem.API.Integration.Configuration;

namespace WarehouseManagementSystem.API.ServiceExtensions;

public static class MessagingExtensions
{
    public static IServiceCollection AddWmsMessaging(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<MessagingOptions>(configuration.GetSection(MessagingOptions.SectionName));

        services.AddSingleton<IRabbitMqConnectionFactory, RabbitMqConnectionFactory>();
        services.AddSingleton<IRabbitMqTopologyConfigurator, RabbitMqTopologyConfigurator>();

        return services;
    }
}
