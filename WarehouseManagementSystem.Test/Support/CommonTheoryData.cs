namespace WarehouseManagementSystem.Tests.Support;

/// <summary>
/// Provides test data for invalid required string inputs, including null, empty, and whitespace strings.
/// </summary>
public sealed class InvalidRequiredStringTestData : TheoryData<string?>
{
    public InvalidRequiredStringTestData()
    {
        Add(null);
        Add(string.Empty);
        Add(" ");
        Add("   ");
    }
}

/// <summary>
/// Provides test data for invalid warehouse location inputs, including null values for country, city, and address.
/// </summary>
public sealed class InvalidWarehouseLocationTestData : TheoryData<string?, string?, string?>
{
    public InvalidWarehouseLocationTestData()
    {
        Add(null, "City", "Address");
        Add("PL", null, "Address");
        Add("PL", "City", null);
    }
}

/// <summary>
/// Provides test data for invalid positive decimal inputs, including zero and negative values.
/// </summary>
public sealed class InvalidPositiveDecimalTestData : TheoryData<decimal>
{
    public InvalidPositiveDecimalTestData()
    {
        Add(0m);
        Add(-5m);
    }
}
