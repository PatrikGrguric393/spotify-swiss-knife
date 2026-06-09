using System.ComponentModel.DataAnnotations;

namespace spotify_swiss_knife.Models.Dtos;

public class RefreshRequestDto
{
    [Required]
    public string RefreshToken { get; set; } = string.Empty;
}
