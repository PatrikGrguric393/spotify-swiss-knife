using System.ComponentModel.DataAnnotations;

namespace spotify_swiss_knife.Models.Dtos;

public class TokenRequestDto
{
    [Required]
    public string Username { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;
}
