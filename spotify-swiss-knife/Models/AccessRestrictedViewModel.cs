namespace spotify_swiss_knife.Models;

/// <summary>
/// View model for the "access restricted" page shown when a user lacks permission for a
/// resource.
/// </summary>
public class AccessRestrictedViewModel
{
    public required string Heading { get; init; }
    public required string Message { get; init; }
}
