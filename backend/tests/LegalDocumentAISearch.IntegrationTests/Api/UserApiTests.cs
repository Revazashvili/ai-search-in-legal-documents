using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using LegalDocumentAISearch.IntegrationTests.Fixtures;
using Microsoft.AspNetCore.Mvc.Testing;

namespace LegalDocumentAISearch.IntegrationTests.Api;

public class UserApiTests : IClassFixture<IntegrationTestFixture>
{
    private readonly IntegrationTestFixture _fixture;
    private static int _userCounter;

    public UserApiTests(IntegrationTestFixture fixture)
    {
        _fixture = fixture;
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
        var email = $"useradmin{userId}@example.com";
        var password = "Password123!";

        await client.PostAsJsonAsync("/api/admin/register", new { email, password });
        await client.PostAsJsonAsync("/api/admin/login?useCookies=true", new { email, password });

        return client;
    }

    [Fact]
    public async Task ListUsers_Returns401_WhenNotAuthenticated()
    {
        var client = CreateClient();

        var response = await client.GetAsync("/api/admin/users");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ListUsers_Returns200_WithUsers()
    {
        var client = await GetAuthenticatedClientAsync();

        var response = await client.GetAsync("/api/admin/users");
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(content);
        Assert.Equal(JsonValueKind.Array, doc.RootElement.ValueKind);
        Assert.True(doc.RootElement.GetArrayLength() > 0);
    }

    [Fact]
    public async Task CreateUser_Returns201_WithValidData()
    {
        var client = await GetAuthenticatedClientAsync();

        var newEmail = $"newuser{Interlocked.Increment(ref _userCounter)}@example.com";
        var response = await client.PostAsJsonAsync("/api/admin/users", new
        {
            email = newEmail,
            password = "NewUser123!"
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(content);
        Assert.Equal(newEmail, doc.RootElement.GetProperty("email").GetString());
    }

    [Fact]
    public async Task CreateUser_Returns401_WhenNotAuthenticated()
    {
        var client = CreateClient();

        var response = await client.PostAsJsonAsync("/api/admin/users", new
        {
            email = "unauth@example.com",
            password = "Password123!"
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task CreateUser_ReturnsValidationError_WithWeakPassword()
    {
        var client = await GetAuthenticatedClientAsync();

        var response = await client.PostAsJsonAsync("/api/admin/users", new
        {
            email = $"weak{Interlocked.Increment(ref _userCounter)}@example.com",
            password = "123"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateUser_ReturnsValidationError_WithDuplicateEmail()
    {
        var client = await GetAuthenticatedClientAsync();

        var email = $"dup{Interlocked.Increment(ref _userCounter)}@example.com";
        await client.PostAsJsonAsync("/api/admin/users", new { email, password = "Password123!" });
        var response = await client.PostAsJsonAsync("/api/admin/users", new { email, password = "Password123!" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task DeleteUser_Returns404_ForUnknownId()
    {
        var client = await GetAuthenticatedClientAsync();

        var response = await client.DeleteAsync($"/api/admin/users/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task DeleteUser_Returns204_WhenDeletingAnotherUser()
    {
        var client = await GetAuthenticatedClientAsync();

        // Create a user to delete
        var email = $"todelete{Interlocked.Increment(ref _userCounter)}@example.com";
        var createResponse = await client.PostAsJsonAsync("/api/admin/users", new { email, password = "Password123!" });
        createResponse.EnsureSuccessStatusCode();

        var content = await createResponse.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(content);
        var userId = doc.RootElement.GetProperty("id").GetString();

        var deleteResponse = await client.DeleteAsync($"/api/admin/users/{userId}");

        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);
    }

    [Fact]
    public async Task DeleteUser_Returns400_WhenDeletingSelf()
    {
        var client = await GetAuthenticatedClientAsync();

        // Get current user info
        var infoResponse = await client.GetAsync("/api/admin/manage/info");
        infoResponse.EnsureSuccessStatusCode();

        // List users and find self by email
        var usersResponse = await client.GetAsync("/api/admin/users");
        usersResponse.EnsureSuccessStatusCode();
        var usersContent = await usersResponse.Content.ReadAsStringAsync();

        var infoContent = await infoResponse.Content.ReadAsStringAsync();
        using var infoDoc = JsonDocument.Parse(infoContent);
        var currentEmail = infoDoc.RootElement.GetProperty("email").GetString();

        using var usersDoc = JsonDocument.Parse(usersContent);
        var self = usersDoc.RootElement.EnumerateArray()
            .First(u => u.GetProperty("email").GetString() == currentEmail);
        var selfId = self.GetProperty("id").GetString();

        var deleteResponse = await client.DeleteAsync($"/api/admin/users/{selfId}");

        Assert.Equal(HttpStatusCode.BadRequest, deleteResponse.StatusCode);
    }

    [Fact]
    public async Task ChangePassword_Returns401_WhenNotAuthenticated()
    {
        var client = CreateClient();

        var response = await client.PostAsJsonAsync("/api/admin/manage/info", new
        {
            oldPassword = "old",
            newPassword = "new"
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ChangePassword_Returns200_WithValidPasswords()
    {
        var client = await GetAuthenticatedClientAsync();

        var response = await client.PostAsJsonAsync("/api/admin/manage/info", new
        {
            oldPassword = "Password123!",
            newPassword = "NewPassword456!"
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task ChangePassword_ReturnsError_WithWrongCurrentPassword()
    {
        var client = await GetAuthenticatedClientAsync();

        var response = await client.PostAsJsonAsync("/api/admin/manage/info", new
        {
            oldPassword = "WrongPassword!1",
            newPassword = "NewPassword456!"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
