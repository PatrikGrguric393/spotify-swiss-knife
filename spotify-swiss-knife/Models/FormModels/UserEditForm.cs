using System.ComponentModel.DataAnnotations;

namespace spotify_swiss_knife.Models.FormModels;

public class UserEditForm : UserForm
{
    public string Id { get; set; } = string.Empty;

    [Display(Name = "Role")]
    public string Role { get; set; } = string.Empty;
}
