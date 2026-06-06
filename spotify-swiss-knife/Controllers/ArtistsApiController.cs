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

    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<ArtistSummaryDto>), StatusCodes.Status200OK)]
    public ActionResult<IEnumerable<ArtistSummaryDto>> GetAll(
        [FromQuery] string? q,
        [FromQuery] bool includeDeleted = false)
    {
        var artists = _artistRepository.GetAll(includeDeleted);

        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim();
            artists = artists
                .Where(artist => artist.Name.Contains(term, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        var result = artists
            .OrderBy(artist => artist.Name)
            .Select(ArtistSummaryDto.FromEntity)
            .ToList();

        return Ok(result);
    }

    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ArtistDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult<ArtistDto> GetById(string id, [FromQuery] bool includeDeleted = false)
    {
        var artist = _artistRepository.GetById(id, includeDeleted);
        if (artist is null) return NotFound();

        return Ok(ArtistDto.FromEntity(artist));
    }

    [HttpPost]
    [ProducesResponseType(typeof(ArtistDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public ActionResult<ArtistDto> Create([FromBody] ArtistCreateDto dto)
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
        return CreatedAtAction(nameof(GetById), new { id = artist.Id }, ArtistDto.FromEntity(created));
    }

    [HttpPut("{id}")]
    [ProducesResponseType(typeof(ArtistDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public ActionResult<ArtistDto> Update(string id, [FromBody] ArtistUpdateDto dto)
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
        return Ok(ArtistDto.FromEntity(updated));
    }

    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public IActionResult Delete(string id)
    {
        var artist = _artistRepository.GetById(id);
        if (artist is null) return NotFound();

        _artistRepository.SoftDelete(id);
        return NoContent();
    }
}
