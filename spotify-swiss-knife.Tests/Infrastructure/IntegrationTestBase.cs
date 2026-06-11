using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using spotify_swiss_knife.DAL;
using spotify_swiss_knife.Models.Dtos;

namespace spotify_swiss_knife.Tests.Infrastructure;

/// <summary>
/// Shared setup for the API integration tests. One application is booted per test
/// class (IClassFixture) and the database is reset before each test so tests are
/// independent of execution order.
///
/// The CRUD write endpoints require a JWT bearer token, so <see cref="Client"/> is
/// authenticated as an Admin before each test and is the default for exercising them.
/// Tests that assert the authorization rules themselves use <see cref="AnonymousClient"/>
/// or <see cref="CreateClientAsAsync"/> to act as an anonymous or lesser-privileged caller.
/// </summary>
public abstract class IntegrationTestBase : IClassFixture<CustomWebApplicationFactory>, IAsyncLifetime
{
    // Satisfies the Identity password policy (length, upper, lower, digit) configured in Program.cs.
    private const string TestPassword = "Passw0rd!";

    protected readonly CustomWebApplicationFactory Factory;

    /// <summary>HTTP client authenticated as an Admin — the default for driving the CRUD endpoints.</summary>
    protected HttpClient Client { get; private set; } = default!;

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    protected IntegrationTestBase(CustomWebApplicationFactory factory)
    {
        Factory = factory;
    }

    public async Task InitializeAsync()
    {
        Factory.ResetDatabase();
        Client = await CreateClientAsAsync("Admin");
    }

    public Task DisposeAsync() => Task.CompletedTask;

    /// <summary>
    /// Opens a scope on the application's real service provider so a test can seed
    /// Arrange data or assert on database state through the same DbContext the API uses.
    /// </summary>
    protected IServiceScope NewScope() => Factory.Services.CreateScope();

    protected static SpotifyDbContext Db(IServiceScope scope) =>
        scope.ServiceProvider.GetRequiredService<SpotifyDbContext>();

    /// <summary>A client carrying no bearer token, for asserting endpoints reject anonymous callers.</summary>
    protected HttpClient AnonymousClient() =>
        Factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

    /// <summary>
    /// Creates a user in <paramref name="role"/>, logs in through the real /api/auth/token
    /// endpoint, and returns a client carrying that user's bearer token.
    /// </summary>
    protected async Task<HttpClient> CreateClientAsAsync(string role)
    {
        string username;
        using (var scope = NewScope())
            username = await SeedData.CreateUserAsync(scope, role, TestPassword);

        var client = AnonymousClient();
        var response = await client.PostAsync("/api/auth/token",
            JsonBody(new { username, password = TestPassword }));
        response.EnsureSuccessStatusCode();

        var token = await response.Content.ReadFromJsonAsync<TokenResponseDto>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token!.AccessToken);
        return client;
    }

    protected static StringContent JsonBody(object payload) =>
        new(JsonSerializer.Serialize(payload, JsonOptions), Encoding.UTF8, "application/json");

    /// <summary>A GUID-shaped id guaranteed not to exist in the database.</summary>
    protected static string MissingId => Guid.NewGuid().ToString();
}
