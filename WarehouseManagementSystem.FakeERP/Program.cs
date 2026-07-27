using Microsoft.EntityFrameworkCore;
using WarehouseManagementSystem.Contracts;
using WarehouseManagementSystem.FakeERP;
var builder = Host.CreateApplicationBuilder(args);
builder.Services.Configure<ErpMessagingOptions>(builder.Configuration.GetSection(ErpMessagingOptions.SectionName));
builder.Services.AddDbContext<ErpDbContext>(o => o.UseSqlite(builder.Configuration.GetConnectionString("Erp") ?? "Data Source=fake-erp.db"));
builder.Services.AddSingleton<ErpRabbit>(); builder.Services.AddScoped<DocumentConfirmedHandler>(); builder.Services.AddHostedService<ErpOutboxPublisher>(); builder.Services.AddHostedService<ErpConfirmedConsumer>();
var host = builder.Build(); using (var scope = host.Services.CreateScope()) { var db = scope.ServiceProvider.GetRequiredService<ErpDbContext>(); await db.Database.EnsureCreatedAsync(); if (args.Contains("--demo-order") && !await db.WarehouseOrders.AnyAsync(x => x.ExternalOrderId == "ERP-DEMO-001")) { var correlation = Guid.NewGuid(); var order = new ErpWarehouseOrder { Id = Guid.NewGuid(), ExternalOrderId = "ERP-DEMO-001", CorrelationId = correlation, CreatedAt = DateTimeOffset.UtcNow }; db.WarehouseOrders.Add(order); db.OutboxMessages.Add(new ErpOutboxMessage { Id = Guid.NewGuid(), MessageId = Guid.NewGuid(), CorrelationId = correlation, OccurredAt = DateTimeOffset.UtcNow, Payload = System.Text.Json.JsonSerializer.Serialize(new CreateWarehouseDocumentCommand { MessageId = Guid.NewGuid(), CorrelationId = correlation, OccurredAt = DateTimeOffset.UtcNow, ExternalOrderId = order.ExternalOrderId, DocumentType = "PZ", SourceWarehouseId = Guid.Empty, DocumentDate = DateTime.UtcNow, Items = [] }) }); await db.SaveChangesAsync(); } }
await host.RunAsync();
