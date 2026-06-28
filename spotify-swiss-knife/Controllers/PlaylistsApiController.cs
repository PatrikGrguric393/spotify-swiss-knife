using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using spotify_swiss_knife.DAL;
using spotify_swiss_knife.Models;
using spotify_swiss_knife.Models.Dtos;
using spotify_swiss_knife.Services;

namespace spotify_swiss_knife.Controllers;

// JSON CRUD API for the local library's playlists (JWT-authenticated; see ApiControllerBase).
// GETs are anonymous; writes require Admin/Editor and deletes require Admin. The server-rendered
// counterpart is PlaylistsController.
[ApiController]
[Route("api/playlists")]
[Produces("application/json")]
public class PlaylistsApiController : ApiControllerBase
{
    private readonly PlaylistRepository _playlistRepository;
    private readonly SpotifyDbContext _db;

    public PlaylistsApiController(PlaylistRepository playlistRepository, SpotifyDbContext db)
    {
        _playlistRepository = playlistRepository;
        _db = db;
    }

    [AllowAnonymous]
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<PlaylistListDto>), StatusCodes.Status200OK)]
    public ActionResult<IEnumerable<PlaylistListDto>> GetAll([FromQuery] string? q)
    {
        var playlists = ApplySearchFilter(_playlistRepository.GetAll(), q, p => p.Id, p => p.Name);
        return Ok(playlists.OrderBy(p => p.Name).Select(PlaylistListDto.FromEntity));
    }

    [AllowAnonymous]
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(PlaylistDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult<PlaylistDetailDto> GetById(string id)
    {
        var playlist = _playlistRepository.GetById(id);
        if (playlist is null) return NotFound();

        return Ok(PlaylistDetailDto.FromEntity(playlist));
    }

    [Authorize(Roles = "Admin,Editor")]
    [HttpPost]
    [ProducesResponseType(typeof(PlaylistDetailDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public ActionResult<PlaylistDetailDto> Create([FromBody] PlaylistCreateDto dto)
    {
        if (SpotifyUrlValidationProblem(dto.SpotifyUrl) is { } problem) return problem;

        if (_playlistRepository.ExistsByName(dto.Name))
            return UnprocessableEntity(new { message = $"A playlist named '{dto.Name.Trim()}' already exists." });

        var playlist = new Playlist
        {
            Id = Guid.NewGuid().ToString("N"),
            Name = dto.Name.Trim(),
            Description = dto.Description?.Trim() ?? string.Empty,
            SnapshotId = Guid.NewGuid().ToString("N"),
            ExternalUrls = new ExternalUrls { Spotify = (dto.SpotifyUrl ?? string.Empty).Trim() },
            Owner = new Owner { DisplayName = dto.OwnerDisplayName?.Trim() }
        };

        if (dto.TrackIds.Count > 0)
            playlist.Tracks = BuildTracksPage(dto.TrackIds);

        _playlistRepository.Add(playlist);

        var created = _playlistRepository.GetById(playlist.Id) ?? playlist;
        return CreatedAtAction(nameof(GetById), new { id = playlist.Id }, PlaylistDetailDto.FromEntity(created));
    }

    [Authorize(Roles = "Admin,Editor")]
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(PlaylistDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public ActionResult<PlaylistDetailDto> Update(string id, [FromBody] PlaylistUpdateDto dto)
    {
        var playlist = _playlistRepository.GetById(id);
        if (playlist is null) return NotFound();

        if (SpotifyUrlValidationProblem(dto.SpotifyUrl) is { } problem) return problem;

        if (_playlistRepository.ExistsByName(dto.Name, id))
            return UnprocessableEntity(new { message = $"A playlist named '{dto.Name.Trim()}' already exists." });

        playlist.Name = dto.Name.Trim();
        playlist.Description = dto.Description?.Trim() ?? string.Empty;
        playlist.ExternalUrls ??= new ExternalUrls();
        playlist.ExternalUrls.Spotify = (dto.SpotifyUrl ?? string.Empty).Trim();
        playlist.Owner ??= new Owner();
        playlist.Owner.DisplayName = dto.OwnerDisplayName?.Trim();

        playlist.Tracks = BuildTracksPage(dto.TrackIds);

        _playlistRepository.Save(playlist);

        var updated = _playlistRepository.GetById(id) ?? playlist;
        return Ok(PlaylistDetailDto.FromEntity(updated));
    }

    [Authorize(Roles = "Admin")]
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public IActionResult Delete(string id)
    {
        var playlist = _playlistRepository.GetById(id);
        if (playlist is null) return NotFound();

        _playlistRepository.Delete(id);
        return NoContent();
    }

    private PlaylistTracksPage BuildTracksPage(List<string> trackIds)
    {
        var trackMap = _db.Tracks.Where(t => trackIds.Contains(t.Id)).ToDictionary(t => t.Id);
        var items = trackIds
            .Where(trackMap.ContainsKey)
            .Select(tid => new PlaylistTrack { Track = trackMap[tid] })
            .ToList();
        return new PlaylistTracksPage { Total = items.Count, Limit = items.Count, Offset = 0, Items = items };
    }
}
