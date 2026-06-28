using System.ComponentModel.DataAnnotations;

namespace spotify_swiss_knife.Models.FormModels;

/// <summary>Login form: email, password, and the "remember me" persistence toggle.</summary>
public sealed class UserLoginForm
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    [Display(Name = "Remember me")]
    public bool RememberMe { get; set; }
}
