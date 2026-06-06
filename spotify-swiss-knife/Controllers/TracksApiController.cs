using Microsoft.AspNetCore.Mvc;
using spotify_swiss_knife.DAL;
using spotify_swiss_knife.Models;
using spotify_swiss_knife.Models.Dtos;
using spotify_swiss_knife.Services;

namespace spotify_swiss_knife.Controllers;

[ApiController]
[Route("api/tracks")]
[Produces("application/json")]
public class TracksApiController : ApiControllerBase
{
    private readonly TrackRepository _trackRepository;
    private readonly SpotifyDbContext _context;

    public TracksApiController(TrackRepository trackRepository, SpotifyDbContext context)
    {
        _trackRepository = trackRepository;
        _context = context;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<TrackSummaryDto>), StatusCodes.Status200OK)]
    public ActionResult<IEnumerable<TrackSummaryDto>> GetAll([FromQuery] string? q)
    {
        var tracks = _trackRepository.GetAll();

        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim();
            tracks = tracks
                .Where(t => t.Name.Contains(term, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        return Ok(tracks.OrderBy(t => t.Name).Select(TrackSummaryDto.FromEntity));
    }

    [HttpGet("{id}")]
    [ProducesResponseType(typeof(TrackDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult<TrackDto> GetById(string id)
    {
        var track = _trackRepository.GetById(id);
        if (track is null) return NotFound();

        return Ok(TrackDto.FromEntity(track));
    }

    [HttpPost]
    [ProducesResponseType(typeof(TrackDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult<TrackDto> Create([FromBody] TrackCreateDto dto)
    {
        if (!TryValidateSpotifyUrl(dto.SpotifyUrl, out var error))
        {
            ModelState.AddModelError(nameof(dto.SpotifyUrl), error);
            return ValidationProblem(ModelState);
        }

        if (dto.AlbumId is not null && !_context.Albums.Any(a => a.Id == dto.AlbumId))
            return NotFound(new { message = $"Album '{dto.AlbumId}' not found." });

        var track = new Track
        {
            Id = Guid.NewGuid().ToString(),
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
            var artists = _context.Artists.Where(a => dto.ArtistIds.Contains(a.Id)).ToList();
            foreach (var artist in artists) track.Artists.Add(artist);
        }

        if (dto.AlbumId is not null)
        {
            var album = _context.Albums.Find(dto.AlbumId);
            if (album is not null)
                album.TotalTracks = _context.Tracks.Count(t => t.AlbumId == dto.AlbumId) + 1;
        }

        _trackRepository.Add(track);

        var created = _trackRepository.GetById(track.Id) ?? track;
        return CreatedAtAction(nameof(GetById), new { id = track.Id }, TrackDto.FromEntity(created));
    }

    [HttpPut("{id}")]
    [ProducesResponseType(typeof(TrackDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult<TrackDto> Update(string id, [FromBody] TrackUpdateDto dto)
    {
        var track = _trackRepository.GetById(id);
        if (track is null) return NotFound();

        if (!TryValidateSpotifyUrl(dto.SpotifyUrl, out var error))
        {
            ModelState.AddModelError(nameof(dto.SpotifyUrl), error);
            return ValidationProblem(ModelState);
        }

        if (dto.AlbumId is not null && !_context.Albums.Any(a => a.Id == dto.AlbumId))
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
            var artists = _context.Artists.Where(a => dto.ArtistIds.Contains(a.Id)).ToList();
            foreach (var artist in artists) track.Artists.Add(artist);
        }

        if (oldAlbumId != dto.AlbumId)
        {
            if (oldAlbumId is not null)
            {
                var oldAlbum = _context.Albums.Find(oldAlbumId);
                if (oldAlbum is not null)
                    oldAlbum.TotalTracks = _context.Tracks.Count(t => t.AlbumId == oldAlbumId && t.Id != id);
            }
            if (dto.AlbumId is not null)
            {
                var newAlbum = _context.Albums.Find(dto.AlbumId);
                if (newAlbum is not null)
                    newAlbum.TotalTracks = _context.Tracks.Count(t => t.AlbumId == dto.AlbumId) + 1;
            }
        }

        _context.SaveChanges();

        var updated = _trackRepository.GetById(id) ?? track;
        return Ok(TrackDto.FromEntity(updated));
    }

    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public IActionResult Delete(string id)
    {
        var track = _trackRepository.GetById(id);
        if (track is null) return NotFound();

        _trackRepository.Delete(id);
        return NoContent();
    }
}
