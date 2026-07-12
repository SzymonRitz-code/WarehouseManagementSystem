namespace WarehouseManagementSystem.Infrastructure.Integration;

public class ProcessedMessage
{
    public Guid Id { get; set; }
    public Guid MessageId { get; set; }
    public string Consumer { get; set; } = null!;
    public string MessageType { get; set; } = null!;
    public Guid? CorrelationId { get; set; }
    public DateTimeOffset ProcessedAt { get; set; }

    // TODO(RECRUITMENT): Ustalić retencje deduplikacji. Bez cleanupu tabela rosnie bez konca; po cleanupie
    // bardzo stary replay moze ponownie wykonac efekt. Okres musi wynikac z okna redelivery/replay albo
    // idempotencje trzeba dodatkowo oprzec o klucz biznesowy.
}
