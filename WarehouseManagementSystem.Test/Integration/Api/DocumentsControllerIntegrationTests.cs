using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using WarehouseManagementSystem.API.DTO;
using WarehouseManagementSystem.Domain.Enums;

namespace WarehouseManagementSystem.Tests.Integration.Api;

/// <summary>
/// Integration tests for <see cref="WarehouseManagementSystem.API.Controllers.DocumentsController"/>.
/// Uses <see cref="WmsApiFactory"/> to boot the real API stack against a Testcontainers SQL Server.
/// These tests verify HTTP-level contract: routing, auth middleware, validation pipeline, and response shapes.
/// </summary>
[Collection("Api")]
public class DocumentsControllerIntegrationTests : IClassFixture<WmsApiFactory>
{
    private readonly HttpClient _client;

    public DocumentsControllerIntegrationTests(WmsApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetDocuments_WithoutToken_Returns401()
    {
        var response = await _client.GetAsync("/api/v1/documents");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetDocumentById_WithoutToken_Returns401()
    {
        var response = await _client.GetAsync($"/api/v1/documents/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CreateDocument_WithoutToken_Returns401()
    {
        var dto = new CreateDocumentDto
        {
            DocumentDate = DateTime.UtcNow,
            Type = DocumentType.PZ,
            SourceWarehouseId = Guid.NewGuid(),
            Items =
            [
                new DocumentItemCommandDto
                {
                    ProductId = Guid.NewGuid(),
                    Quantity = 10
                }
            ]
        };

        var response = await _client.PostAsJsonAsync("/api/v1/documents", dto);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CreateDocument_WithoutToken_AndInvalidBody_Returns401_NotValidationError()
    {
        var response = await _client.PostAsJsonAsync("/api/v1/documents", new { });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task HealthEndpoint_Returns200()
    {
        var response = await _client.GetAsync("/health");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
