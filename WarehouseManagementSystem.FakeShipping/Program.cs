using Microsoft.EntityFrameworkCore;
using WarehouseManagementSystem.FakeShipping;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.Configure<ShippingMessagingOptions>(builder.Configuration.GetSection(ShippingMessagingOptions.SectionName));
builder.Services.AddDbContext<ShippingDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("Shipping") ?? "Data Source=fake-shipping.db"));
builder.Services.AddSingleton<ShippingRabbitMqConnectionFactory>();
builder.Services.AddSingleton<ShippingRabbitMqTopology>();
builder.Services.AddScoped<DocumentConfirmedHandler>();
builder.Services.AddHostedService<ShippingConsumerWorker>();

var host = builder.Build();
using (var scope = host.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ShippingDbContext>();
    await db.Database.EnsureCreatedAsync();
}
await host.RunAsync();
