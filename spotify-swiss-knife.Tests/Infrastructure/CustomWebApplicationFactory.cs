using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using spotify_swiss_knife.DAL;
using spotify_swiss_knife.Services;

namespace spotify_swiss_knife.Tests.Infrastructure;

// Boots the real application for integration tests with two swaps that make it runnable without
// external dependencies: the Npgsql-backed DbContext is replaced with a per-factory in-memory
// EF database, and the Spotify-dependent background scheduler is dropped. Each instance gets its
// own database name so test classes stay isolated.
public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _databaseName = $"ssk-tests-{Guid.NewGuid()}";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureServices(services =>
        {
            // Drop the Npgsql-backed DbContext registration and everything Npgsql pulled in.
            var toRemove = services.Where(d =>
                    d.ServiceType == typeof(DbContextOptions<SpotifyDbContext>) ||
                    d.ServiceType == typeof(DbContextOptions) ||
                    (d.ServiceType.Namespace?.StartsWith("Npgsql", StringComparison.Ordinal) ?? false) ||
                    (d.ImplementationType?.Namespace?.StartsWith("Npgsql", StringComparison.Ordinal) ?? false))
                .ToList();

            foreach (var descriptor in toRemove)
                services.Remove(descriptor);

            services.AddDbContext<SpotifyDbContext>(options =>
                options.UseInMemoryDatabase(_databaseName));

            // The shuffle scheduler is a hosted background service that runs on host
            // startup and resolves SpotifyAuthService, which throws when Spotify
            // credentials are unconfigured (as they are under test). It has no role in
            // the API integration tests, so drop it to keep the test host bootable.
            var scheduler = services.SingleOrDefault(d =>
                d.ImplementationType == typeof(ShuffleSchedulerService));
            if (scheduler is not null)
                services.Remove(scheduler);
        });
    }

    public void ResetDatabase()
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SpotifyDbContext>();
        db.Database.EnsureDeleted();
        db.Database.EnsureCreated();
    }
}
