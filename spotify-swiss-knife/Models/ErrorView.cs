namespace spotify_swiss_knife.Models;

public class ErrorViewModel
{
    private string? RequestId { get; set; }

    private bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
}
