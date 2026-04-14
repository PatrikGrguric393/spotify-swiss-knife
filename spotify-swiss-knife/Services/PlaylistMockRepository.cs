using spotify_swiss_knife.Models;

namespace spotify_swiss_knife.Services;

public class PlaylistMockRepository
{
    private readonly MockMusicDataSnapshot _snapshot;

    public PlaylistMockRepository() : this(MockMusicDataStore.GetSnapshot())
    {
    }

    public PlaylistMockRepository(MockMusicDataSnapshot snapshot)
    {
        _snapshot = snapshot;
    }

    public List<Playlist> GetAll() => _snapshot.Playlists;

    public Playlist? GetById(string id) => _snapshot.Playlists.FirstOrDefault(playlist => playlist.Id == id);
}