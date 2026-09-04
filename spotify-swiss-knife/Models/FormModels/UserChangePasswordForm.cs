using System.ComponentModel.DataAnnotations;

namespace spotify_swiss_knife.Models.FormModels;

/// <summary>
/// Change-password form: the current password (to re-authenticate the signed-in user) plus the
/// new password and its confirmation (which must match). New-password rules mirror
/// <see cref="UserRegisterForm"/>.
/// </summary>
public sealed class UserChangePasswordForm
{
    [Required]
    [DataType(DataType.Password)]
    [Display(Name = "Current password")]
    public string CurrentPassword { get; set; } = string.Empty;

    [Required]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "Password must be at least 6 characters.")]
    [RegularExpression(@"^(?=.*\d).+$", ErrorMessage = "Password must contain at least one digit.")]
    [DataType(DataType.Password)]
    [Display(Name = "New password")]
    public string NewPassword { get; set; } = string.Empty;

    [Required]
    [DataType(DataType.Password)]
    [Display(Name = "Confirm new password")]
    [Compare(nameof(NewPassword), ErrorMessage = "Passwords do not match.")]
    public string ConfirmNewPassword { get; set; } = string.Empty;
}
