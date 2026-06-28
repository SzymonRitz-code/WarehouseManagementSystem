using System.Data.Common;
using System.Diagnostics;
using FluentAssertions;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Testcontainers.MsSql;
using WarehouseManagementSystem.API.DTO;
using WarehouseManagementSystem.API.Services.Products.Query;
using WarehouseManagementSystem.API.Services.Stocks.Query;
using WarehouseManagementSystem.API.Services.Documents.Query;
using WarehouseManagementSystem.Domain.Enums;
using WarehouseManagementSystem.Domain.Model.CatalogDomain;
using WarehouseManagementSystem.Domain.Model.DocumentsDomain;
using WarehouseManagementSystem.Domain.Model.InventoryDomain;
using WarehouseManagementSystem.Domain.Model.WarehouseDomain;
using WarehouseManagementSystem.Domain.ValueObjects;
using WarehouseManagementSystem.Infrastructure.Persistence;

namespace WarehouseManagementSystem.Tests.Performance;

/// <summary>
/// Performance tests for query operations in the Warehouse Management System, focusing on ensuring that queries scale efficiently with increasing data volumes and adhere to performance guardrails.
/// </summary>
/// <param name="database">The database fixture providing the test database context.</param>
[Trait("TestType", "Performance")]
[Trait("Category", "QueryPerformance")]
public sealed class QueryPerformanceTests(QueryPerformanceDatabaseFixture database)
    : IClassFixture<QueryPerformanceDatabaseFixture>
{
    #region Constants

    private const int PageSize = 50;
    private const int MeasurementIterations = 7;
    private const int ExpectedPagedListSqlCommands = 2; // Count query + paged data query.

    #endregion

    #region Performance Tests

    /// <summary>
    /// Tests that the ProductListQuery scales efficiently with increasing row counts, using bounded SQL commands and adhering to performance guardrails for median and 95th percentile execution times.
    /// </summary>
    /// <param name="rowCount">The number of rows to seed in the database for the test.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Theory]
    [InlineData(1)]
    [InlineData(100)]
    [InlineData(1_000)]
    [InlineData(10_000)]
    public async Task ProductListQuery_ShouldUseBoundedSqlCommandsAndScaleWithinGuardrail(int rowCount)
    {
        // Arrange
        var databaseName = await database.CreateMigratedDatabaseAsync();
        await using (var seedContext = database.CreateContext(databaseName))
        {
            await SeedProductsAsync(seedContext, rowCount);
        }

        var interceptor = new CommandCounterInterceptor();
        await using var context = database.CreateContext(databaseName, interceptor);
        var service = new ProductQueryService(context);
        var query = new ProductListQuery
        {
            Page = 1,
            PageSize = PageSize,
            Search = "PERF",
            Unit = UnitOfMeasure.Piece,
            IsActive = true,
            SortBy = "name",
            SortDirection = "asc"
        };

        await service.GetProductsPageAsync(query);

        // Act
        interceptor.Reset();
        var result = await service.GetProductsPageAsync(query);
        var commandCount = interceptor.CommandCount;
        interceptor.Reset();
        var timings = await MeasureAsync(() => service.GetProductsPageAsync(query), MeasurementIterations);

        // Assert
        commandCount.Should().BeLessThanOrEqualTo(ExpectedPagedListSqlCommands);
        result.Items.Should().HaveCount(Math.Min(PageSize, rowCount));
        result.TotalItems.Should().Be(rowCount);
        timings.Median.Should().BeLessThan(MedianGuardrail(rowCount));
        timings.P95.Should().BeLessThan(P95Guardrail(rowCount));
    }

    /// <summary>
    /// Tests that the StockListQuery scales efficiently with increasing row counts, using bounded SQL commands and adhering to performance guardrails for median and 95th percentile execution times.
    /// </summary>
    /// <param name="rowCount">The number of rows to seed in the database for the test.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Theory]
    [InlineData(1)]
    [InlineData(100)]
    [InlineData(1_000)]
    [InlineData(10_000)]
    public async Task StockListQuery_ShouldUseBoundedSqlCommandsAndScaleWithinGuardrail(int rowCount)
    {
        // Arrange
        var databaseName = await database.CreateMigratedDatabaseAsync();
        Warehouse warehouse;
        WarehouseZone zone;

        await using (var seedContext = database.CreateContext(databaseName))
        {
            (warehouse, zone) = await SeedStockDataAsync(seedContext, rowCount);
        }

        var interceptor = new CommandCounterInterceptor();
        await using var context = database.CreateContext(databaseName, interceptor);
        var service = new StockQueryService(context);
        var query = new StockListQuery
        {
            Page = 1,
            PageSize = PageSize,
            WarehouseId = warehouse.Id,
            ZoneId = zone.Id,
            Search = "SKU",
            AvailableOnly = true,
            SortBy = "quantityAvailable",
            SortDirection = "desc"
        };

        await service.GetStocksAsync(query);

        // Act
        interceptor.Reset();
        var result = await service.GetStocksAsync(query);
        var commandCount = interceptor.CommandCount;
        interceptor.Reset();
        var timings = await MeasureAsync(() => service.GetStocksAsync(query), MeasurementIterations);

        // Assert
        commandCount.Should().BeLessThanOrEqualTo(ExpectedPagedListSqlCommands);
        result.Items.Should().HaveCount(Math.Min(PageSize, rowCount));
        result.TotalItems.Should().Be(rowCount);
        timings.Median.Should().BeLessThan(MedianGuardrail(rowCount));
        timings.P95.Should().BeLessThan(P95Guardrail(rowCount));
    }

    /// <summary>
    /// Tests that the DocumentListQuery scales efficiently with increasing row counts, using bounded SQL commands and adhering to performance guardrails for median and 95th percentile execution times.
    /// </summary>
    /// <param name="rowCount">The number of rows to seed in the database for the test.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Theory]
    [InlineData(1)]
    [InlineData(100)]
    [InlineData(1_000)]
    [InlineData(10_000)]
    public async Task DocumentListQuery_ShouldUseBoundedSqlCommandsAndScaleWithinGuardrail(int rowCount)
    {
        // Arrange
        var databaseName = await database.CreateMigratedDatabaseAsync();
        Warehouse warehouse;

        await using (var seedContext = database.CreateContext(databaseName))
        {
            warehouse = await SeedDocumentDataAsync(seedContext, rowCount);
        }

        var interceptor = new CommandCounterInterceptor();
        await using var context = database.CreateContext(databaseName, interceptor);
        var service = new DocumentQueryService(context);
        var query = new DocumentListQuery
        {
            Page = 1,
            PageSize = PageSize,
            Search = "PERF",
            Status = DocumentStatus.Confirmed,
            WarehouseId = warehouse.Id,
            SortBy = "documentNumber",
            SortDirection = "desc"
        };

        await service.GetDocumentsPageAsync(query);

        // Act
        interceptor.Reset();
        var result = await service.GetDocumentsPageAsync(query);
        var commandCount = interceptor.CommandCount;
        interceptor.Reset();
        var timings = await MeasureAsync(() => service.GetDocumentsPageAsync(query), MeasurementIterations);

        // Assert
        commandCount.Should().BeLessThanOrEqualTo(ExpectedPagedListSqlCommands);
        result.Items.Should().HaveCount(Math.Min(PageSize, rowCount));
        result.TotalItems.Should().Be(rowCount);
        timings.Median.Should().BeLessThan(MedianGuardrail(rowCount));
        timings.P95.Should().BeLessThan(P95Guardrail(rowCount));
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Measures the execution time of an asynchronous action over a specified number of iterations and returns a summary containing the median and 95th percentile timings.
    /// </summary>
    /// <typeparam name="T">The type of the result returned by the asynchronous action.</typeparam>
    /// <param name="action">The asynchronous action to measure.</param>
    /// <param name="iterations">The number of iterations to perform for measurement.</param>
    /// <returns>A task representing the asynchronous operation, containing the timing summary.</returns>
    private static async Task<TimingSummary> MeasureAsync<T>(
        Func<Task<T>> action,
        int iterations)
    {
        var samples = new List<TimeSpan>(iterations);

        for (var i = 0; i < iterations; i++)
        {
            var stopwatch = Stopwatch.StartNew();
            await action();
            stopwatch.Stop();
            samples.Add(stopwatch.Elapsed);
        }

        return TimingSummary.From(samples);
    }

    /// <summary>
    /// Returns the median guardrail time based on the number of rows, providing a performance threshold for median execution time.
    /// </summary>
    /// <param name="rowCount">The number of rows to consider for determining the guardrail.</param>
    /// <returns>A <see cref="TimeSpan"/> representing the median guardrail time.</returns>
    private static TimeSpan MedianGuardrail(int rowCount)
    {
        // Timing is a secondary guardrail. Command count is the primary deterministic regression signal.
        return rowCount switch
        {
            <= 100 => TimeSpan.FromSeconds(2),
            <= 1_000 => TimeSpan.FromSeconds(4),
            _ => TimeSpan.FromSeconds(8)
        };
    }

    /// <summary>
    /// Returns the 95th percentile guardrail time based on the number of rows, providing a performance threshold for the 95th percentile execution time.
    /// </summary>
    /// <param name="rowCount">The number of rows to consider for determining the guardrail.</param>
    /// <returns>A <see cref="TimeSpan"/> representing the 95th percentile guardrail time.</returns>
    private static TimeSpan P95Guardrail(int rowCount) => MedianGuardrail(rowCount) * 2;

    #endregion

    #region Data Seeding Methods

    /// <summary>
    /// Seeds the database with a specified number of product records for performance testing, creating products with unique SKUs and names, and saving them to the provided database context.
    /// </summary>
    /// <param name="context">The database context to use for seeding products.</param>
    /// <param name="rowCount">The number of product records to create.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    private static async Task SeedProductsAsync(
        WarehouseManagementSystemDbContext context,
        int rowCount)
    {
        var user = CreateUser();
        var products = Enumerable.Range(0, rowCount)
            .Select(index => new Product(
                $"PERF-SKU-{index:D6}",
                $"Performance Product {index:D6}",
                UnitOfMeasure.Piece,
                requiresBatch: false,
                user,
                weight: index % 100,
                volume: index % 50))
            .ToArray();

        context.Products.AddRange(products);
        await context.SaveChangesAsync();
    }

    /// <summary>
    /// Seeds the database with a specified number of stock records for performance testing, creating a warehouse, a zone, and products with associated stock quantities, and saving them to the provided database context.
    /// </summary>
    /// <param name="context">The database context to use for seeding stock data.</param>
    /// <param name="rowCount">The number of stock records to create.</param>
    /// <returns>A task representing the asynchronous operation, containing the created warehouse and zone.</returns>
    private static async Task<(Warehouse Warehouse, WarehouseZone Zone)> SeedStockDataAsync(
        WarehouseManagementSystemDbContext context,
        int rowCount)
    {
        var user = CreateUser();
        var warehouse = new Warehouse("WH-STOCK-PERF", "Stock Performance Warehouse", "Poland", "Warsaw", "Dock 1", user);
        var zone = warehouse.AddZone("PICK", "Picking", TemperatureType.Ambient, true);
        var products = Enumerable.Range(0, rowCount)
            .Select(index => new Product($"SKU-{index:D6}", $"Stock Product {index:D6}", UnitOfMeasure.Piece, false, user))
            .ToArray();
        var stocks = products
            .Select((product, index) =>
            {
                var stock = new Stock(product.Id, warehouse.Id, zone.Id, null, 100 + index);
                var reservedQuantity = index % 10;
                if (reservedQuantity > 0)
                {
                    stock.IncreaseReserved(reservedQuantity);
                }

                return stock;
            })
            .ToArray();

        context.Warehouses.Add(warehouse);
        context.Products.AddRange(products);
        context.Stocks.AddRange(stocks);
        await context.SaveChangesAsync();

        return (warehouse, zone);
    }

    /// <summary>
    /// Seeds the database with a specified number of document records for performance testing, creating a warehouse, a zone, a product, and associated documents with items, and saving them to the provided database context.
    /// </summary>
    /// <param name="context">The database context to use for seeding document data.</param>
    /// <param name="rowCount">The number of document records to create.</param>
    /// <returns>A task representing the asynchronous operation, containing the created warehouse.</returns>
    private static async Task<Warehouse> SeedDocumentDataAsync(
        WarehouseManagementSystemDbContext context,
        int rowCount)
    {
        var user = CreateUser();
        var warehouse = new Warehouse("WH-DOC-PERF", "Document Performance Warehouse", "Poland", "Warsaw", "Dock 2", user);
        var zone = warehouse.AddZone("DOC", "Documents Zone", TemperatureType.Ambient, true);
        var product = new Product("DOC-PERF", "Performance Document Product", UnitOfMeasure.Piece, false, user);
        var documents = Enumerable.Range(0, rowCount)
            .Select(index =>
            {
                var document = new Document(DateTime.UtcNow.AddMinutes(-index), DocumentType.PZ, user, warehouse.Id);
                document.AddItem(new DocumentItem(product.Id, 1 + index % 25, null, null, zone.Id));
                document.SetNumber($"PERF/PZ/{index:D6}");
                document.Confirm(user);
                return document;
            })
            .ToArray();

        context.Warehouses.Add(warehouse);
        context.Products.Add(product);
        context.Documents.AddRange(documents);
        await context.SaveChangesAsync();

        return warehouse;
    }

    private static UserSnapshot CreateUser() => new(
        Guid.Parse("33333333-3333-3333-3333-333333333333"),
        "performance.test@example.com",
        "Performance Tester");

    #endregion
}

