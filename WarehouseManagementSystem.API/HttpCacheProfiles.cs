namespace WarehouseManagementSystem.API;

/// <summary>
/// Defines named HTTP response cache profiles used by controllers to align cache duration with data volatility.
/// </summary>
public static class HttpCacheProfiles
{
    /// <summary>
    /// Cache profile for reference data that changes infrequently and can be reused for longer periods.
    /// </summary>
    public const string ReferenceData = nameof(ReferenceData);

    /// <summary>
    /// Cache profile for operational data that should refresh more frequently than reference data.
    /// </summary>
    public const string OperationalData = nameof(OperationalData);

    /// <summary>
    /// Cache profile for highly volatile data where very short-lived responses still reduce repeated reads.
    /// </summary>
    public const string VolatileData = nameof(VolatileData);

    /// <summary>
    /// Cache profile for audit-oriented data where limited response caching is acceptable despite frequent writes.
    /// </summary>
    public const string AuditData = nameof(AuditData);

    /// <summary>
    /// Cache duration in minutes for reference data responses.
    /// </summary>
    public const int ReferenceDataDuration = 15;

    /// <summary>
    /// Cache duration in minutes for operational data responses.
    /// </summary>
    public const int OperationalDataDuration = 5;

    /// <summary>
    /// Cache duration in minutes for volatile data responses.
    /// </summary>
    public const int VolatileDataDuration = 5;

    /// <summary>
    /// Cache duration in minutes for audit data responses.
    /// </summary>
    public const int AuditDataDuration = 15;
}
