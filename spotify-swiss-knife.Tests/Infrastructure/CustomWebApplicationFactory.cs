using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using spotify_swiss_knife.DAL;

namespace spotify_swiss_knife.Tests.Infrastructure;

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
