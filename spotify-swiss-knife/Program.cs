using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using spotify_swiss_knife.DAL;
using spotify_swiss_knife.Models;
using spotify_swiss_knife.Services;

var playlistRepository = new PlaylistRepository();
var musicData = playlistRepository.GetAll();



// Find all songs that aren't local, will be used for downloading playlists
var nonLocalTracks = musicData.First()
    .Tracks
    .Items
    .Where(i => !i.Track.IsLocal)
    .Select(i => i.Track)
    .ToList();

// The complement: used for letting the user know which songs won't be downloaded
var localTracks = musicData.First()
    .Tracks
    .Items
    .Where(i => i.Track.IsLocal)
    .Select(i => i.Track)
    .ToList();
var localTrackCount = musicData.First()
    .Tracks
    .Items
    .Count(i => i.Track.IsLocal);

// Get song count, maybe will be needed for shuffling
var songCount = musicData.First().Tracks.Items.Count();


Console.WriteLine(nonLocalTracks.Count());
Console.WriteLine(localTracks.Count());
Console.WriteLine(songCount);







var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddDbContext<SpotifyDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("SpotifyDbContext")));
builder.Services.AddScoped<TrackRepository>();
builder.Services.AddScoped<AlbumRepository>();
builder.Services.AddScoped<ArtistRepository>();
builder.Services.AddScoped<PlaylistRepository>();

builder.Services.AddHttpClient("spotify");
builder.Services.AddScoped<SpotifyAuthService>();

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
    options.LoginPath = "/account/login";
    options.AccessDeniedPath = "/account/denied";
});

// Spotify OAuth is kept as a separate, optional connection on its own scheme so it
// does not reset the Identity defaults used for app login and authorization.
builder.Services.AddAuthentication().AddCookie("SpotifyConnect", options =>
{
    options.Cookie.Name = "SpotifySSK";
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    options.SlidingExpiration = false;
});

var app = builder.Build();

await IdentitySeeder.SeedAsync(app.Services);

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.MapControllerRoute(
    name: "shuffle",
    pattern: "shuffle",
    defaults: new { controller = "Services", action = "ShufflePlaylist" });

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
