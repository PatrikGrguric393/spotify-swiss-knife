namespace spotify_swiss_knife.Models;

public sealed record GlobalSearchResult
{
    public string EntityType { get; init; } = string.Empty;

    public string Title { get; init; } = string.Empty;

    public string Subtitle { get; init; } = string.Empty;

    public string Url { get; init; } = string.Empty;
}