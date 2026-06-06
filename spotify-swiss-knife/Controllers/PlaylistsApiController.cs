using Microsoft.AspNetCore.Mvc;
using spotify_swiss_knife.DAL;
using spotify_swiss_knife.Models;
using spotify_swiss_knife.Models.Dtos;
using spotify_swiss_knife.Services;

namespace spotify_swiss_knife.Controllers;

[ApiController]
[Route("api/playlists")]
[Produces("application/json")]
public class PlaylistsApiController : ApiControllerBase
{
    private readonly PlaylistRepository _playlistRepository;
    private readonly SpotifyDbContext _context;

    public PlaylistsApiController(PlaylistRepository playlistRepository, SpotifyDbContext context)
    {
        _playlistRepository = playlistRepository;
        _context = context;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<PlaylistSummaryDto>), StatusCodes.Status200OK)]
    public ActionResult<IEnumerable<PlaylistSummaryDto>> GetAll([FromQuery] string? q)
    {
        var playlists = _playlistRepository.GetAll();

        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim();
            playlists = playlists
                .Where(p => p.Name.Contains(term, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        return Ok(playlists.OrderBy(p => p.Name).Select(PlaylistSummaryDto.FromEntity));
    }

    [HttpGet("{id}")]
    [ProducesResponseType(typeof(PlaylistDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult<PlaylistDto> GetById(string id)
    {
        var playlist = _playlistRepository.GetById(id);
        if (playlist is null) return NotFound();

        return Ok(PlaylistDto.FromEntity(playlist));
    }

    [HttpPost]
    [ProducesResponseType(typeof(PlaylistDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public ActionResult<PlaylistDto> Create([FromBody] PlaylistCreateDto dto)
    {
        if (!TryValidateSpotifyUrl(dto.SpotifyUrl, out var error))
        {
            ModelState.AddModelError(nameof(dto.SpotifyUrl), error);
            return ValidationProblem(ModelState);
        }

        if (_context.Playlists.Any(p => p.Name.Trim().ToLower() == dto.Name.Trim().ToLower()))
            return UnprocessableEntity(new { message = $"A playlist named '{dto.Name.Trim()}' already exists." });

        var playlist = new Playlist
        {
            Id = Guid.NewGuid().ToString(),
            Name = dto.Name.Trim(),
            Description = dto.Description?.Trim() ?? string.Empty,
            SnapshotId = Guid.NewGuid().ToString(),
            ExternalUrls = new ExternalUrls { Spotify = (dto.SpotifyUrl ?? string.Empty).Trim() },
            Owner = new Owner { DisplayName = dto.OwnerDisplayName?.Trim() }
        };

        if (dto.TrackIds.Count > 0)
        {
            var trackMap = _context.Tracks
                .Where(t => dto.TrackIds.Contains(t.Id))
                .ToDictionary(t => t.Id);

            var items = dto.TrackIds
                .Where(trackMap.ContainsKey)
                .Select(tid => new PlaylistTrack { Track = trackMap[tid] })
                .ToList();

            playlist.Tracks = new PlaylistTracksPage
            {
                Total = items.Count,
                Limit = items.Count,
                Offset = 0,
                Items = items
            };
        }

        _playlistRepository.Add(playlist);

        var created = _playlistRepository.GetById(playlist.Id) ?? playlist;
        return CreatedAtAction(nameof(GetById), new { id = playlist.Id }, PlaylistDto.FromEntity(created));
    }

    [HttpPut("{id}")]
    [ProducesResponseType(typeof(PlaylistDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public ActionResult<PlaylistDto> Update(string id, [FromBody] PlaylistUpdateDto dto)
    {
        var playlist = _playlistRepository.GetById(id);
        if (playlist is null) return NotFound();

        if (!TryValidateSpotifyUrl(dto.SpotifyUrl, out var error))
        {
            ModelState.AddModelError(nameof(dto.SpotifyUrl), error);
            return ValidationProblem(ModelState);
        }

        if (_context.Playlists.Any(p => p.Id != id && p.Name.Trim().ToLower() == dto.Name.Trim().ToLower()))
            return UnprocessableEntity(new { message = $"A playlist named '{dto.Name.Trim()}' already exists." });

        playlist.Name = dto.Name.Trim();
        playlist.Description = dto.Description?.Trim() ?? string.Empty;
        playlist.ExternalUrls ??= new ExternalUrls();
        playlist.ExternalUrls.Spotify = (dto.SpotifyUrl ?? string.Empty).Trim();
        playlist.Owner ??= new Owner();
        playlist.Owner.DisplayName = dto.OwnerDisplayName?.Trim();

        var trackMap = _context.Tracks
            .Where(t => dto.TrackIds.Contains(t.Id))
            .ToDictionary(t => t.Id);

        var items = dto.TrackIds
            .Where(trackMap.ContainsKey)
            .Select(tid => new PlaylistTrack { Track = trackMap[tid] })
            .ToList();

        playlist.Tracks = new PlaylistTracksPage
        {
            Total = items.Count,
            Limit = items.Count,
            Offset = 0,
            Items = items
        };

        _playlistRepository.Save(playlist);

        var updated = _playlistRepository.GetById(id) ?? playlist;
        return Ok(PlaylistDto.FromEntity(updated));
    }

    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public IActionResult Delete(string id)
    {
        var playlist = _playlistRepository.GetById(id);
        if (playlist is null) return NotFound();

        _playlistRepository.Delete(id);
        return NoContent();
    }
}
