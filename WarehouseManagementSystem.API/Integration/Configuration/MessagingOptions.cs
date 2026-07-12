namespace WarehouseManagementSystem.API.Integration.Configuration;

/// <summary>
/// Represents the configuration options for messaging in the Warehouse Management System API.
/// </summary>
public class MessagingOptions
{
    /// <summary>
    /// Gets the name of the configuration section for messaging options.
    /// </summary>
    public const string SectionName = "Messaging";

    /// <summary>
    /// Gets or sets a value indicating whether messaging is enabled.
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// Gets or sets the batch size for publishing messages.
    /// </summary>
    public int PublishBatchSize { get; set; } = 20;

    /// <summary>
    /// Gets or sets the interval in seconds for polling messages to publish.
    /// </summary>
    public int PublishPollIntervalSeconds { get; set; } = 5;

    /// <summary>
    /// Gets or sets the RabbitMQ configuration options.
    /// </summary>
    public RabbitMqOptions RabbitMq { get; set; } = new();

    /// <summary>
    /// Gets or sets the exchange configuration options.
    /// </summary>
    public ExchangeOptions Exchanges { get; set; } = new();

    /// <summary>
    /// Gets or sets the shipping consumer configuration options.
    /// </summary>
    public ShippingConsumerOptions Shipping { get; set; } = new();
}

public class RabbitMqOptions
{
    // BROKER CONFIGURATION: te pola konfigurują konkretny broker RabbitMQ.
    /// <summary>
    /// Gets or sets the hostname of the RabbitMQ server.
    /// </summary>
    public string HostName { get; set; } = "localhost";

    /// <summary>
    /// Gets or sets the port number for the RabbitMQ server.
    /// </summary>
    public int Port { get; set; } = 5672;

    public string UserName { get; set; } = "guest";

    public string Password { get; set; } = "guest";

    /// <summary>
    /// Gets or sets the virtual host for the RabbitMQ server.
    /// </summary>
    public string VirtualHost { get; set; } = "/";
}

public class ExchangeOptions
{
    // EXCHANGE CONFIGURATION: exchange to punkt wejścia dla publish, nie miejsce przechowywania wiadomości.
    /// <summary>
    /// Gets or sets the name of the exchange for WMS events.
    /// </summary>
    public string WmsEvents { get; set; } = "wms.events";

    /// <summary>
    /// Gets or sets the name of the dead-letter exchange for WMS events.
    /// </summary>
    public string DeadLetter { get; set; } = "wms.events.dlx";
}

public class ShippingConsumerOptions
{
    // QUEUE / ROUTING KEY CONFIGURATION:
    // queue przechowuje wiadomości do odbioru,
    // routing key pomaga exchange skierować wiadomość do właściwej queue.
    /// <summary>
    /// Gets or sets the name of the queue for confirmed shipping documents.
    /// </summary>
    public string DocumentConfirmedQueue { get; set; } = "shipping.document-confirmed";

    /// <summary>
    /// Gets or sets the name of the dead-letter queue for confirmed shipping documents.
    /// </summary>
    public string DocumentConfirmedDeadLetterQueue { get; set; } = "shipping.document-confirmed.dlq";

    /// <summary>
    /// Gets or sets the routing key for confirmed shipping documents.
    /// </summary>
    public string DocumentConfirmedRoutingKey { get; set; } = "document.confirmed";

    /// <summary>
    /// Gets or sets the prefetch count for the shipping consumer, which determines how many messages can be fetched from the queue before acknowledging them.
    /// </summary>
    public ushort PrefetchCount { get; set; } = 10;
}
