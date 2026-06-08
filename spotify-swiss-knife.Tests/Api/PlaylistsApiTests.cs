using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using spotify_swiss_knife.Models.Dtos;
using spotify_swiss_knife.Tests.Infrastructure;

namespace spotify_swiss_knife.Tests.Api;

public class PlaylistsApiTests : IntegrationTestBase
{
    private const string BaseUrl = "/api/playlists";

    public PlaylistsApiTests(CustomWebApplicationFactory factory) : base(factory) { }

    private static object ValidPayload(string name = "Created Playlist", string[]? trackIds = null) => new
    {
        name,
        description = "A test playlist",
        ownerDisplayName = "tester",
        trackIds = trackIds ?? Array.Empty<string>()
    };

    // ---------- Read ----------

    [Fact]
    public async Task GetAll_ReturnsOkAndIncludesSeededPlaylist()
    {
        using var scope = NewScope();
        var playlist = await SeedData.CreatePlaylistAsync(Db(scope), "Findable Playlist");

        var response = await Client.GetAsync(BaseUrl);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var playlists = await response.Content.ReadFromJsonAsync<List<PlaylistListDto>>();
        playlists!.Should().Contain(p => p.Id == playlist.Id && p.Name == "Findable Playlist");
    }

    [Fact]
    public async Task GetById_ExistingPlaylist_ReturnsOkWithPlaylist()
    {
        using var scope = NewScope();
        var playlist = await SeedData.CreatePlaylistAsync(Db(scope), "Lookup Playlist");

        var response = await Client.GetAsync($"{BaseUrl}/{playlist.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var dto = await response.Content.ReadFromJsonAsync<PlaylistDetailDto>();
        dto!.Id.Should().Be(playlist.Id);
        dto.Name.Should().Be("Lookup Playlist");
    }

    [Fact]
    public async Task GetById_ExistingPlaylist_ReturnsItsTracksInOrder()
    {
        using var scope = NewScope();
        var db = Db(scope);
        var first = await SeedData.CreateTrackAsync(db, "First Track");
        var second = await SeedData.CreateTrackAsync(db, "Second Track");
        var playlist = await SeedData.CreatePlaylistAsync(db, "Ordered Playlist", first.Id, second.Id);

        var response = await Client.GetAsync($"{BaseUrl}/{playlist.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var dto = await response.Content.ReadFromJsonAsync<PlaylistDetailDto>();
        dto!.Tracks.Select(t => t.Name).Should().Equal("First Track", "Second Track");
    }

    [Fact]
    public async Task GetById_ReturnsTracksWithNameAndUrl()
    {
        using var scope = NewScope();
        var db = Db(scope);
        var track = await SeedData.CreateTrackAsync(db, "Playlist Track",
            spotifyUrl: "https://open.spotify.com/track/xyz789");
        var playlist = await SeedData.CreatePlaylistAsync(db, "URL Playlist", track.Id);

        var response = await Client.GetAsync($"{BaseUrl}/{playlist.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var dto = await response.Content.ReadFromJsonAsync<PlaylistDetailDto>();
        var t = dto!.Tracks.Should().ContainSingle().Subject;
        t.Name.Should().Be("Playlist Track");
        t.SpotifyUrl.Should().Be("https://open.spotify.com/track/xyz789");
    }

    [Fact]
    public async Task GetById_NonExistentPlaylist_ReturnsNotFound()
    {
        var response = await Client.GetAsync($"{BaseUrl}/{MissingId}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ---------- Create ----------

    [Fact]
    public async Task Create_ValidPlaylist_ReturnsCreatedAndPersists()
    {
        var response = await Client.PostAsync(BaseUrl, JsonBody(ValidPayload("Brand New Playlist")));

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        response.Headers.Location.Should().NotBeNull();

        var dto = await response.Content.ReadFromJsonAsync<PlaylistDetailDto>();
        dto!.Name.Should().Be("Brand New Playlist");

        using var scope = NewScope();
        (await Db(scope).Playlists.FindAsync(dto.Id)).Should().NotBeNull();
    }

    [Fact]
    public async Task Create_WithTracks_LinksTracks()
    {
        using var arrange = NewScope();
        var track = await SeedData.CreateTrackAsync(Db(arrange), "Playlist Track");

        var response = await Client.PostAsync(BaseUrl,
            JsonBody(ValidPayload("Tracked Playlist", new[] { track.Id })));

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var dto = await response.Content.ReadFromJsonAsync<PlaylistDetailDto>();
        dto!.Tracks.Should().ContainSingle(t => t.Name == "Playlist Track");
    }

    [Fact]
    public async Task Create_MissingName_ReturnsBadRequest()
    {
        var response = await Client.PostAsync(BaseUrl, JsonBody(new { description = "no name" }));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Create_NameTooLong_ReturnsBadRequest()
    {
        var response = await Client.PostAsync(BaseUrl, JsonBody(ValidPayload(new string('x', 201))));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Create_NonSpotifyUrl_ReturnsBadRequest()
    {
        var response = await Client.PostAsync(BaseUrl, JsonBody(new
        {
            name = "Bad Url Playlist",
            description = "x",
            spotifyUrl = "https://example.com/playlist/1",
            trackIds = Array.Empty<string>()
        }));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Create_DuplicateName_ReturnsUnprocessableEntity()
    {
        using var arrange = NewScope();
        await SeedData.CreatePlaylistAsync(Db(arrange), "Existing Playlist");

        var response = await Client.PostAsync(BaseUrl, JsonBody(ValidPayload("Existing Playlist")));

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    // ---------- Update ----------

    [Fact]
    public async Task Update_ExistingPlaylist_ReturnsOkAndPersistsChanges()
    {
        using var arrange = NewScope();
        var playlist = await SeedData.CreatePlaylistAsync(Db(arrange), "Before Update");

        var payload = new
        {
            name = "After Update",
            description = "Updated description",
            ownerDisplayName = "pg",
            trackIds = Array.Empty<string>()
        };

        var response = await Client.PutAsync($"{BaseUrl}/{playlist.Id}", JsonBody(payload));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var dto = await response.Content.ReadFromJsonAsync<PlaylistDetailDto>();
        dto!.Name.Should().Be("After Update");
        dto.Description.Should().Be("Updated description");

        using var assert = NewScope();
        (await Db(assert).Playlists.FindAsync(playlist.Id))!.Name.Should().Be("After Update");
    }

    [Fact]
    public async Task Update_NonExistentPlaylist_ReturnsNotFound()
    {
        var response = await Client.PutAsync($"{BaseUrl}/{MissingId}", JsonBody(ValidPayload("Ghost")));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Update_MissingName_ReturnsBadRequest()
    {
        using var arrange = NewScope();
        var playlist = await SeedData.CreatePlaylistAsync(Db(arrange));

        var response = await Client.PutAsync($"{BaseUrl}/{playlist.Id}", JsonBody(new { description = "no name" }));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Update_DuplicateName_ReturnsUnprocessableEntity()
    {
        using var arrange = NewScope();
        await SeedData.CreatePlaylistAsync(Db(arrange), "Taken Name");
        var target = await SeedData.CreatePlaylistAsync(Db(arrange), "Original Name");

        var response = await Client.PutAsync($"{BaseUrl}/{target.Id}", JsonBody(ValidPayload("Taken Name")));

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    // ---------- Delete ----------

    [Fact]
    public async Task Delete_ExistingPlaylist_ReturnsNoContentAndRemovesFromDatabase()
    {
        using var arrange = NewScope();
        var playlist = await SeedData.CreatePlaylistAsync(Db(arrange), "To Delete");

        var response = await Client.DeleteAsync($"{BaseUrl}/{playlist.Id}");
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        using var assert = NewScope();
        (await Db(assert).Playlists.FindAsync(playlist.Id)).Should().BeNull();
    }

    [Fact]
    public async Task Delete_NonExistentPlaylist_ReturnsNotFound()
    {
        var response = await Client.DeleteAsync($"{BaseUrl}/{MissingId}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
