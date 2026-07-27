using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using WarehouseManagementSystem.Contracts;

namespace WarehouseManagementSystem.FakeERP;

public sealed class ErpMessagingOptions
{
    public const string SectionName = "ErpMessaging";
    public string HostName { get; set; } = "localhost";
    public int Port { get; set; } = 5672;
    public string UserName { get; set; } = "guest";
    public string Password { get; set; } = "guest";
    public string CommandsExchange { get; set; } = "erp.commands";
    public string CommandsRoutingKey { get; set; } = "erp.document.create";
    public string Queue { get; set; } = "erp.document-confirmed";
    public string RetryExchange { get; set; } = "erp.document-confirmed.retry.exchange";
    public string RetryQueue { get; set; } = "erp.document-confirmed.retry";
    public string DeadLetterExchange { get; set; } = "erp.document-confirmed.dlx";
    public string DeadLetterQueue { get; set; } = "erp.document-confirmed.dlq";
    public string EventExchange { get; set; } = "wms.events";
    public int RetryDelaySeconds { get; set; } = 10;
    public int MaxRetryAttempts { get; set; } = 3;
}
public sealed class ErpRabbit(IOptions<ErpMessagingOptions> options)
{
    public IConnection Connect()
    {
        var o = options.Value;
        return new ConnectionFactory
        {
            HostName = o.HostName,
            Port = o.Port,
            UserName = o.UserName,
            Password = o.Password
        }.CreateConnection();
    }
}
public sealed class ErpOutboxPublisher(
    IServiceScopeFactory scopes,
    ErpRabbit rabbit,
    IOptions<ErpMessagingOptions> options,
    ILogger<ErpOutboxPublisher> logger) : BackgroundService

{
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                using var s = scopes.CreateScope();
                var db = s.ServiceProvider.GetRequiredService<ErpDbContext>();
                using var c = rabbit.Connect();
                using var ch = c.CreateModel();

                var o = options.Value;
                ch.ExchangeDeclare(o.CommandsExchange, ExchangeType.Direct, true);
                ch.ConfirmSelect();
                var messages = await db.OutboxMessages
                    .Where(x => x.Status == "Pending" || x.Status == "Failed")
                    .Where(x => x.NextAttemptAt == null || x.NextAttemptAt <= DateTimeOffset.UtcNow).Take(20).ToListAsync(ct);
                foreach (var m in messages)
                {
                    try
                    {
                        var p = ch.CreateBasicProperties();
                        p.Persistent = true;
                        p.MessageId = m.MessageId.ToString();
                        p.CorrelationId = m.CorrelationId.ToString();
                        p.Type = nameof(CreateWarehouseDocumentCommand);

                        ch.BasicPublish(o.CommandsExchange, o.CommandsRoutingKey, p, Encoding.UTF8.GetBytes(m.Payload));

                        if (!ch.WaitForConfirms(TimeSpan.FromSeconds(10)))
                        {
                            throw new InvalidOperationException("Broker did not confirm publish.");
                        }

                        m.Status = "Published";
                        m.PublishedAt = DateTimeOffset.UtcNow;
                    }
                    catch (Exception ex)
                    {
                        m.RetryCount++;
                        m.LastError = ex.Message;
                        m.Status = m.RetryCount >= 3
                            ? "Abandoned"
                            : "Failed";
                        m.NextAttemptAt = DateTimeOffset.UtcNow.AddSeconds(30);
                    }
                }
                await db.SaveChangesAsync(ct);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "ERP outbox iteration failed");
            }

            await Task.Delay(TimeSpan.FromSeconds(5), ct);
        }
    }
}
public sealed class ErpConfirmedConsumer(
    IServiceScopeFactory scopes, ErpRabbit rabbit,
    IOptions<ErpMessagingOptions> options,
    ILogger<ErpConfirmedConsumer> logger)
    : BackgroundService
{
    protected override Task ExecuteAsync(CancellationToken ct)
    {
        return Task.Run(() => Run(ct), ct);
    }

    private void Run(CancellationToken ct)
    {
        using var c = rabbit.Connect(); using var ch = c.CreateModel();
        var o = options.Value; ch.ExchangeDeclare(o.EventExchange, ExchangeType.Direct, true);
        ch.ExchangeDeclare(o.RetryExchange, ExchangeType.Direct, true);
        ch.ExchangeDeclare(o.DeadLetterExchange, ExchangeType.Direct, true);
        ch.QueueDeclare(o.DeadLetterQueue, true, false, false);
        ch.QueueBind(o.DeadLetterQueue, o.DeadLetterExchange, "document.confirmed");
        ch.QueueDeclare(o.RetryQueue, true, false, false, new Dictionary<string, object> {
            { "x-message-ttl", o.RetryDelaySeconds * 1000 },
            { "x-dead-letter-exchange", o.EventExchange },
            { "x-dead-letter-routing-key", "document.confirmed" }
        });
        ch.QueueBind(o.RetryQueue, o.RetryExchange, "document.confirmed");
        ch.QueueDeclare(o.Queue, true, false, false, new Dictionary<string, object> {
            { "x-dead-letter-exchange", o.DeadLetterExchange },
            { "x-dead-letter-routing-key", "document.confirmed" }
        });
        ch.QueueBind(o.Queue, o.EventExchange, "document.confirmed");
        var consumer = new EventingBasicConsumer(ch);
        consumer.Received += (_, d) =>
        {
            try
            {
                var m = JsonSerializer.Deserialize<DocumentConfirmedIntegrationEvent>(
                    Encoding.UTF8.GetString(d.Body.ToArray()),
                    new JsonSerializerOptions(JsonSerializerDefaults.Web)) ?? throw new InvalidOperationException();

                using var s = scopes.CreateScope();

                s.ServiceProvider.GetRequiredService<DocumentConfirmedHandler>()
                    .HandleAsync(m, ct)
                    .GetAwaiter()
                    .GetResult();

                ch.BasicAck(d.DeliveryTag, false);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "ERP confirmation failed");
                ch.BasicNack(d.DeliveryTag, false, false);
            }
        };
        ch.BasicConsume(o.Queue, false, consumer); 
        ct.WaitHandle.WaitOne();
    }
}
