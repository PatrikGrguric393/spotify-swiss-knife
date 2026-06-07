using System.ComponentModel.DataAnnotations;

namespace spotify_swiss_knife.Models.FormModels;

public class RegisterModel
{
    [Required]
    [EmailAddress]
    [Display(Name = "Email")]
    public string Email { get; set; } = string.Empty;

    [Required]
    [Display(Name = "First name")]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Last name")]
    public string LastName { get; set; } = string.Empty;

    [DataType(DataType.Date)]
    [Display(Name = "Date of birth")]
    public DateOnly? DateOfBirth { get; set; }

    [Required]
    [StringLength(11, MinimumLength = 11, ErrorMessage = "OIB must be exactly 11 digits.")]
    [RegularExpression("^[0-9]*$", ErrorMessage = "OIB may contain digits only.")]
    [Display(Name = "OIB")]
    public string OIB { get; set; } = string.Empty;

    [Required]
    [StringLength(13, MinimumLength = 13, ErrorMessage = "JMBG must be exactly 13 digits.")]
    [RegularExpression("^[0-9]*$", ErrorMessage = "JMBG may contain digits only.")]
    [Display(Name = "JMBG")]
    public string JMBG { get; set; } = string.Empty;

    [Required]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "Password must be at least 6 characters.")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    [Required]
    [DataType(DataType.Password)]
    [Display(Name = "Confirm password")]
    [Compare(nameof(Password), ErrorMessage = "Passwords do not match.")]
    public string ConfirmPassword { get; set; } = string.Empty;
}
