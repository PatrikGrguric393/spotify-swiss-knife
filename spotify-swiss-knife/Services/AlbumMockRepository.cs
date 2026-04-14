using spotify_swiss_knife.Models;

namespace spotify_swiss_knife.Services;

public class AlbumMockRepository
{
    private readonly MockMusicDataSnapshot _snapshot;

    public AlbumMockRepository() : this(MockMusicDataStore.GetSnapshot())
    {
    }

    public AlbumMockRepository(MockMusicDataSnapshot snapshot)
    {
        _snapshot = snapshot;
    }

    public List<Album> GetAll() => _snapshot.Albums;

    public Album? GetById(string id) => _snapshot.Albums.FirstOrDefault(album => album.Id == id);
}