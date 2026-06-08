using System.ComponentModel.DataAnnotations;

namespace spotify_swiss_knife.Models.FormModels;

public class EditUserModel
{
    public string Id { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
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
    [StringLength(10, MinimumLength = 10, ErrorMessage = "JMBAG must be exactly 10 digits.")]
    [RegularExpression("^[0-9]*$", ErrorMessage = "JMBAG may contain digits only.")]
    [Display(Name = "JMBAG")]
    public string JMBAG { get; set; } = string.Empty;

    [Display(Name = "Role")]
    public string Role { get; set; } = string.Empty;
}
