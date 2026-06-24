using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using WarehouseManagementSystem.API.Controllers;
using WarehouseManagementSystem.API.DTO;
using WarehouseManagementSystem.API.Services.Queries;

namespace WarehouseManagementSystem.Tests.Controllers;

public class StocksControllerTests
{
    private readonly Mock<IStockQueryService> _stockQuery = new();
    private readonly StocksController _controller;

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

    private static PagedResult<StockDto> CreatePagedStocksResult(int page, int pageSize) => new()
    {
        Items = [],
        Page = page,
        PageSize = pageSize,
        TotalItems = 0
    };

    private static StockDto CreateStockDto() => new(
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
