using System.Security.Cryptography;
using System.Text;

namespace WarehouseManagementSystem.API.Caching;

public interface ICacheKeyBuilder
{
    /// <summary>
    /// Builds a deterministic cache key from the instance prefix, logical region, contract version, generation and parameters.
    /// </summary>
    /// <param name="instancePrefix">Cache namespace prefix used to isolate the application or environment.</param>
    /// <param name="region">Logical cache region such as products, stocks or documents.</param>
    /// <param name="contractVersion">Version of the cached query contract or payload shape.</param>
    /// <param name="generation">Region generation used to invalidate stale entries without deleting them.</param>
    /// <param name="parameters">Canonical key/value parameters that define the query identity.</param>
    /// <returns>The constructed cache key.</returns>
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
        string instancePrefix,
        string region,
        string contractVersion,
        long generation,
        IReadOnlyDictionary<string, string> parameters)
    {
        var canonical = BuildCanonicalParameters(parameters);
        var hash = ComputeHash(canonical);

        return $"{instancePrefix}:{region}:{contractVersion}:g{generation}:{hash}";
    }

    /// <summary>
    /// Produces a canonical parameter string by sorting keys and joining key/value pairs.
    /// </summary>
    /// <param name="parameters">The parameters to include in the canonical representation.</param>
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
    /// Computes a SHA-256 hash and returns it as lowercase hexadecimal text.
    /// </summary>
    /// <param name="value">The string value to hash.</param>
    /// <returns>A lowercase hexadecimal representation of the hash.</returns>
    private static string ComputeHash(string value)
    {
        var bytes = Encoding.UTF8.GetBytes(value);
        var hashBytes = SHA256.HashData(bytes);

        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }
}
