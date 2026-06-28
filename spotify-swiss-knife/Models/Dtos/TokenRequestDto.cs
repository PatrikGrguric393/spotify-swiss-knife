using System.ComponentModel.DataAnnotations;

namespace spotify_swiss_knife.Models.Dtos;

/// <summary>Credentials posted to the app's own token endpoint to obtain a JWT.</summary>
public sealed class TokenRequestDto
{
    [Required]
    public string Username { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;
}
