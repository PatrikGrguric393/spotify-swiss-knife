using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using spotify_swiss_knife.DAL;
using spotify_swiss_knife.Models;
using spotify_swiss_knife.Models.Dtos;
using spotify_swiss_knife.Services;

namespace spotify_swiss_knife.Controllers;

// JSON CRUD API for the local library's tracks (JWT-authenticated; see ApiControllerBase).
// GETs are anonymous; writes require Admin/Editor and deletes require Admin. The server-rendered
// counterpart is TracksController.
[ApiController]
[Route("api/tracks")]
[Produces("application/json")]
public class TracksApiController : ApiControllerBase
{
    private readonly TrackRepository _trackRepository;
    private readonly SpotifyDbContext _db;

    public TracksApiController(TrackRepository trackRepository, SpotifyDbContext db)
    {
        _trackRepository = trackRepository;
        _db = db;
    }

    [AllowAnonymous]
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<TrackListDto>), StatusCodes.Status200OK)]
    public ActionResult<IEnumerable<TrackListDto>> GetAll([FromQuery] string? q)
    {
        var tracks = ApplySearchFilter(_trackRepository.GetAll(), q, t => t.Id, t => t.Name);
        return Ok(tracks.OrderBy(t => t.Name).Select(TrackListDto.FromEntity));
    }

    [AllowAnonymous]
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(TrackDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult<TrackDetailDto> GetById(string id)
    {
        var track = _trackRepository.GetById(id);
        if (track is null) return NotFound();

        return Ok(TrackDetailDto.FromEntity(track));
    }

    [Authorize(Roles = "Admin,Editor")]
    [HttpPost]
    [ProducesResponseType(typeof(TrackDetailDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult<TrackDetailDto> Create([FromBody] TrackCreateDto dto)
    {
        if (SpotifyUrlValidationProblem(dto.SpotifyUrl) is { } problem) return problem;

        if (dto.AlbumId is not null && !_db.Albums.Any(a => a.Id == dto.AlbumId))
            return NotFound(new { message = $"Album '{dto.AlbumId}' not found." });

        var track = new Track
        {
            Id = Guid.NewGuid().ToString("N"),
            Name = dto.Name.Trim(),
            DurationMs = dto.DurationMs,
            DiscNumber = dto.DiscNumber,
            TrackNumber = dto.TrackNumber,
            IsLocal = dto.IsLocal,
            AlbumId = dto.AlbumId,
            ExternalUrls = new ExternalUrls { Spotify = (dto.SpotifyUrl ?? string.Empty).Trim() }
        };

        if (dto.ArtistIds.Count > 0)
        {
            var artists = _db.Artists.Where(a => dto.ArtistIds.Contains(a.Id)).ToList();
            foreach (var artist in artists) track.Artists.Add(artist);
        }

        if (dto.AlbumId is not null)
        {
            var album = _db.Albums.Find(dto.AlbumId);
            if (album is not null)
                album.TotalTracks = _db.Tracks.Count(t => t.AlbumId == dto.AlbumId) + 1;
        }

        _trackRepository.Add(track);

        var created = _trackRepository.GetById(track.Id) ?? track;
        return CreatedAtAction(nameof(GetById), new { id = track.Id }, TrackDetailDto.FromEntity(created));
    }

    [Authorize(Roles = "Admin,Editor")]
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(TrackDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult<TrackDetailDto> Update(string id, [FromBody] TrackUpdateDto dto)
    {
        var track = _trackRepository.GetById(id);
        if (track is null) return NotFound();

        if (SpotifyUrlValidationProblem(dto.SpotifyUrl) is { } problem) return problem;

        if (dto.AlbumId is not null && !_db.Albums.Any(a => a.Id == dto.AlbumId))
            return NotFound(new { message = $"Album '{dto.AlbumId}' not found." });

        var oldAlbumId = track.AlbumId;

        track.Name = dto.Name.Trim();
        track.DurationMs = dto.DurationMs;
        track.DiscNumber = dto.DiscNumber;
        track.TrackNumber = dto.TrackNumber;
        track.IsLocal = dto.IsLocal;
        track.AlbumId = dto.AlbumId;
        track.ExternalUrls ??= new ExternalUrls();
        track.ExternalUrls.Spotify = (dto.SpotifyUrl ?? string.Empty).Trim();

        track.Artists.Clear();
        if (dto.ArtistIds.Count > 0)
        {
            var artists = _db.Artists.Where(a => dto.ArtistIds.Contains(a.Id)).ToList();
            foreach (var artist in artists) track.Artists.Add(artist);
        }

        if (oldAlbumId != dto.AlbumId)
        {
            if (oldAlbumId is not null)
            {
                var oldAlbum = _db.Albums.Find(oldAlbumId);
                if (oldAlbum is not null)
                    oldAlbum.TotalTracks = _db.Tracks.Count(t => t.AlbumId == oldAlbumId && t.Id != id);
            }
            if (dto.AlbumId is not null)
            {
                var newAlbum = _db.Albums.Find(dto.AlbumId);
                if (newAlbum is not null)
                    newAlbum.TotalTracks = _db.Tracks.Count(t => t.AlbumId == dto.AlbumId) + 1;
            }
        }

        _db.SaveChanges();

        var updated = _trackRepository.GetById(id) ?? track;
        return Ok(TrackDetailDto.FromEntity(updated));
    }

    [Authorize(Roles = "Admin")]
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public IActionResult Delete(string id)
    {
        var track = _trackRepository.GetById(id);
        if (track is null) return NotFound();

        _trackRepository.Delete(id);
        return NoContent();
    }
}
