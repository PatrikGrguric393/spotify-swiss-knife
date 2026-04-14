using spotify_swiss_knife.Models;

namespace spotify_swiss_knife.Services;

public class TrackMockRepository
{
    private readonly MockMusicDataSnapshot _snapshot;

    public TrackMockRepository() : this(MockMusicDataStore.GetSnapshot())
    {
    }

    public TrackMockRepository(MockMusicDataSnapshot snapshot)
    {
        _snapshot = snapshot;
    }

    public List<Track> GetAll() => _snapshot.Tracks;

    public Track? GetById(string id) => _snapshot.Tracks.FirstOrDefault(track => track.Id == id);
}