using System.Security.Cryptography;
using System.Text;

namespace WarehouseManagementSystem.API.Caching;

public interface ICacheKeyBuilder
{
    /// <summary>
    /// Builds a cache key based on the provided parameters.
    /// </summary>
    /// <param name="instancePrefix">The prefix for the cache key, typically identifying the application instance or environment.</param>
    /// <param name="region">The cache region, often representing a geographical area or logical grouping of data.</param>
    /// <param name="contractVersion">The version of the contract or API that defines the structure of the cached data.</param>
    /// <param name="generation">The generation number of the cached data, used for invalidation purposes.</param>
    /// <param name="parameters">Additional parameters that influence the cache key, typically key-value pairs.</param>
    /// <returns>A string representing the constructed cache key.</returns>
    string Build(
        string instancePrefix,
        string region,
        string contractVersion,
        long generation,
        IReadOnlyDictionary<string, string> parameters);
}

public sealed class CacheKeyBuilder : ICacheKeyBuilder
{
    public string Build(
        string instancePrefix, // Prefiks instancji, ktÃ³ry identyfikuje konkretnÄ… instancjÄ™ aplikacji lub Å›rodowisko.
        string region, // czy region to jest region geograficzny? Tak, region odnosi siÄ™ do geograficznego obszaru, w ktÃ³rym dziaÅ‚a aplikacja lub przechowywane sÄ… dane. MoÅ¼e to byÄ‡ np. "us-east-1" dla regionu wschodniego USA.
        string contractVersion, // czy contractVersion to jest wersja kontraktu API? Tak, contractVersion odnosi siÄ™ do wersji kontraktu API lub interfejsu, ktÃ³ry definiuje sposÃ³b komunikacji miÄ™dzy rÃ³Å¼nymi komponentami systemu. MoÅ¼e to byÄ‡ np. "v1", "v2" itp.
        long generation, // czy generation to jest numer generacji danych? Tak, generation odnosi siÄ™ do numeru generacji danych lub wersji danych, ktÃ³re sÄ… przechowywane w pamiÄ™ci podrÄ™cznej. MoÅ¼e to byÄ‡ np. liczba caÅ‚kowita, ktÃ³ra zwiÄ™ksza siÄ™ przy kaÅ¼dej zmianie danych.
        IReadOnlyDictionary<string, string> parameters)
    {
        var canonical = BuildCanonicalParameters(parameters);
        var hash = ComputeHash(canonical);

        return $"{instancePrefix}:{region}:{contractVersion}:g{generation}:{hash}";
    }

    /// <summary>
    /// Builds a canonical string representation of the provided parameters by ordering them and concatenating them in a specific format.
    /// </summary>
    /// <param name="parameters">The parameters to be included in the canonical string.</param>
    /// <returns>A canonical string representation of the parameters.</returns>
    private static string BuildCanonicalParameters(IReadOnlyDictionary<string, string> parameters)
    {
        return string.Join(
            "|",
            parameters
                .OrderBy(x => x.Key, StringComparer.Ordinal)
                .Select(x => $"{x.Key}={x.Value}"));
    }

    /// <summary>
    /// Computes a SHA256 hash of the provided string value and returns it as a lowercase hexadecimal string.
    /// </summary>
    /// <param name="value">The string value to hash.</param>
    /// <returns>A lowercase hexadecimal string representation of the SHA256 hash.</returns>
    private static string ComputeHash(string value)
    {
        var bytes = Encoding.UTF8.GetBytes(value);
        var hashBytes = SHA256.HashData(bytes);

        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }
}
