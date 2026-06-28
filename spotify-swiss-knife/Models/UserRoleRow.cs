namespace spotify_swiss_knife.Models;

/// <summary>
/// Flattened, read-only projection of a user plus their current role, used to render rows in
/// the admin user-management list. Not an EF entity.
/// </summary>
public sealed record UserRoleRow
{
    public string Id { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string FullName { get; init; } = string.Empty;
    public string OIB { get; init; } = string.Empty;
    public string JMBAG { get; init; } = string.Empty;
    public string CurrentRole { get; init; } = string.Empty;
}
