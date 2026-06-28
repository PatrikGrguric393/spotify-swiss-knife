namespace spotify_swiss_knife.Models.FormModels;

/// <summary>The set of playlist ids selected for an immediate (manual) shuffle.</summary>
public sealed class PlaylistShuffleForm
{
    public List<string> PlaylistIds { get; set; } = [];
}
