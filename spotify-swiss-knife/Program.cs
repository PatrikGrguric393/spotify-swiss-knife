using System.Formats.Tar;
using spotify_swiss_knife.Models;
using spotify_swiss_knife.Services;

var musicData = ExampleMusicData.Create();



// Find all songs that aren't local, will be used for downloading playlists
var nonLocalTracks = musicData.Playlists.First()
    .Tracks
    .Items
    .Where(i => !i.Track.IsLocal)
    .Select(i => i.Track)
    .ToList();

// The complement: used for letting the user know which songs won't be downloaded
var localTracks = musicData.Playlists.First()
    .Tracks
    .Items
    .Where(i => i.Track.IsLocal)
    .Select(i => i.Track)
    .ToList();
var localTrackCount = musicData.Playlists.First()
    .Tracks
    .Items
    .Count(i => i.Track.IsLocal);

// Get song count, maybe will be needed for shuffling
var songCount = musicData.Playlists.First().Tracks.Items.Count();


Console.WriteLine(nonLocalTracks.Count());
Console.WriteLine(localTracks.Count());
Console.WriteLine(songCount);







var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

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
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
