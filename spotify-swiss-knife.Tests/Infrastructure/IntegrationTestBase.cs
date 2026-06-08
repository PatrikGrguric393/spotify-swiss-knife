using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using spotify_swiss_knife.DAL;

namespace spotify_swiss_knife.Tests.Infrastructure;

/// <summary>
/// Shared setup for the API integration tests. One application is booted per test
/// class (IClassFixture) and the database is reset before each test so tests are
/// independent of execution order.
/// </summary>
public abstract class IntegrationTestBase : IClassFixture<CustomWebApplicationFactory>
{
    protected readonly CustomWebApplicationFactory Factory;
    protected readonly HttpClient Client;

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    protected IntegrationTestBase(CustomWebApplicationFactory factory)
    {
        Factory = factory;
        Factory.ResetDatabase();
        Client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
    }

    /// <summary>
    /// Opens a scope on the application's real service provider so a test can seed
    /// Arrange data or assert on database state through the same DbContext the API uses.
    /// </summary>
    protected IServiceScope NewScope() => Factory.Services.CreateScope();

    protected static SpotifyDbContext Db(IServiceScope scope) =>
        scope.ServiceProvider.GetRequiredService<SpotifyDbContext>();

    protected static StringContent JsonBody(object payload) =>
        new(JsonSerializer.Serialize(payload, JsonOptions), Encoding.UTF8, "application/json");

    /// <summary>A GUID-shaped id guaranteed not to exist in the database.</summary>
    protected static string MissingId => Guid.NewGuid().ToString();
}
