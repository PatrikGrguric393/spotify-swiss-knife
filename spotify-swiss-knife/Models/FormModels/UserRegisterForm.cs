using System.ComponentModel.DataAnnotations;

namespace spotify_swiss_knife.Models.FormModels;

/// <summary>
/// Registration form: the shared <see cref="UserForm"/> profile fields plus password and its
/// confirmation (which must match).
/// </summary>
public sealed class UserRegisterForm : UserForm
{
    [Required]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "Password must be at least 6 characters.")]
    [RegularExpression(@"^(?=.*\d).+$", ErrorMessage = "Password must contain at least one digit.")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    [Required]
    [DataType(DataType.Password)]
    [Display(Name = "Confirm password")]
    [Compare(nameof(Password), ErrorMessage = "Passwords do not match.")]
    public string ConfirmPassword { get; set; } = string.Empty;
}
