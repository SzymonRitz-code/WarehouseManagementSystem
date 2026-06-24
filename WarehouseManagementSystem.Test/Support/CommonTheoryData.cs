namespace WarehouseManagementSystem.Tests.Support;

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

public sealed class InvalidWarehouseLocationTestData : TheoryData<string?, string?, string?>
{
    public InvalidWarehouseLocationTestData()
    {
        Add(null, "City", "Address");
        Add("PL", null, "Address");
        Add("PL", "City", null);
    }
}

public sealed class InvalidPositiveDecimalTestData : TheoryData<decimal>
{
    public InvalidPositiveDecimalTestData()
    {
        Add(0m);
        Add(-5m);
    }
}
