using System.Globalization;

namespace WarehouseManagementSystem.API.Caching;

/// <summary>
/// Provides methods to normalize various types of values for use in cache keys, ensuring consistent formatting and handling of null values.
/// </summary>
public static class CacheKeyNormalizer
{
    /// <summary>
    /// Normalizes a string value by trimming whitespace and returning a placeholder for null values.
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns>
    public static string NormalizeString(string? value)
    {
        return value?.Trim() ?? "<null>";
    }

    /// <summary>
    /// Normalizes a string value for sorting purposes by trimming whitespace, converting to lowercase, and returning a placeholder for null values.
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns>
    public static string NormalizeSort(string? value)
    {
        return (value?.Trim().ToLowerInvariant()) ?? "<null>";
    }

    /// <summary>
    /// Normalizes a DateTimeOffset value by converting it to UTC and formatting it in ISO 8601 format, returning a placeholder for null values.
    /// </summary>
    /// <param name="value">The DateTimeOffset value to normalize.</param>
    /// <returns>A string representation of the normalized DateTimeOffset value.</returns>
    public static string NormalizeDate(DateTimeOffset? value)
    {
        return value.HasValue
            ? value.Value.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture)
            : "<null>";
    }

    /// <summary>
    /// Normalizes a Guid value by formatting it in the standard "D" format, returning a placeholder for null values.
    /// </summary>
    /// <param name="value">The Guid value to normalize.</param>
    /// <returns>A string representation of the normalized Guid value.</returns>
    public static string NormalizeGuid(Guid? value)
    {
        return value?.ToString("D") ?? "<null>";
    }

    /// <summary>
    /// Normalizes a boolean value by converting it to a lowercase string, returning a placeholder for null values.
    /// </summary>
    /// <param name="value">The boolean value to normalize.</param>
    /// <returns>A string representation of the normalized boolean value.</returns>
    public static string NormalizeBool(bool? value)
    {
        return value.HasValue
            ? value.Value.ToString().ToLowerInvariant()
            : "<null>";
    }
    
    /// <summary>
    /// Normalizes an enum value by converting it to its integer representation, returning a placeholder for null values.
    /// </summary>
    /// <typeparam name="TEnum">The type of the enum.</typeparam>
    /// <param name="value">The enum value to normalize.</param>
    /// <returns>A string representation of the normalized enum value.</returns>
    public static string NormalizeEnum<TEnum>(TEnum? value) where TEnum : struct, Enum
    {
        return value.HasValue
            ? Convert.ToInt32(value.Value, CultureInfo.InvariantCulture).ToString(CultureInfo.InvariantCulture)
            : "<null>";
    }
    
    /// <summary>
    /// Normalizes an integer value by converting it to a string using invariant culture.
    /// </summary>
    /// <param name="value">The integer value to normalize.</param>
    /// <returns>A string representation of the normalized integer value.</returns>
    public static string NormalizeInt(int value)
    {
        return value.ToString(CultureInfo.InvariantCulture);
    }
    
    /// <summary>
    /// Normalizes a decimal value by converting it to a string using invariant culture.
    /// </summary>
    /// <param name="value">The decimal value to normalize.</param>
    /// <returns>A string representation of the normalized decimal value.</returns>
    public static string NormalizeDecimal(decimal value)
    {
        return value.ToString(CultureInfo.InvariantCulture);
    }
}
