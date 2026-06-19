namespace WarehouseManagementSystem.API;

public static class HttpCacheProfiles
{
    public const string ReferenceData = nameof(ReferenceData);
    public const string OperationalData = nameof(OperationalData);
    public const string VolatileData = nameof(VolatileData);
    public const string AuditData = nameof(AuditData);

    public const int ReferenceDataDuration = 300;
    public const int OperationalDataDuration = 60;
    public const int VolatileDataDuration = 30;
    public const int AuditDataDuration = 120;
}
