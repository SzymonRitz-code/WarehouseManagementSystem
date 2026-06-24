using AutoMapper;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using WarehouseManagementSystem.API.Controllers;
using WarehouseManagementSystem.API.DTO;
using WarehouseManagementSystem.API.Services.Documents;
using WarehouseManagementSystem.API.Services.Queries;
using WarehouseManagementSystem.API.Services.User;
using WarehouseManagementSystem.Domain.Enums;
using WarehouseManagementSystem.Domain.ValueObjects;

namespace WarehouseManagementSystem.Tests.Controllers;

public class DocumentsControllerTests
{
    private readonly Mock<IDocumentCommandService> _commandService = new();
    private readonly Mock<IDocumentQueryService> _queryService = new();
    private readonly Mock<IMapper> _mapper = new();
    private readonly Mock<IUserService> _userService = new();
    private readonly Mock<ILogger<DocumentsController>> _logger = new();
    private readonly DocumentsController _controller;

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

    private static PagedResult<DocumentListDto> CreatePagedDocumentsResult(int page, int pageSize) => new()
    {
        Items = [],
        Page = page,
        PageSize = pageSize,
        TotalItems = 0
    };
}
