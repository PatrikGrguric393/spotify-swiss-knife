using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace spotify_swiss_knife.Models;

/// <summary>
/// Application user, extending ASP.NET Identity with profile fields. OIB and JMBAG are
/// Croatian national identifiers (an 11-digit personal number and a 10-digit student number)
/// captured during registration.
/// </summary>
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
    [StringLength(10, MinimumLength = 10)]
    [RegularExpression("^[0-9]*$")]
    public string JMBAG { get; set; } = string.Empty;
}
