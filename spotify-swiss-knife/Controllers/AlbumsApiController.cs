using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using spotify_swiss_knife.DAL;
using spotify_swiss_knife.Models;
using spotify_swiss_knife.Models.Dtos;
using spotify_swiss_knife.Services;

namespace spotify_swiss_knife.Controllers;

[ApiController]
[Route("api/albums")]
[Produces("application/json")]
public class AlbumsApiController : ApiControllerBase
{
    private readonly AlbumRepository _albumRepository;
    private readonly SpotifyDbContext _context;

    public AlbumsApiController(AlbumRepository albumRepository, SpotifyDbContext context)
    {
        _albumRepository = albumRepository;
        _context = context;
    }

    [AllowAnonymous]
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<AlbumListDto>), StatusCodes.Status200OK)]
    public ActionResult<IEnumerable<AlbumListDto>> GetAll([FromQuery] string? q)
    {
        var albums = _albumRepository.GetAll();

        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim();
            albums = albums
                .Where(a => a.Id.Equals(term, StringComparison.OrdinalIgnoreCase)
                            || a.Name.Contains(term, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        return Ok(albums.OrderBy(a => a.Name).Select(AlbumListDto.FromEntity));
    }

    [AllowAnonymous]
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(AlbumDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult<AlbumDetailDto> GetById(string id)
    {
        var album = _albumRepository.GetById(id);
        if (album is null) return NotFound();

        return Ok(AlbumDetailDto.FromEntity(album));
    }

    [Authorize(Roles = "Admin,Editor")]
    [HttpPost]
    [ProducesResponseType(typeof(AlbumDetailDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public ActionResult<AlbumDetailDto> Create([FromBody] AlbumCreateDto dto)
    {
        if (!TryValidateSpotifyUrl(dto.SpotifyUrl, out var error))
        {
            ModelState.AddModelError(nameof(dto.SpotifyUrl), error);
            return ValidationProblem(ModelState);
        }

        if (_albumRepository.ExistsByName(dto.Name))
            return UnprocessableEntity(new { message = $"An album named '{dto.Name.Trim()}' already exists." });

        var album = new Album
        {
            Id = Guid.NewGuid().ToString(),
            Name = dto.Name.Trim(),
            AlbumType = dto.AlbumType.Trim(),
            ReleaseDate = dto.ReleaseDate.Trim(),
            ReleaseDatePrecision = "day",
            TotalTracks = 0,
            Label = dto.Label?.Trim(),
            Popularity = dto.Popularity,
            ExternalUrls = new ExternalUrls { Spotify = (dto.SpotifyUrl ?? string.Empty).Trim() }
        };

        if (dto.ArtistIds.Count > 0)
        {
            var artists = _context.Artists.Where(a => dto.ArtistIds.Contains(a.Id)).ToList();
            foreach (var artist in artists) album.Artists.Add(artist);
        }

        _albumRepository.Add(album);

        if (dto.TrackIds.Count > 0)
        {
            var tracks = _context.Tracks.Where(t => dto.TrackIds.Contains(t.Id)).ToList();
            foreach (var track in tracks) track.AlbumId = album.Id;
            album.TotalTracks = tracks.Count;
            _context.SaveChanges();
        }

        var created = _albumRepository.GetById(album.Id) ?? album;
        return CreatedAtAction(nameof(GetById), new { id = album.Id }, AlbumDetailDto.FromEntity(created));
    }

    [Authorize(Roles = "Admin,Editor")]
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(AlbumDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public ActionResult<AlbumDetailDto> Update(string id, [FromBody] AlbumUpdateDto dto)
    {
        var album = _albumRepository.GetById(id);
        if (album is null) return NotFound();

        if (!TryValidateSpotifyUrl(dto.SpotifyUrl, out var error))
        {
            ModelState.AddModelError(nameof(dto.SpotifyUrl), error);
            return ValidationProblem(ModelState);
        }

        if (_albumRepository.ExistsByName(dto.Name, id))
            return UnprocessableEntity(new { message = $"An album named '{dto.Name.Trim()}' already exists." });

        album.Name = dto.Name.Trim();
        album.AlbumType = dto.AlbumType.Trim();
        album.ReleaseDate = dto.ReleaseDate.Trim();
        album.ReleaseDatePrecision = "day";
        album.Label = dto.Label?.Trim();
        album.Popularity = dto.Popularity;
        album.ExternalUrls ??= new ExternalUrls();
        album.ExternalUrls.Spotify = (dto.SpotifyUrl ?? string.Empty).Trim();

        album.Artists.Clear();
        if (dto.ArtistIds.Count > 0)
        {
            var artists = _context.Artists.Where(a => dto.ArtistIds.Contains(a.Id)).ToList();
            foreach (var artist in artists) album.Artists.Add(artist);
        }

        var previousTracks = _context.Tracks.Where(t => t.AlbumId == id).ToList();
        foreach (var track in previousTracks) track.AlbumId = null;

        if (dto.TrackIds.Count > 0)
        {
            var newTracks = _context.Tracks.Where(t => dto.TrackIds.Contains(t.Id)).ToList();
            foreach (var track in newTracks) track.AlbumId = id;
            album.TotalTracks = newTracks.Count;
        }
        else
        {
            album.TotalTracks = 0;
        }

        _context.SaveChanges();

        var updated = _albumRepository.GetById(id) ?? album;
        return Ok(AlbumDetailDto.FromEntity(updated));
    }

    [Authorize(Roles = "Admin")]
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public IActionResult Delete(string id)
    {
        var album = _albumRepository.GetById(id);
        if (album is null) return NotFound();

        _albumRepository.Delete(id);
        return NoContent();
    }
}
