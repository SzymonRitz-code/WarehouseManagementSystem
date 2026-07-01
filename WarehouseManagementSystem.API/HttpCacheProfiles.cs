namespace WarehouseManagementSystem.API;

public static class HttpCacheProfiles
{
    public const string ReferenceData = nameof(ReferenceData); // czym są reference data? To są dane, które rzadko się zmieniają i są używane jako punkt odniesienia w systemie. Mogą to być np. dane konfiguracyjne, słowniki, listy kategorii itp.
    public const string OperationalData = nameof(OperationalData); // czym są operational data? To są dane, które są używane w codziennych operacjach systemu. Mogą to być np. dane transakcyjne, dane użytkowników, dane zamówień itp.
    public const string VolatileData = nameof(VolatileData); // czym są volatile data? To są dane, które zmieniają się często i szybko tracą swoją aktualność. Mogą to być np. dane sesji użytkownika, dane tymczasowe itp.
    public const string AuditData = nameof(AuditData); // czym są audit data? To są dane związane z audytem i śledzeniem działań w systemie. Mogą to być np. logi operacji, zmiany w danych itp.

    public const int ReferenceDataDuration = 15;
    public const int OperationalDataDuration = 5;
    public const int VolatileDataDuration = 5;
    public const int AuditDataDuration = 15;
}
