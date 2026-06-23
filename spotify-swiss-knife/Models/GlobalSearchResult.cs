namespace spotify_swiss_knife.Models;

/// <summary>
/// One hit in the global search dropdown. <see cref="EntityType"/> labels the kind of result
/// (album, artist, track, …) and <see cref="Url"/> links to its detail page.
/// </summary>
public sealed record GlobalSearchResult
{
    public string EntityType { get; init; } = string.Empty;

    public string Title { get; init; } = string.Empty;

    public string Subtitle { get; init; } = string.Empty;

    public string Url { get; init; } = string.Empty;
}
