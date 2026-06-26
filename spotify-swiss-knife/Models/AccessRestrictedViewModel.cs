namespace spotify_swiss_knife.Models;

/// <summary>
/// View model for the "access restricted" page shown when a user lacks permission for a
/// resource.
/// </summary>
public class AccessRestrictedViewModel
{
    public required string Heading { get; init; }
    public required string Message { get; init; }

    /// <summary>
    /// Same-site, always-relative URL of the page the user came from, used by the "go back"
    /// action. Falls back to the home page ("/") when no safe previous page is known.
    /// </summary>
    public required string BackUrl { get; init; }
}
