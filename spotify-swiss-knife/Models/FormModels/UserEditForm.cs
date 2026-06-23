using System.ComponentModel.DataAnnotations;

namespace spotify_swiss_knife.Models.FormModels;

/// <summary>
/// Admin user-edit form: the shared <see cref="UserForm"/> profile fields plus the user id and
/// their assigned role.
/// </summary>
public class UserEditForm : UserForm
{
    public string Id { get; set; } = string.Empty;

    [Display(Name = "Role")]
    public string Role { get; set; } = string.Empty;
}
