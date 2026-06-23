using System.ComponentModel.DataAnnotations;

namespace spotify_swiss_knife.Models.Dtos;

/// <summary>Body posted to exchange a valid refresh token for a fresh access token.</summary>
public class RefreshRequestDto
{
    [Required]
    public string RefreshToken { get; set; } = string.Empty;
}
