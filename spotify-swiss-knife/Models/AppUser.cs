using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace spotify_swiss_knife.Models;

public class AppUser : IdentityUser
{
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public DateOnly? DateOfBirth { get; set; }

    [Required]
    [StringLength(11, MinimumLength = 11)]
    [RegularExpression("^[0-9]*$")]
    public string OIB { get; set; } = string.Empty;

    [Required]
    [StringLength(13, MinimumLength = 13)]
    [RegularExpression("^[0-9]*$")]
    public string JMBG { get; set; } = string.Empty;
}
