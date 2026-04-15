using System.Formats.Tar;
using spotify_swiss_knife.Models;
using spotify_swiss_knife.Services;

var playlistRepository = new PlaylistMockRepository();
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
builder.Services.AddSingleton<TrackMockRepository>();
builder.Services.AddSingleton<AlbumMockRepository>();
builder.Services.AddSingleton<ArtistMockRepository>();
builder.Services.AddSingleton<PlaylistMockRepository>();

var app = builder.Build();

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

app.UseAuthorization();

app.MapControllerRoute(
    name: "library-songs",
    pattern: "library/songs",
    defaults: new { controller = "Library", action = "Songs" });

app.MapControllerRoute(
    name: "library-albums",
    pattern: "library/albums",
    defaults: new { controller = "Library", action = "Albums" });

app.MapControllerRoute(
    name: "library-artists",
    pattern: "library/artists",
    defaults: new { controller = "Library", action = "Artists" });

app.MapControllerRoute(
    name: "library-playlists",
    pattern: "library/playlists",
    defaults: new { controller = "Library", action = "Playlists" });

app.MapControllerRoute(
    name: "library",
    pattern: "library",
    defaults: new { controller = "Library", action = "Index" });

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