#region Database Fixture

/// <summary>
/// Fixture for setting up a SQL Server database in a Docker container for performance testing, providing methods to create and migrate databases, and to create DbContext instances with optional command counting for query performance measurement.
/// </summary>
public sealed class QueryPerformanceDatabaseFixture : IAsyncLifetime
{
    private readonly MsSqlContainer _container = new MsSqlBuilder()
        .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
        .WithPassword("Wms_perf_Strong_Password_123!")
        .Build();

    public async Task InitializeAsync()
    {
        await _container.StartAsync();
    }

    public async Task DisposeAsync()
    {
        await _container.DisposeAsync();
    }

    public async Task<string> CreateMigratedDatabaseAsync()
    {
        var databaseName = $"WmsPerf_{Guid.NewGuid():N}";

        await using var context = CreateContext(databaseName);
        await context.Database.MigrateAsync();

        return databaseName;
    }

    public WarehouseManagementSystemDbContext CreateContext(
        string databaseName,
        CommandCounterInterceptor? interceptor = null)
    {
        var builder = new DbContextOptionsBuilder<WarehouseManagementSystemDbContext>()
            .UseSqlServer(BuildConnectionString(databaseName));

        if (interceptor is not null)
        {
            builder.AddInterceptors(interceptor);
        }

        return new WarehouseManagementSystemDbContext(builder.Options);
    }

