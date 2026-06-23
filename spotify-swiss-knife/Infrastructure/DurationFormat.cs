namespace spotify_swiss_knife.Infrastructure;

// Shared formatting for track durations, which are stored as milliseconds but shown to users
// as minutes:seconds. Centralised so the library and global-search views render durations
// identically.
public static class DurationFormat
{
    // Formats a millisecond duration as m:ss (e.g. 213000 -> "3:33"). Negative values clamp to
    // "0:00" so a bad stored value can never produce a malformed string.
    public static string MinutesSeconds(int durationMs)
    {
        var totalSeconds = Math.Max(0, durationMs / 1000);
        return $"{totalSeconds / 60}:{totalSeconds % 60:D2}";
    }
}
