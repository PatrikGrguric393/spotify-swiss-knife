using System.ComponentModel.DataAnnotations;

namespace spotify_swiss_knife.Models.FormModels;

public class CreateScheduleForm
{
    [Required]
    public string PlaylistId { get; set; } = string.Empty;

    [Required]
    public string PlaylistName { get; set; } = string.Empty;

    public ShuffleRandomnessLevel RandomnessLevel { get; set; }

    // One or more days-of-week (0 = Sunday … 6 = Saturday).
    [Required]
    [MinLength(1, ErrorMessage = "Select at least one day.")]
    public List<int> DaysOfWeek { get; set; } = [];

    // "HH:mm" as entered in the client's local timezone, e.g. "08:00".
    // Converted to UTC in ToCronExpression using TimezoneOffsetMinutes.
    [Required]
    [RegularExpression(@"^([01]\d|2[0-3]):[0-5]\d$", ErrorMessage = "Enter a valid time (HH:mm).")]
    public string TimeUtc { get; set; } = "08:00";

    // Browser Date.getTimezoneOffset(): minutes to add to local time to reach UTC.
    // Null when unavailable (e.g. no JS), in which case the time is treated as UTC.
    public int? TimezoneOffsetMinutes { get; set; }

    // Builds the 5-field UTC cron string, converting the submitted local time
    // and days to UTC. A conversion that crosses midnight shifts the day-of-week.
    public string ToCronExpression()
    {
        var parts = TimeUtc.Split(':');
        var localMinutes = int.Parse(parts[0]) * 60 + int.Parse(parts[1]);
        var total = localMinutes + (TimezoneOffsetMinutes ?? 0);

        // Offset is within ±14h, so the day shift is always -1, 0 or +1.
        var dayDelta = (int)Math.Floor(total / 1440.0);
        var utcMinutes = total - dayDelta * 1440;

        var hour = (utcMinutes / 60).ToString("D2");
        var minute = (utcMinutes % 60).ToString("D2");
        var days = string.Join(",", DaysOfWeek
            .Select(d => (((d + dayDelta) % 7) + 7) % 7)
            .Distinct()
            .OrderBy(d => d));

        return $"{minute} {hour} * * {days}";
    }
}