    private string BuildConnectionString(string databaseName)
    {
        var builder = new SqlConnectionStringBuilder(_container.GetConnectionString())
        {
            InitialCatalog = databaseName,
            TrustServerCertificate = true
        };

        return builder.ConnectionString;
    }
}

#endregion

#region Command Counter Interceptor

/// <summary>
/// A custom DbCommandInterceptor that counts the number of database commands executed, allowing for performance testing and validation of SQL command usage in query operations.
/// </summary>
public sealed class CommandCounterInterceptor : DbCommandInterceptor
{
    private int _commandCount;

    public int CommandCount => Volatile.Read(ref _commandCount);

    public void Reset()
    {
        Interlocked.Exchange(ref _commandCount, 0);
    }

    public override InterceptionResult<DbDataReader> ReaderExecuting(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<DbDataReader> result)
    {
        CountCommand();
        return base.ReaderExecuting(command, eventData, result);
    }

    public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<DbDataReader> result,
        CancellationToken cancellationToken = default)
    {
        CountCommand();
        return base.ReaderExecutingAsync(command, eventData, result, cancellationToken);
    }

    public override InterceptionResult<object> ScalarExecuting(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<object> result)
    {
        CountCommand();
        return base.ScalarExecuting(command, eventData, result);
    }

    public override ValueTask<InterceptionResult<object>> ScalarExecutingAsync(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<object> result,
        CancellationToken cancellationToken = default)
    {
        CountCommand();
        return base.ScalarExecutingAsync(command, eventData, result, cancellationToken);
    }

    public override InterceptionResult<int> NonQueryExecuting(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<int> result)
    {
        CountCommand();
        return base.NonQueryExecuting(command, eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        CountCommand();
        return base.NonQueryExecutingAsync(command, eventData, result, cancellationToken);
    }

    private void CountCommand()
    {
        Interlocked.Increment(ref _commandCount);
    }
}

#endregion

#region Timing Summary

public sealed record TimingSummary(TimeSpan Median, TimeSpan P95)
{
    public static TimingSummary From(IReadOnlyCollection<TimeSpan> samples)
    {
        if (samples.Count == 0)
        {
            throw new ArgumentException("At least one timing sample is required.", nameof(samples));
        }

        var ordered = samples.OrderBy(x => x).ToArray();
        var median = ordered[ordered.Length / 2];
        var p95Index = (int)Math.Ceiling(ordered.Length * 0.95m) - 1;

        return new TimingSummary(median, ordered[Math.Clamp(p95Index, 0, ordered.Length - 1)]);
    }
}

#endregion
