namespace WarehouseManagementSystem.Tests.Integration.Api;

using Testcontainers.MsSql;

public sealed class ApiFixture : IAsyncLifetime
{
    private readonly MsSqlContainer _sqlContainer = new MsSqlBuilder()
        .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
        .Build();

    public string ConnectionString { get; private set; } = string.Empty;

    public async Task InitializeAsync()
    {
        await _sqlContainer.StartAsync();
        ConnectionString = _sqlContainer.GetConnectionString();
    }

    public async Task DisposeAsync()
    {
        await _sqlContainer.DisposeAsync();
    }
}

[CollectionDefinition("Api", DisableParallelization = true)]
public sealed class ApiCollection : ICollectionFixture<ApiFixture>;
