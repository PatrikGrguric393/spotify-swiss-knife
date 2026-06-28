using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using spotify_swiss_knife.Models.Dtos;
using spotify_swiss_knife.Tests.Infrastructure;

namespace spotify_swiss_knife.Tests.Api;

public class AlbumsApiTests : IntegrationTestBase
{
    private const string BaseUrl = "/api/albums";

    public AlbumsApiTests(CustomWebApplicationFactory factory) : base(factory) { }

    private static object ValidPayload(string name = "Created Album") => new
    {
        name,
        albumType = "album",
        releaseDate = "2023-05-01",
        label = "Label",
        popularity = 50,
        artistIds = Array.Empty<string>(),
        trackIds = Array.Empty<string>()
    };

    // ---------- Read ----------

    [Fact]
    public async Task GetAll_ReturnsOkAndIncludesSeededAlbum()
    {
        using var scope = NewScope();
        var album = await SeedData.CreateAlbumAsync(Db(scope), "Findable Album");

        var response = await Client.GetAsync(BaseUrl);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var albums = await response.Content.ReadFromJsonAsync<List<AlbumListDto>>();
        albums.Should().NotBeNull();
        albums!.Should().Contain(a => a.Id == album.Id && a.Name == "Findable Album");
    }

    [Fact]
    public async Task GetById_ExistingAlbum_ReturnsOkWithAlbum()
    {
        using var scope = NewScope();
        var album = await SeedData.CreateAlbumAsync(Db(scope), "Lookup Album");

        var response = await Client.GetAsync($"{BaseUrl}/{album.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var dto = await response.Content.ReadFromJsonAsync<AlbumDetailDto>();
        dto!.Id.Should().Be(album.Id);
        dto.Name.Should().Be("Lookup Album");
    }

    [Fact]
    public async Task GetById_ReturnsTracksWithNameAndUrl()
    {
        using var scope = NewScope();
        var db = Db(scope);
        var album = await SeedData.CreateAlbumAsync(db, "Track URL Album");
        await SeedData.CreateTrackAsync(db, "Featured Track",
            albumId: album.Id,
            spotifyUrl: "https://open.spotify.com/track/abc123");

        var response = await Client.GetAsync($"{BaseUrl}/{album.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var dto = await response.Content.ReadFromJsonAsync<AlbumDetailDto>();
        var t = dto!.Tracks.Should().ContainSingle().Subject;
        t.Name.Should().Be("Featured Track");
        t.SpotifyUrl.Should().Be("https://open.spotify.com/track/abc123");
    }

    [Fact]
    public async Task GetById_NonExistentAlbum_ReturnsNotFound()
    {
        var response = await Client.GetAsync($"{BaseUrl}/{MissingId}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetAll_WithSearchQuery_ReturnsOnlyMatchingAlbums()
    {
        using var scope = NewScope();
        var match = await SeedData.CreateAlbumAsync(Db(scope), "Random Access Memories");
        await SeedData.CreateAlbumAsync(Db(scope), "Abbey Road");

        var response = await Client.GetAsync($"{BaseUrl}?q=random");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var albums = await response.Content.ReadFromJsonAsync<List<AlbumListDto>>();
        albums!.Should().ContainSingle(a => a.Id == match.Id);
    }

    [Fact]
    public async Task GetAll_WithIdQuery_ReturnsExactMatch()
    {
        using var scope = NewScope();
        var match = await SeedData.CreateAlbumAsync(Db(scope), "Random Access Memories");
        await SeedData.CreateAlbumAsync(Db(scope), "Abbey Road");

        var response = await Client.GetAsync($"{BaseUrl}?q={match.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var albums = await response.Content.ReadFromJsonAsync<List<AlbumListDto>>();
        albums!.Should().ContainSingle(a => a.Id == match.Id);
    }

    // ---------- Authorization ----------

    [Fact]
    public async Task Create_WithoutToken_ReturnsUnauthorized()
    {
        var response = await AnonymousClient().PostAsync(BaseUrl, JsonBody(ValidPayload()));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Update_WithoutToken_ReturnsUnauthorized()
    {
        var response = await AnonymousClient().PutAsync($"{BaseUrl}/{MissingId}", JsonBody(ValidPayload()));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Delete_WithoutToken_ReturnsUnauthorized()
    {
        var response = await AnonymousClient().DeleteAsync($"{BaseUrl}/{MissingId}");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Create_AsInsufficientRole_ReturnsForbidden()
    {
        var client = await CreateClientAsAsync("User");

        var response = await client.PostAsync(BaseUrl, JsonBody(ValidPayload()));

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // ---------- Create ----------

    [Fact]
    public async Task Create_ValidAlbum_ReturnsCreatedAndPersists()
    {
        var response = await Client.PostAsync(BaseUrl, JsonBody(ValidPayload("Brand New Album")));

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        response.Headers.Location.Should().NotBeNull();

        var dto = await response.Content.ReadFromJsonAsync<AlbumDetailDto>();
        dto!.Name.Should().Be("Brand New Album");

        using var scope = NewScope();
        var persisted = await Db(scope).Albums.FindAsync(dto.Id);
        persisted.Should().NotBeNull();
        persisted!.Name.Should().Be("Brand New Album");
    }

    [Fact]
    public async Task Create_LinksProvidedArtists()
    {
        using var arrange = NewScope();
        var artist = await SeedData.CreateArtistAsync(Db(arrange), "Linked Artist");

        var payload = new
        {
            name = "Album With Artist",
            albumType = "album",
            releaseDate = "2023-05-01",
            popularity = 10,
            artistIds = new[] { artist.Id },
            trackIds = Array.Empty<string>()
        };

        var response = await Client.PostAsync(BaseUrl, JsonBody(payload));

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var dto = await response.Content.ReadFromJsonAsync<AlbumDetailDto>();
        dto!.Artists.Should().ContainSingle(a => a.Id == artist.Id);
    }

    [Fact]
    public async Task Create_MissingName_ReturnsBadRequest()
    {
        var response = await Client.PostAsync(BaseUrl,
            JsonBody(new { albumType = "album", releaseDate = "2023-05-01", popularity = 10 }));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Create_InvalidReleaseDateFormat_ReturnsBadRequest()
    {
        var response = await Client.PostAsync(BaseUrl,
            JsonBody(new { name = "Bad Date", albumType = "album", releaseDate = "01/05/2023", popularity = 10 }));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Create_PopularityOutOfRange_ReturnsBadRequest()
    {
        var response = await Client.PostAsync(BaseUrl,
            JsonBody(new { name = "Too Popular", albumType = "album", releaseDate = "2023-05-01", popularity = 150 }));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Create_NonSpotifyUrl_ReturnsBadRequest()
    {
        var response = await Client.PostAsync(BaseUrl, JsonBody(new
        {
            name = "Bad Url",
            albumType = "album",
            releaseDate = "2023-05-01",
            popularity = 10,
            spotifyUrl = "https://example.com/album/1"
        }));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Create_WithNonExistentArtist_ReturnsNotFound()
    {
        var payload = new
        {
            name = "Orphan Artist Album",
            albumType = "album",
            releaseDate = "2023-05-01",
            popularity = 10,
            artistIds = new[] { MissingId },
            trackIds = Array.Empty<string>()
        };

        var response = await Client.PostAsync(BaseUrl, JsonBody(payload));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Create_WithNonExistentTrack_ReturnsNotFound()
    {
        var payload = new
        {
            name = "Orphan Track Album",
            albumType = "album",
            releaseDate = "2023-05-01",
            popularity = 10,
            artistIds = Array.Empty<string>(),
            trackIds = new[] { MissingId }
        };

        var response = await Client.PostAsync(BaseUrl, JsonBody(payload));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Create_DuplicateName_ReturnsUnprocessableEntityAndDoesNotInsert()
    {
        using var arrange = NewScope();
        await SeedData.CreateAlbumAsync(Db(arrange), "Existing Album");

        var response = await Client.PostAsync(BaseUrl, JsonBody(ValidPayload("Existing Album")));

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);

        using var assert = NewScope();
        Db(assert).Albums.Count(a => a.Name == "Existing Album").Should().Be(1);
    }

    // ---------- Update ----------

    [Fact]
    public async Task Update_ExistingAlbum_ReturnsOkAndPersistsChanges()
    {
        using var arrange = NewScope();
        var album = await SeedData.CreateAlbumAsync(Db(arrange), "Before Update");

        var payload = new
        {
            name = "After Update",
            albumType = "single",
            releaseDate = "2024-01-01",
            label = "New Label",
            popularity = 80,
            artistIds = Array.Empty<string>(),
            trackIds = Array.Empty<string>()
        };

        var response = await Client.PutAsync($"{BaseUrl}/{album.Id}", JsonBody(payload));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var dto = await response.Content.ReadFromJsonAsync<AlbumDetailDto>();
        dto!.Name.Should().Be("After Update");
        dto.Popularity.Should().Be(80);

        using var assert = NewScope();
        (await Db(assert).Albums.FindAsync(album.Id))!.Name.Should().Be("After Update");
    }

    [Fact]
    public async Task Update_NonExistentAlbum_ReturnsNotFound()
    {
        var response = await Client.PutAsync($"{BaseUrl}/{MissingId}", JsonBody(ValidPayload("Ghost")));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Update_WithNonExistentArtist_ReturnsNotFound()
    {
        using var arrange = NewScope();
        var album = await SeedData.CreateAlbumAsync(Db(arrange), "Album");

        var payload = new
        {
            name = "Album",
            albumType = "album",
            releaseDate = "2023-05-01",
            popularity = 10,
            artistIds = new[] { MissingId },
            trackIds = Array.Empty<string>()
        };

        var response = await Client.PutAsync($"{BaseUrl}/{album.Id}", JsonBody(payload));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Update_WithNonExistentTrack_ReturnsNotFound()
    {
        using var arrange = NewScope();
        var album = await SeedData.CreateAlbumAsync(Db(arrange), "Album");

        var payload = new
        {
            name = "Album",
            albumType = "album",
            releaseDate = "2023-05-01",
            popularity = 10,
            artistIds = Array.Empty<string>(),
            trackIds = new[] { MissingId }
        };

        var response = await Client.PutAsync($"{BaseUrl}/{album.Id}", JsonBody(payload));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Update_MissingName_ReturnsBadRequest()
    {
        using var arrange = NewScope();
        var album = await SeedData.CreateAlbumAsync(Db(arrange));

        var response = await Client.PutAsync($"{BaseUrl}/{album.Id}",
            JsonBody(new { albumType = "album", releaseDate = "2023-05-01", popularity = 10 }));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Update_DuplicateName_ReturnsUnprocessableEntity()
    {
        using var arrange = NewScope();
        await SeedData.CreateAlbumAsync(Db(arrange), "Taken Name");
        var target = await SeedData.CreateAlbumAsync(Db(arrange), "Original Name");

        var payload = new
        {
            name = "Taken Name",
            albumType = "album",
            releaseDate = "2023-05-01",
            popularity = 10,
            artistIds = Array.Empty<string>(),
            trackIds = Array.Empty<string>()
        };

        var response = await Client.PutAsync($"{BaseUrl}/{target.Id}", JsonBody(payload));

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    // ---------- Delete ----------

    [Fact]
    public async Task Delete_ExistingAlbum_ReturnsNoContentAndRemovesFromDatabase()
    {
        using var arrange = NewScope();
        var album = await SeedData.CreateAlbumAsync(Db(arrange), "To Delete");

        var response = await Client.DeleteAsync($"{BaseUrl}/{album.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        using var assert = NewScope();
        (await Db(assert).Albums.FindAsync(album.Id)).Should().BeNull();
    }

    [Fact]
    public async Task Delete_NonExistentAlbum_ReturnsNotFound()
    {
        var response = await Client.DeleteAsync($"{BaseUrl}/{MissingId}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
