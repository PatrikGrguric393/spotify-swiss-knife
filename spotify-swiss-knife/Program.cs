using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpLogging;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using spotify_swiss_knife.DAL;
using spotify_swiss_knife.Filters;
using spotify_swiss_knife.Models;
using spotify_swiss_knife;
using spotify_swiss_knife.Services;

var builder = WebApplication.CreateBuilder(args);

// Single-line, UTC-timestamped console output so `docker logs` stays readable.
builder.Logging.AddSimpleConsole(options =>
{
    options.SingleLine = true;
    options.TimestampFormat = "yyyy-MM-dd HH:mm:ss ";
    options.UseUtcTimestamp = true;
});

// One combined log line per request/response. Bodies and headers are intentionally
// omitted to avoid leaking credentials, tokens, or personal data.
builder.Services.AddHttpLogging(options =>
{
    options.LoggingFields = HttpLoggingFields.RequestMethod
        | HttpLoggingFields.RequestPath
        | HttpLoggingFields.RequestQuery
        | HttpLoggingFields.ResponseStatusCode
        | HttpLoggingFields.Duration;
    options.CombineLogs = true;
});

// Add services to the container. AuditActionFilter logs every mutating controller action.
builder.Services.AddControllersWithViews(options => options.Filters.Add<AuditActionFilter>());
builder.Services.Configure<Microsoft.AspNetCore.Http.Features.FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 512 * 1024 * 1024; // 512 MB
});
builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize = 512 * 1024 * 1024; // 512 MB
});
builder.Services.AddDbContext<SpotifyDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("SpotifyDbContext")));

builder.Services.AddDataProtection()
    .PersistKeysToDbContext<SpotifyDbContext>();
builder.Services.AddScoped<TrackRepository>();
builder.Services.AddScoped<AlbumRepository>();
builder.Services.AddScoped<ArtistRepository>();
builder.Services.AddScoped<PlaylistRepository>();
builder.Services.AddSingleton<AlbumCoverStorage>();

builder.Services.AddHttpClient("spotify");
builder.Services.AddScoped<SpotifyAuthService>();
builder.Services.AddSingleton<SigningKeyProvider>();
builder.Services.AddScoped<JwtTokenService>();
builder.Services.AddHostedService<ShuffleSchedulerService>();

builder.Services.AddIdentity<AppUser, IdentityRole>(options =>
    {
        options.Password.RequiredLength = 6;
        options.Password.RequireNonAlphanumeric = false;
        options.User.RequireUniqueEmail = true;
        options.SignIn.RequireConfirmedAccount = false;
    })
    .AddEntityFrameworkStores<SpotifyDbContext>()
    .AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.Name = "SSKAuth";
    options.LoginPath = "/login";
    options.AccessDeniedPath = "/account/denied";
    options.Events.OnRedirectToLogin = ctx =>
    {
        if (ctx.Request.Path.StartsWithSegments("/api"))
        {
            ctx.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return Task.CompletedTask;
        }
        ctx.Response.Redirect(ctx.RedirectUri);
        return Task.CompletedTask;
    };
    options.Events.OnRedirectToAccessDenied = ctx =>
    {
        if (ctx.Request.Path.StartsWithSegments("/api"))
        {
            ctx.Response.StatusCode = StatusCodes.Status403Forbidden;
            return Task.CompletedTask;
        }
        ctx.Response.Redirect(ctx.RedirectUri);
        return Task.CompletedTask;
    };
});

// Spotify OAuth and the API's JWT bearer scheme are both added as separate, optional
// schemes so they do not reset the Identity defaults used for app login and authorization.
builder.Services.AddAuthentication()
    .AddCookie(SpotifyAuthDefaults.Scheme, options =>
    {
        options.Cookie.Name = "SpotifySSK";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        options.SlidingExpiration = false;
    })
    .AddJwtBearer();

// The HS256 signing key is generated once and persisted in the database (see
// SigningKeyProvider), not read from config. Bound via the DI-aware options builder and
// resolved through a key resolver so the key is read lazily — the row is created at
// startup, after the database has migrated.
builder.Services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
    .Configure<SigningKeyProvider>((options, keyProvider) =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"] ?? "spotify-swiss-knife",
            ValidAudience = builder.Configuration["Jwt:Audience"] ?? "spotify-swiss-knife-api",
            IssuerSigningKeyResolver = (_, _, _, _) => new[] { keyProvider.GetSigningKey() },
            ClockSkew = TimeSpan.FromSeconds(30),
            NameClaimType = ClaimTypes.Name,
            RoleClaimType = ClaimTypes.Role,
        };
    });

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<SpotifyDbContext>();
    if (db.Database.IsRelational())
        await db.Database.MigrateAsync();
    else
        await db.Database.EnsureCreatedAsync();

    // Generate and persist the JWT signing key on first run, now that the schema exists.
    scope.ServiceProvider.GetRequiredService<SigningKeyProvider>().GetSigningKey();
}

await IdentitySeeder.SeedAsync(app.Services);

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpLogging();

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();

// Exposed so integration tests can bootstrap the app via WebApplicationFactory<Program>.
public partial class Program { }
