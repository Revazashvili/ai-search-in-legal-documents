using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using LegalDocumentAISearch.Application.Interfaces;
using LegalDocumentAISearch.IntegrationTests.Fixtures;
using Microsoft.AspNetCore.Mvc.Testing;
using NSubstitute;

namespace LegalDocumentAISearch.IntegrationTests.Api;

public class DocumentApiTests : IClassFixture<IntegrationTestFixture>
{
    private readonly IntegrationTestFixture _fixture;
    private static int _userCounter;

    public DocumentApiTests(IntegrationTestFixture fixture)
    {
        _fixture = fixture;

        // Configure PDF extractor mock to return valid text so uploads succeed
        fixture.PdfTextExtractor
            .ExtractText(Arg.Any<Stream>(), Arg.Any<string>())
            .Returns("Sample extracted text content from PDF");
    }

    private HttpClient CreateClient()
    {
        return _fixture.Factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = true
        });
    }

    private async Task<HttpClient> GetAuthenticatedClientAsync()
    {
        var client = CreateClient();

        var userId = Interlocked.Increment(ref _userCounter);
        var email = $"testuser{userId}@example.com";
        var password = "Password123!";

        // Register
        var registerPayload = new { email, password };
        var registerResponse = await client.PostAsJsonAsync("/api/admin/register", registerPayload);
        registerResponse.EnsureSuccessStatusCode();

        // Login with cookies
        var loginPayload = new { email, password };
        var loginResponse = await client.PostAsJsonAsync("/api/admin/login?useCookies=true", loginPayload);
        loginResponse.EnsureSuccessStatusCode();

        return client;
    }

    [Fact]
    public async Task GetDocuments_Returns401_WhenNotAuthenticated()
    {
        var client = CreateClient();

        var response = await client.GetAsync("/api/admin/documents");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetDocuments_Returns200_WhenAuthenticated()
    {
        var client = await GetAuthenticatedClientAsync();

        var response = await client.GetAsync("/api/admin/documents");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetDocuments_ReturnsEmptyList_WhenNoDocuments()
    {
        var client = await GetAuthenticatedClientAsync();

        var response = await client.GetAsync("/api/admin/documents");
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(content);

        Assert.Equal(JsonValueKind.Array, doc.RootElement.ValueKind);
    }

    [Fact]
    public async Task PostDocument_Returns202_WithValidMultipartForm()
    {
        var client = await GetAuthenticatedClientAsync();

        using var form = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(Encoding.UTF8.GetBytes("Fake PDF content"));
        fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/pdf");
        form.Add(fileContent, "file", "test.pdf");
        form.Add(new StringContent("Test Upload Title"), "title");
        form.Add(new StringContent("Civil Code"), "sourceLawName");
        form.Add(new StringContent("Law"), "documentType");
        form.Add(new StringContent("FixedSize"), "chunkingStrategy");

        var response = await client.PostAsync("/api/admin/documents", form);

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
    }

    [Fact]
    public async Task GetDocument_Returns404_ForUnknownId()
    {
        var client = await GetAuthenticatedClientAsync();

        var response = await client.GetAsync($"/api/admin/documents/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task DeleteDocument_Returns404_ForUnknownId()
    {
        var client = await GetAuthenticatedClientAsync();

        var response = await client.DeleteAsync($"/api/admin/documents/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
