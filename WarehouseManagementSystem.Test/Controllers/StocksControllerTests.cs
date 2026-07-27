using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using WarehouseManagementSystem.API.Controllers;
using WarehouseManagementSystem.API.DTO;
using WarehouseManagementSystem.API.Services.Stocks.Query;

namespace WarehouseManagementSystem.Tests.Controllers;

/// <summary>
/// Tests for the <see cref="StocksController"/> class in the API controllers.
/// </summary>
public class StocksControllerTests
{
    private readonly Mock<IStockQueryService> _stockQuery = new();
    private readonly StocksController _controller;

    /// <summary>
    /// Initializes a new instance of the <see cref="StocksControllerTests"/> class.
    /// </summary>
    public StocksControllerTests()
    {
        _controller = new StocksController(_stockQuery.Object)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };
    }
    /// <summary>
    /// Tests that the GetStocks method returns a paged result from the query service.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Fact]
    public async Task GetStocks_ShouldReturnPagedResultFromQueryService()
    {
        // Arrange
        var query = new StockListQuery { Page = 2, PageSize = 25 };
        var expected = CreatePagedStocksResult(query.Page, query.PageSize);
        _stockQuery
            .Setup(x => x.GetStocksAsync(query, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        // Act
        var result = await _controller.GetStocks(query, CancellationToken.None);

        // Assert
        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeSameAs(expected);
    }
    /// <summary>
    /// Tests that the GetStock method returns a stock detail from the query service when the stock exists.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Fact]
    public async Task GetStock_ShouldReturnNotFound_WhenStockDoesNotExist()
    {
        // Arrange
        var stockId = Guid.NewGuid();
        _stockQuery
            .Setup(x => x.GetStockDetailsAsync(stockId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((StockDto?)null);

        // Act
        var result = await _controller.GetStock(stockId, CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<NotFoundResult>();
    }
    /// <summary>
    /// Tests that the GetStockAvailability method returns a list of stock availability from the query service.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Fact]
    public async Task GetStockAvailability_ShouldReturnAvailabilityList()
    {
        // Arrange
        var expected = new List<StockDto> { CreateStockDto() };
        _stockQuery
            .Setup(x => x.GetStockAvailabilityAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        // Act
        var result = await _controller.GetStockAvailability(CancellationToken.None);

        // Assert
        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeSameAs(expected);
    }
    /// <summary>
    /// Tests that the GetOptions method sets the Allow header correctly.
    /// </summary>
    [Fact]
    public void GetOptions_ShouldSetAllowHeader()
    {
        // Arrange

        // Act
        var result = _controller.GetOptions();

        // Assert
        result.Should().BeOfType<OkResult>();
        _controller.Response.Headers.Allow.ToString().Should().Be("GET, HEAD, OPTIONS");
    }
    /// <summary>
    /// Creates a paged result of stocks with the specified page and page size.
    /// </summary>
    /// <param name="page">The page number.</param>
    /// <param name="pageSize">The number of items per page.</param>
    /// <returns>A paged result of StockDto.</returns>
    private static PagedResult<StockDto> CreatePagedStocksResult(int page, int pageSize)
    {
        return new()
        {
            Items = [],
            Page = page,
            PageSize = pageSize,
            TotalItems = 0
        };
    }

    /// <summary>
    /// Creates a sample StockDto object for testing purposes.
    /// </summary>
    /// <returns>A sample StockDto object.</returns>
    private static StockDto CreateStockDto()
    {
        return new(
        Guid.NewGuid(),
        null,
        10,
        2,
        8,
        DateTimeOffset.UtcNow,
        Guid.NewGuid(),
        "SKU-001",
        "Packing Tape",
        Guid.NewGuid(),
        "Main Warehouse",
        Guid.NewGuid(),
        "Picking",
        "Piece");
    }
}
