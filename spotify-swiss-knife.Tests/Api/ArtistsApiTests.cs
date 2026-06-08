using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using spotify_swiss_knife.Models.Dtos;
using spotify_swiss_knife.Tests.Infrastructure;

namespace spotify_swiss_knife.Tests.Api;

public class ArtistsApiTests : IntegrationTestBase
{
    private const string BaseUrl = "/api/artists";

    public ArtistsApiTests(CustomWebApplicationFactory factory) : base(factory) { }

    private static object ValidPayload(string name = "Created Artist", string? spotifyUrl = null) =>
        new { name, spotifyUrl };

    // ---------- Read ----------

    [Fact]
    public async Task GetAll_ReturnsOkAndIncludesSeededArtist()
    {
        using var scope = NewScope();
        var artist = await SeedData.CreateArtistAsync(Db(scope), "Findable Artist");

        var response = await Client.GetAsync(BaseUrl);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var artists = await response.Content.ReadFromJsonAsync<List<ArtistSummaryDto>>();
        artists!.Should().Contain(a => a.Id == artist.Id && a.Name == "Findable Artist");
    }

    [Fact]
    public async Task GetById_ExistingArtist_ReturnsOkWithArtist()
    {
        using var scope = NewScope();
        var artist = await SeedData.CreateArtistAsync(Db(scope), "Lookup Artist");

        var response = await Client.GetAsync($"{BaseUrl}/{artist.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var dto = await response.Content.ReadFromJsonAsync<ArtistDto>();
        dto!.Id.Should().Be(artist.Id);
        dto.Name.Should().Be("Lookup Artist");
    }

    [Fact]
    public async Task GetById_NonExistentArtist_ReturnsNotFound()
    {
        var response = await Client.GetAsync($"{BaseUrl}/{MissingId}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ---------- Create ----------

    [Fact]
    public async Task Create_ValidArtist_ReturnsCreatedAndPersists()
    {
        var response = await Client.PostAsync(BaseUrl,
            JsonBody(ValidPayload("New Artist", "https://open.spotify.com/artist/xyz")));

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        response.Headers.Location.Should().NotBeNull();

        var dto = await response.Content.ReadFromJsonAsync<ArtistDto>();
        dto!.Name.Should().Be("New Artist");

        using var scope = NewScope();
        (await Db(scope).Artists.FindAsync(dto.Id)).Should().NotBeNull();
    }

    [Fact]
    public async Task Create_MissingName_ReturnsBadRequest()
    {
        var response = await Client.PostAsync(BaseUrl, JsonBody(new { spotifyUrl = (string?)null }));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Create_NonSpotifyUrl_ReturnsBadRequest()
    {
        var response = await Client.PostAsync(BaseUrl,
            JsonBody(ValidPayload("Bad Url Artist", "https://example.com/artist/1")));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Create_DuplicateName_ReturnsUnprocessableEntity()
    {
        using var arrange = NewScope();
        await SeedData.CreateArtistAsync(Db(arrange), "Existing Artist");

        var response = await Client.PostAsync(BaseUrl, JsonBody(ValidPayload("Existing Artist")));

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    // ---------- Update ----------

    [Fact]
    public async Task Update_ExistingArtist_ReturnsOkAndPersistsChanges()
    {
        using var arrange = NewScope();
        var artist = await SeedData.CreateArtistAsync(Db(arrange), "Before Update");

        var response = await Client.PutAsync($"{BaseUrl}/{artist.Id}", JsonBody(ValidPayload("After Update")));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var dto = await response.Content.ReadFromJsonAsync<ArtistDto>();
        dto!.Name.Should().Be("After Update");

        using var assert = NewScope();
        (await Db(assert).Artists.FindAsync(artist.Id))!.Name.Should().Be("After Update");
    }

    [Fact]
    public async Task Update_NonExistentArtist_ReturnsNotFound()
    {
        var response = await Client.PutAsync($"{BaseUrl}/{MissingId}", JsonBody(ValidPayload("Ghost")));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Update_MissingName_ReturnsBadRequest()
    {
        using var arrange = NewScope();
        var artist = await SeedData.CreateArtistAsync(Db(arrange));

        var response = await Client.PutAsync($"{BaseUrl}/{artist.Id}", JsonBody(new { spotifyUrl = (string?)null }));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Update_DuplicateName_ReturnsUnprocessableEntity()
    {
        using var arrange = NewScope();
        await SeedData.CreateArtistAsync(Db(arrange), "Taken Name");
        var target = await SeedData.CreateArtistAsync(Db(arrange), "Original Name");

        var response = await Client.PutAsync($"{BaseUrl}/{target.Id}", JsonBody(ValidPayload("Taken Name")));

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    // ---------- Delete (soft-delete semantics) ----------

    [Fact]
    public async Task Delete_ExistingArtist_ReturnsNoContentAndSoftDeletes()
    {
        using var arrange = NewScope();
        var artist = await SeedData.CreateArtistAsync(Db(arrange), "To Delete");

        var response = await Client.DeleteAsync($"{BaseUrl}/{artist.Id}");
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Default read hides soft-deleted artists.
        (await Client.GetAsync($"{BaseUrl}/{artist.Id}")).StatusCode.Should().Be(HttpStatusCode.NotFound);
        // includeDeleted=true still returns it.
        (await Client.GetAsync($"{BaseUrl}/{artist.Id}?includeDeleted=true")).StatusCode.Should().Be(HttpStatusCode.OK);

        // The row still exists in the database, only flagged as deleted.
        using var assert = NewScope();
        var persisted = await Db(assert).Artists.FindAsync(artist.Id);
        persisted.Should().NotBeNull();
        persisted!.DeletedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task Delete_NonExistentArtist_ReturnsNotFound()
    {
        var response = await Client.DeleteAsync($"{BaseUrl}/{MissingId}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
