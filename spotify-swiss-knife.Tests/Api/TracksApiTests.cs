using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using spotify_swiss_knife.Models.Dtos;
using spotify_swiss_knife.Tests.Infrastructure;

namespace spotify_swiss_knife.Tests.Api;

public class TracksApiTests : IntegrationTestBase
{
    private const string BaseUrl = "/api/tracks";

    public TracksApiTests(CustomWebApplicationFactory factory) : base(factory) { }

    private static object ValidPayload(string name = "Created Track", string? albumId = null) => new
    {
        name,
        durationMs = 180000,
        discNumber = 1,
        trackNumber = 1,
        isLocal = false,
        albumId,
        artistIds = Array.Empty<string>()
    };

    // ---------- Read ----------

    [Fact]
    public async Task GetAll_ReturnsOkAndIncludesSeededTrack()
    {
        using var scope = NewScope();
        var track = await SeedData.CreateTrackAsync(Db(scope), "Findable Track");

        var response = await Client.GetAsync(BaseUrl);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var tracks = await response.Content.ReadFromJsonAsync<List<TrackSummaryDto>>();
        tracks!.Should().Contain(t => t.Id == track.Id && t.Name == "Findable Track");
    }

    [Fact]
    public async Task GetById_ExistingTrack_ReturnsOkWithTrack()
    {
        using var scope = NewScope();
        var track = await SeedData.CreateTrackAsync(Db(scope), "Lookup Track");

        var response = await Client.GetAsync($"{BaseUrl}/{track.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var dto = await response.Content.ReadFromJsonAsync<TrackDto>();
        dto!.Id.Should().Be(track.Id);
        dto.Name.Should().Be("Lookup Track");
    }

    [Fact]
    public async Task GetById_NonExistentTrack_ReturnsNotFound()
    {
        var response = await Client.GetAsync($"{BaseUrl}/{MissingId}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ---------- Create ----------

    [Fact]
    public async Task Create_ValidTrack_ReturnsCreatedAndPersists()
    {
        var response = await Client.PostAsync(BaseUrl, JsonBody(ValidPayload("Brand New Track")));

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        response.Headers.Location.Should().NotBeNull();

        var dto = await response.Content.ReadFromJsonAsync<TrackDto>();
        dto!.Name.Should().Be("Brand New Track");

        using var scope = NewScope();
        (await Db(scope).Tracks.FindAsync(dto.Id)).Should().NotBeNull();
    }

    [Fact]
    public async Task Create_WithExistingAlbum_LinksAlbum()
    {
        using var arrange = NewScope();
        var album = await SeedData.CreateAlbumAsync(Db(arrange), "Host Album");

        var response = await Client.PostAsync(BaseUrl, JsonBody(ValidPayload("Album Track", album.Id)));

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var dto = await response.Content.ReadFromJsonAsync<TrackDto>();
        dto!.Album!.Id.Should().Be(album.Id);
    }

    [Fact]
    public async Task Create_MissingName_ReturnsBadRequest()
    {
        var response = await Client.PostAsync(BaseUrl,
            JsonBody(new { durationMs = 1000, discNumber = 1, trackNumber = 1 }));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Create_DiscNumberOutOfRange_ReturnsBadRequest()
    {
        var response = await Client.PostAsync(BaseUrl,
            JsonBody(new { name = "Bad Disc", durationMs = 1000, discNumber = 6, trackNumber = 1 }));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Create_TrackNumberOutOfRange_ReturnsBadRequest()
    {
        var response = await Client.PostAsync(BaseUrl,
            JsonBody(new { name = "Bad Number", durationMs = 1000, discNumber = 1, trackNumber = 999 }));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Create_NonSpotifyUrl_ReturnsBadRequest()
    {
        var response = await Client.PostAsync(BaseUrl, JsonBody(new
        {
            name = "Bad Url Track",
            durationMs = 1000,
            discNumber = 1,
            trackNumber = 1,
            spotifyUrl = "https://example.com/track/1"
        }));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Create_NonExistentAlbum_ReturnsNotFound()
    {
        var response = await Client.PostAsync(BaseUrl, JsonBody(ValidPayload("Orphan Track", MissingId)));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ---------- Update ----------

    [Fact]
    public async Task Update_ExistingTrack_ReturnsOkAndPersistsChanges()
    {
        using var arrange = NewScope();
        var track = await SeedData.CreateTrackAsync(Db(arrange), "Before Update");

        var payload = new
        {
            name = "After Update",
            durationMs = 220000,
            discNumber = 1,
            trackNumber = 2,
            isLocal = true,
            albumId = (string?)null,
            artistIds = Array.Empty<string>()
        };

        var response = await Client.PutAsync($"{BaseUrl}/{track.Id}", JsonBody(payload));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var dto = await response.Content.ReadFromJsonAsync<TrackDto>();
        dto!.Name.Should().Be("After Update");
        dto.DurationMs.Should().Be(220000);

        using var assert = NewScope();
        (await Db(assert).Tracks.FindAsync(track.Id))!.Name.Should().Be("After Update");
    }

    [Fact]
    public async Task Update_NonExistentTrack_ReturnsNotFound()
    {
        var response = await Client.PutAsync($"{BaseUrl}/{MissingId}", JsonBody(ValidPayload("Ghost Track")));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Update_WithNonExistentAlbum_ReturnsNotFound()
    {
        using var arrange = NewScope();
        var track = await SeedData.CreateTrackAsync(Db(arrange), "Track");

        var response = await Client.PutAsync($"{BaseUrl}/{track.Id}", JsonBody(ValidPayload("Track", MissingId)));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Update_MissingName_ReturnsBadRequest()
    {
        using var arrange = NewScope();
        var track = await SeedData.CreateTrackAsync(Db(arrange));

        var response = await Client.PutAsync($"{BaseUrl}/{track.Id}",
            JsonBody(new { durationMs = 1000, discNumber = 1, trackNumber = 1 }));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // ---------- Delete ----------

    [Fact]
    public async Task Delete_ExistingTrack_ReturnsNoContentAndRemovesFromDatabase()
    {
        using var arrange = NewScope();
        var track = await SeedData.CreateTrackAsync(Db(arrange), "To Delete");

        var response = await Client.DeleteAsync($"{BaseUrl}/{track.Id}");
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        using var assert = NewScope();
        (await Db(assert).Tracks.FindAsync(track.Id)).Should().BeNull();
    }

    [Fact]
    public async Task Delete_NonExistentTrack_ReturnsNotFound()
    {
        var response = await Client.DeleteAsync($"{BaseUrl}/{MissingId}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
