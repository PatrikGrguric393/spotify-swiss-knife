using spotify_swiss_knife.Models;

namespace spotify_swiss_knife.Services;

public class ArtistMockRepository
{
    private readonly MockMusicDataSnapshot _snapshot;

    public ArtistMockRepository() : this(MockMusicDataStore.GetSnapshot())
    {
    }

    public ArtistMockRepository(MockMusicDataSnapshot snapshot)
    {
        _snapshot = snapshot;
    }

    public List<Artist> GetAll() => _snapshot.Artists;

    public Artist? GetById(string id) => _snapshot.Artists.FirstOrDefault(artist => artist.Id == id);
}