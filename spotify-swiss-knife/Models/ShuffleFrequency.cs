namespace spotify_swiss_knife.Models;

/// <summary>
/// How often a <see cref="ScheduledShuffle"/> repeats. Drives which day fields the schedule
/// form uses: Daily ignores them, Weekly/CustomWeekly use days-of-week, Monthly uses a
/// day-of-month. Numeric values are persisted, so keep them stable.
/// </summary>
public enum ShuffleFrequency
{
    Daily = 0,
    Weekly = 1,
    Monthly = 2,

    // Like Weekly but allows more than one day per week.
    CustomWeekly = 3
}
