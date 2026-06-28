namespace spotify_swiss_knife.Models;

/// <summary>
/// View model for the "access restricted" page shown when a user lacks permission for a
/// resource.
/// </summary>
public sealed class AccessRestrictedViewModel
{
    /// <summary>Bold title rendered at the top of the restricted-access page (e.g. "Access Denied").</summary>
    public required string Heading { get; init; }

    /// <summary>Body text explaining why access was denied and what the user can do.</summary>
    public required string Message { get; init; }

    /// <summary>
    /// Same-site, always-relative URL of the page the user came from, used by the "go back"
    /// action. Falls back to the home page ("/") when no safe previous page is known.
    /// </summary>
    public required string BackUrl { get; init; }
}
