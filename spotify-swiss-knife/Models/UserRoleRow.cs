namespace spotify_swiss_knife.Models;

/// <summary>
/// Flattened, read-only projection of a user plus their current role, used to render rows in
/// the admin user-management list. Not an EF entity.
/// </summary>
public class UserRoleRow
{
    public string Id { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string OIB { get; set; } = string.Empty;
    public string JMBAG { get; set; } = string.Empty;
    public string CurrentRole { get; set; } = string.Empty;
}
