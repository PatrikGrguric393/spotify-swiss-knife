using Microsoft.AspNetCore.Identity;

namespace spotify_swiss_knife.Models;

public class AppUser : IdentityUser
{
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public DateOnly? DateOfBirth { get; set; }
}
