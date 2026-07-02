namespace WarehouseManagementSystem.API.Caching;

/// <summary>
/// Binds Redis cache connectivity and TTL tuning options from configuration.
/// </summary>
public sealed class RedisCacheOptions
{
    public const string SectionName = "RedisCache";

    public bool Enabled { get; set; } = true;
    public string ConnectionString { get; set; } = "localhost:6379";
    public string InstancePrefix { get; set; } = "wms";
    public int DefaultTtlMinutes { get; set; } = 15;
    public int NegativeTtlSeconds { get; set; } = 30;
    public int TtlJitterSeconds { get; set; } = 30;
    public int ConnectTimeoutMs { get; set; } = 2000;
    public int OperationTimeoutMs { get; set; } = 1500;
}
