using System.Globalization;

namespace WarehouseManagementSystem.API.Caching;

/// <summary>
/// Normalizes query values into stable cache key fragments so equivalent requests produce identical keys.
/// </summary>
public static class CacheKeyNormalizer
{
    /// <summary>
    /// Trims a string value and maps null to a stable placeholder token.
    /// </summary>
    /// <param name="value">The string value to normalize.</param>
    /// <returns>A normalized string fragment.</returns>
    public static string NormalizeString(string? value)
    {
        return value?.Trim() ?? "<null>";
    }

    /// <summary>
    /// Normalizes sort-related values by trimming whitespace and lowercasing the text.
    /// </summary>
    /// <param name="value">The sort value to normalize.</param>
    /// <returns>A normalized sort fragment.</returns>
    public static string NormalizeSort(string? value)
    {
        return (value?.Trim().ToLowerInvariant()) ?? "<null>";
    }

    /// <summary>
    /// Converts a DateTimeOffset to UTC and formats it in invariant ISO-8601 form.
    /// </summary>
    /// <param name="value">The DateTimeOffset value to normalize.</param>
    /// <returns>A normalized date fragment.</returns>
    public static string NormalizeDate(DateTimeOffset? value)
    {
        return value.HasValue
            ? value.Value.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture)
            : "<null>";
    }

    /// <summary>
    /// Formats a GUID using the canonical D representation.
    /// </summary>
    /// <param name="value">The Guid value to normalize.</param>
    /// <returns>A normalized GUID fragment.</returns>
    public static string NormalizeGuid(Guid? value)
    {
        return value?.ToString("D") ?? "<null>";
    }

    /// <summary>
    /// Formats a nullable boolean as lowercase text and preserves null via a stable placeholder.
    /// </summary>
    /// <param name="value">The boolean value to normalize.</param>
    /// <returns>A normalized boolean fragment.</returns>
    public static string NormalizeBool(bool? value)
    {
        return value.HasValue
            ? value.Value.ToString().ToLowerInvariant()
            : "<null>";
    }

    /// <summary>
    /// Converts an enum to its invariant integer representation.
    /// </summary>
    /// <typeparam name="TEnum">The type of the enum.</typeparam>
    /// <param name="value">The enum value to normalize.</param>
    /// <returns>A normalized enum fragment.</returns>
    public static string NormalizeEnum<TEnum>(TEnum? value) where TEnum : struct, Enum
    {
        return value.HasValue
            ? Convert.ToInt32(value.Value, CultureInfo.InvariantCulture).ToString(CultureInfo.InvariantCulture)
            : "<null>";
    }

    /// <summary>
    /// Formats an integer using invariant culture.
    /// </summary>
    /// <param name="value">The integer value to normalize.</param>
    /// <returns>A normalized integer fragment.</returns>
    public static string NormalizeInt(int value)
    {
        return value.ToString(CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Formats a decimal using invariant culture so key generation is locale independent.
    /// </summary>
    /// <param name="value">The decimal value to normalize.</param>
    /// <returns>A normalized decimal fragment.</returns>
    public static string NormalizeDecimal(decimal value)
    {
        return value.ToString(CultureInfo.InvariantCulture);
    }
}
