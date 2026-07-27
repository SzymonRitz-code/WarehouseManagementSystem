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
        var messagingOptions = options.Value;
        return new ConnectionFactory
        {
            HostName = messagingOptions.HostName,
            Port = messagingOptions.Port,
            UserName = messagingOptions.UserName,
            Password = messagingOptions.Password
        }.CreateConnection();
    }
}
public sealed class ErpOutboxPublisher(
    IServiceScopeFactory scopeFactory,
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
                using var publishingScope = scopeFactory.CreateScope();
                var erpDbContext = publishingScope.ServiceProvider.GetRequiredService<ErpDbContext>();
                using var rabbitConnection = rabbit.Connect();
                using var commandChannel = rabbitConnection.CreateModel();

                var messagingOptions = options.Value;
                commandChannel.ExchangeDeclare(messagingOptions.CommandsExchange, ExchangeType.Direct, true);
                commandChannel.ConfirmSelect();
                var pendingOutboxMessages = await erpDbContext.OutboxMessages
                    .Where(x => x.Status == "Pending" || x.Status == "Failed")
                    .Where(x => x.NextAttemptAt == null || x.NextAttemptAt <= DateTimeOffset.UtcNow).Take(20).ToListAsync(ct);
                foreach (var outboxMessage in pendingOutboxMessages)
                {
                    try
                    {
                        var publishProperties = commandChannel.CreateBasicProperties();
                        publishProperties.Persistent = true;
                        publishProperties.MessageId = outboxMessage.MessageId.ToString();
                        publishProperties.CorrelationId = outboxMessage.CorrelationId.ToString();
                        publishProperties.Type = nameof(CreateWarehouseDocumentCommand);

                        commandChannel.BasicPublish(messagingOptions.CommandsExchange, messagingOptions.CommandsRoutingKey, publishProperties, Encoding.UTF8.GetBytes(outboxMessage.Payload));

                        if (!commandChannel.WaitForConfirms(TimeSpan.FromSeconds(10)))
                        {
                            throw new InvalidOperationException("Broker did not confirm publish.");
                        }

                        outboxMessage.Status = "Published";
                        outboxMessage.PublishedAt = DateTimeOffset.UtcNow;
                    }
                    catch (Exception ex)
                    {
                        outboxMessage.RetryCount++;
                        outboxMessage.LastError = ex.Message;
                        outboxMessage.Status = outboxMessage.RetryCount >= 3
                            ? "Abandoned"
                            : "Failed";
                        outboxMessage.NextAttemptAt = DateTimeOffset.UtcNow.AddSeconds(30);
                    }
                }
                await erpDbContext.SaveChangesAsync(ct);
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
    IServiceScopeFactory scopeFactory, ErpRabbit rabbit,
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
        using var rabbitConnection = rabbit.Connect(); using var confirmedDocumentChannel = rabbitConnection.CreateModel();
        var messagingOptions = options.Value; confirmedDocumentChannel.ExchangeDeclare(messagingOptions.EventExchange, ExchangeType.Direct, true);
        confirmedDocumentChannel.ExchangeDeclare(messagingOptions.RetryExchange, ExchangeType.Direct, true);
        confirmedDocumentChannel.ExchangeDeclare(messagingOptions.DeadLetterExchange, ExchangeType.Direct, true);
        confirmedDocumentChannel.QueueDeclare(messagingOptions.DeadLetterQueue, true, false, false);
        confirmedDocumentChannel.QueueBind(messagingOptions.DeadLetterQueue, messagingOptions.DeadLetterExchange, "document.confirmed");
        confirmedDocumentChannel.QueueDeclare(messagingOptions.RetryQueue, true, false, false, new Dictionary<string, object> {
            { "x-message-ttl", messagingOptions.RetryDelaySeconds * 1000 },
            { "x-dead-letter-exchange", messagingOptions.EventExchange },
            { "x-dead-letter-routing-key", "document.confirmed" }
        });
        confirmedDocumentChannel.QueueBind(messagingOptions.RetryQueue, messagingOptions.RetryExchange, "document.confirmed");
        confirmedDocumentChannel.QueueDeclare(messagingOptions.Queue, true, false, false, new Dictionary<string, object> {
            { "x-dead-letter-exchange", messagingOptions.DeadLetterExchange },
            { "x-dead-letter-routing-key", "document.confirmed" }
        });
        confirmedDocumentChannel.QueueBind(messagingOptions.Queue, messagingOptions.EventExchange, "document.confirmed");
        var confirmationConsumer = new EventingBasicConsumer(confirmedDocumentChannel);
        confirmationConsumer.Received += (_, delivery) =>
        {
            try
            {
                var documentConfirmedEvent = JsonSerializer.Deserialize<DocumentConfirmedIntegrationEvent>(
                    Encoding.UTF8.GetString(delivery.Body.ToArray()),
                    new JsonSerializerOptions(JsonSerializerDefaults.Web)) ?? throw new InvalidOperationException();

                using var consumerScope = scopeFactory.CreateScope();

                consumerScope.ServiceProvider.GetRequiredService<DocumentConfirmedHandler>()
                    .HandleAsync(documentConfirmedEvent, ct)
                    .GetAwaiter()
                    .GetResult();

                confirmedDocumentChannel.BasicAck(delivery.DeliveryTag, false);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "ERP confirmation failed");
                confirmedDocumentChannel.BasicNack(delivery.DeliveryTag, false, false);
            }
        };
        confirmedDocumentChannel.BasicConsume(messagingOptions.Queue, false, confirmationConsumer); 
        ct.WaitHandle.WaitOne();
    }
}
