using spotify_swiss_knife.Models;

namespace spotify_swiss_knife.Services;

public class MockRepository
{
    public List<Artist> Artists => _artistRepository.GetAll();

    public List<Album> Albums => _albumRepository.GetAll();

    public List<Track> Tracks => _trackRepository.GetAll();

    public List<Playlist> Playlists => _playlistRepository.GetAll();

    private readonly ArtistMockRepository _artistRepository;
    private readonly AlbumMockRepository _albumRepository;
    private readonly TrackMockRepository _trackRepository;
    private readonly PlaylistMockRepository _playlistRepository;

    private MockRepository(
        ArtistMockRepository artistRepository,
        AlbumMockRepository albumRepository,
        TrackMockRepository trackRepository,
        PlaylistMockRepository playlistRepository)
    {
        _artistRepository = artistRepository;
        _albumRepository = albumRepository;
        _trackRepository = trackRepository;
        _playlistRepository = playlistRepository;
    }

    public static MockRepository Create()
    {
        var snapshot = MockMusicDataStore.GetSnapshot();
        return new MockRepository(
            new ArtistMockRepository(snapshot),
            new AlbumMockRepository(snapshot),
            new TrackMockRepository(snapshot),
            new PlaylistMockRepository(snapshot));
    }

    public Artist? GetArtistById(string id) => _artistRepository.GetById(id);

    public Album? GetAlbumById(string id) => _albumRepository.GetById(id);

    public Track? GetTrackById(string id) => _trackRepository.GetById(id);

    public Playlist? GetPlaylistById(string id) => _playlistRepository.GetById(id);
}