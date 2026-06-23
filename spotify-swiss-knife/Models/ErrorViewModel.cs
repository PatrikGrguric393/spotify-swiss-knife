namespace spotify_swiss_knife.Models;

/// <summary>
/// View model for the default error page. <see cref="ShowRequestId"/> hides the request id
/// when none is available so the page doesn't render an empty label.
/// </summary>
public class ErrorViewModel
{
    public string? RequestId { get; set; }

    public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
}
