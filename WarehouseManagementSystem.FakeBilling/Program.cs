using Microsoft.EntityFrameworkCore;
using WarehouseManagementSystem.FakeBilling;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.Configure<BillingMessagingOptions>(builder.Configuration.GetSection(BillingMessagingOptions.SectionName));
builder.Services.AddDbContext<BillingDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("Billing") ?? "Data Source=fake-billing.db"));
builder.Services.AddSingleton<BillingRabbitMqConnectionFactory>();
builder.Services.AddSingleton<BillingRabbitMqTopology>();
builder.Services.AddScoped<DocumentConfirmedBillingHandler>();
builder.Services.AddHostedService<BillingConsumerWorker>();

var host = builder.Build();
using (var scope = host.Services.CreateScope())
{
    await scope.ServiceProvider.GetRequiredService<BillingDbContext>().Database.EnsureCreatedAsync();
}

await host.RunAsync();
