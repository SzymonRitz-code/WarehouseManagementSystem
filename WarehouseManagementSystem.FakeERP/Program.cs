using Microsoft.EntityFrameworkCore;
using WarehouseManagementSystem.Contracts;
using WarehouseManagementSystem.FakeERP;
var builder = Host.CreateApplicationBuilder(args);
builder.Services.Configure<ErpMessagingOptions>(builder.Configuration.GetSection(ErpMessagingOptions.SectionName));
builder.Services.AddDbContext<ErpDbContext>(
    databaseOptions => databaseOptions.UseSqlite(builder.Configuration.GetConnectionString("Erp") ?? "Data Source=fake-erp.db")
    );
builder.Services.AddSingleton<ErpRabbit>();
builder.Services.AddScoped<DocumentConfirmedHandler>();
builder.Services.AddHostedService<ErpOutboxPublisher>();
builder.Services.AddHostedService<ErpConfirmedConsumer>();
var host = builder.Build();

using (var erpScope = host.Services.CreateScope())
{
    var erpDbContext = erpScope.ServiceProvider.GetRequiredService<ErpDbContext>();
    await erpDbContext.Database.EnsureCreatedAsync();
    if (args.Contains("--demo-order") && !await erpDbContext.WarehouseOrders.AnyAsync(x => x.ExternalOrderId == "ERP-DEMO-001"))
    {
        var orderCorrelationId = Guid.NewGuid();
        var demoOrder = new ErpWarehouseOrder
        {
            Id = Guid.NewGuid(),
            ExternalOrderId = "ERP-DEMO-001",
            CorrelationId = orderCorrelationId,
            CreatedAt = DateTimeOffset.UtcNow
        };
        erpDbContext.WarehouseOrders.Add(demoOrder);
        erpDbContext.OutboxMessages.Add(new ErpOutboxMessage
        {
            Id = Guid.NewGuid(),
            MessageId = Guid.NewGuid(),
            CorrelationId = orderCorrelationId,
            OccurredAt = DateTimeOffset.UtcNow,
            Payload = System.Text.Json.JsonSerializer.Serialize(new CreateWarehouseDocumentCommand
            {
                MessageId = Guid.NewGuid(),
                CorrelationId = orderCorrelationId,
                OccurredAt = DateTimeOffset.UtcNow,
                ExternalOrderId = demoOrder.ExternalOrderId,
                DocumentType = "PZ",
                SourceWarehouseId = Guid.Empty,
                DocumentDate = DateTime.UtcNow,
                Items = []
            })
        });
        await erpDbContext.SaveChangesAsync();
    }
}
await host.RunAsync();
