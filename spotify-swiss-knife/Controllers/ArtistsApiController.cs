using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using spotify_swiss_knife.Models;
using spotify_swiss_knife.Models.Dtos;
using spotify_swiss_knife.Services;

namespace spotify_swiss_knife.Controllers;

[ApiController]
[Route("api/artists")]
[Produces("application/json")]
public class ArtistsApiController : ApiControllerBase
{
    private readonly ArtistRepository _artistRepository;

    public ArtistsApiController(ArtistRepository artistRepository)
    {
        _artistRepository = artistRepository;
    }

    [AllowAnonymous]
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<ArtistListDto>), StatusCodes.Status200OK)]
    public ActionResult<IEnumerable<ArtistListDto>> GetAll(
        [FromQuery] string? q,
        [FromQuery] bool includeDeleted = false)
    {
        var artists = _artistRepository.GetAll(includeDeleted);

        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim();
            artists = artists
                .Where(artist => artist.Id.Equals(term, StringComparison.OrdinalIgnoreCase)
                                 || artist.Name.Contains(term, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        var result = artists
            .OrderBy(artist => artist.Name)
            .Select(ArtistListDto.FromEntity)
            .ToList();

        return Ok(result);
    }

    [AllowAnonymous]
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ArtistDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult<ArtistDetailDto> GetById(string id, [FromQuery] bool includeDeleted = false)
    {
        var artist = _artistRepository.GetById(id, includeDeleted);
        if (artist is null) return NotFound();

        return Ok(ArtistDetailDto.FromEntity(artist));
    }

    [Authorize(Roles = "Admin,Editor")]
    [HttpPost]
    [ProducesResponseType(typeof(ArtistDetailDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public ActionResult<ArtistDetailDto> Create([FromBody] ArtistCreateDto dto)
    {
        if (!TryValidateSpotifyUrl(dto.SpotifyUrl, out var error))
        {
            ModelState.AddModelError(nameof(dto.SpotifyUrl), error);
            return ValidationProblem(ModelState);
        }

        if (_artistRepository.ExistsByName(dto.Name))
        {
            return UnprocessableEntity(new { message = $"An artist named '{dto.Name.Trim()}' already exists." });
        }

        var artist = new Artist
        {
            Id = Guid.NewGuid().ToString(),
            Name = dto.Name.Trim(),
            ExternalUrls = new ExternalUrls { Spotify = (dto.SpotifyUrl ?? string.Empty).Trim() }
        };

        _artistRepository.Add(artist);

        var created = _artistRepository.GetById(artist.Id) ?? artist;
        return CreatedAtAction(nameof(GetById), new { id = artist.Id }, ArtistDetailDto.FromEntity(created));
    }

    [Authorize(Roles = "Admin,Editor")]
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(ArtistDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public ActionResult<ArtistDetailDto> Update(string id, [FromBody] ArtistUpdateDto dto)
    {
        var artist = _artistRepository.GetById(id, includeDeleted: true);
        if (artist is null) return NotFound();

        if (!TryValidateSpotifyUrl(dto.SpotifyUrl, out var error))
        {
            ModelState.AddModelError(nameof(dto.SpotifyUrl), error);
            return ValidationProblem(ModelState);
        }

        if (_artistRepository.ExistsByName(dto.Name, id))
        {
            return UnprocessableEntity(new { message = $"An artist named '{dto.Name.Trim()}' already exists." });
        }

        artist.Name = dto.Name.Trim();
        artist.ExternalUrls ??= new ExternalUrls();
        artist.ExternalUrls.Spotify = (dto.SpotifyUrl ?? string.Empty).Trim();

        _artistRepository.Update(artist);

        var updated = _artistRepository.GetById(id, includeDeleted: true) ?? artist;
        return Ok(ArtistDetailDto.FromEntity(updated));
    }

    [Authorize(Roles = "Admin,Editor")]
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public IActionResult Delete(string id)
    {
        var artist = _artistRepository.GetById(id);
        if (artist is null) return NotFound();

        _artistRepository.SoftDelete(id);
        return NoContent();
    }
}
