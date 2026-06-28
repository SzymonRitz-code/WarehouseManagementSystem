using AutoMapper;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using WarehouseManagementSystem.API.Controllers;
using WarehouseManagementSystem.API.DTO;
using WarehouseManagementSystem.API.Services.Documents.Command;
using WarehouseManagementSystem.API.Services.Documents.Query;
using WarehouseManagementSystem.API.Services.User;
using WarehouseManagementSystem.Domain.Enums;
using WarehouseManagementSystem.Domain.ValueObjects;

namespace WarehouseManagementSystem.Tests.Controllers;

/// <summary>
/// Tests for the <see cref="DocumentsController"/> class in the API controllers.
/// </summary>
public class DocumentsControllerTests
{
    private readonly Mock<IDocumentCommandService> _commandService = new();
    private readonly Mock<IDocumentQueryService> _queryService = new();
    private readonly Mock<IMapper> _mapper = new();
    private readonly Mock<IUserService> _userService = new();
    private readonly Mock<ILogger<DocumentsController>> _logger = new();
    private readonly DocumentsController _controller;

    /// <summary>
    /// Initializes a new instance of the <see cref="DocumentsControllerTests"/> class.
    /// </summary>
    public DocumentsControllerTests()
    {
        _userService
            .Setup(x => x.GetUser(It.IsAny<HttpContext>()))
            .Returns(new UserSnapshot(
                Guid.Parse("33333333-3333-3333-3333-333333333333"),
                "documents.test@example.com",
                "Documents Tester"));

        _controller = new DocumentsController(
            _commandService.Object,
            _queryService.Object,
            _mapper.Object,
            _userService.Object,
            _logger.Object)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };
    }

    /// <summary>
    /// Tests that the GetDocuments method returns a paged result from the query service.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Fact]
    public async Task GetDocuments_ShouldReturnPagedResultFromQueryService()
    {
        // Arrange
        var query = new DocumentListQuery { Page = 1, PageSize = 20 };
        var expected = CreatePagedDocumentsResult(query.Page, query.PageSize);
        _queryService
            .Setup(x => x.GetDocumentsPageAsync(query, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        // Act
        var result = await _controller.GetDocuments(query, CancellationToken.None);

        // Assert
        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeSameAs(expected);
    }
    /// <summary>
    /// Tests that the GetPendingDocuments method forces the status to Draft before querying the service.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Fact]
    public async Task GetPendingDocuments_ShouldForceDraftStatusBeforeQuerying()
    {
        // Arrange
        var query = new DocumentListQuery { Status = DocumentStatus.Confirmed };
        var expected = CreatePagedDocumentsResult(query.Page, query.PageSize);
        _queryService
            .Setup(x => x.GetDocumentsPageAsync(query, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        // Act
        var result = await _controller.GetPendingDocuments(query, CancellationToken.None);

        // Assert
        query.Status.Should().Be(DocumentStatus.Draft);
        result.Result.Should().BeOfType<OkObjectResult>();
    }
    /// <summary>
    /// Tests that the GetByTypeAndStatus method returns a BadRequest result when provided with invalid query values.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Fact]
    public async Task GetByTypeAndStatus_ShouldReturnBadRequest_WhenQueryValuesAreInvalid()
    {
        // Arrange
        const string invalidType = "not-a-type";

        // Act
        var result = await _controller.GetByTypeAndStatus(invalidType, "Draft");

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }
    /// <summary>
    /// Tests that the ConfirmDocument method delegates to the command service and returns a NoContent result.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Fact]
    public async Task ConfirmDocument_ShouldDelegateToCommandServiceAndReturnNoContent()
    {
        // Arrange
        var documentId = Guid.NewGuid();

        // Act
        var result = await _controller.ConfirmDocument(documentId);

        // Assert
        result.Should().BeOfType<NoContentResult>();
        _commandService.Verify(
            x => x.ConfirmDocumentAsync(
                documentId,
                It.Is<UserSnapshot>(u => u.Name == "Documents Tester"),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
    /// <summary>
    /// Creates a paged result of DocumentListDto with the specified page and page size.
    /// </summary>
    /// <param name="page">The page number.</param>
    /// <param name="pageSize">The number of items per page.</param>
    /// <returns>A paged result of DocumentListDto.</returns>
    private static PagedResult<DocumentListDto> CreatePagedDocumentsResult(int page, int pageSize)
    {
        return new()
        {
            Items = [],
            Page = page,
            PageSize = pageSize,
            TotalItems = 0
        };
    }
}
