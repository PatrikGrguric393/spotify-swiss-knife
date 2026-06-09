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

    // "HH:mm" in UTC, e.g. "08:00".
    [Required]
    [RegularExpression(@"^([01]\d|2[0-3]):[0-5]\d$", ErrorMessage = "Enter a valid time (HH:mm).")]
    public string TimeUtc { get; set; } = "08:00";

    // Builds the 5-field cron string from DaysOfWeek and TimeUtc.
    public string ToCronExpression()
    {
        var parts = TimeUtc.Split(':');
        var hour = parts[0];
        var minute = parts[1];
        var days = string.Join(",", DaysOfWeek.Distinct().OrderBy(d => d));
        return $"{minute} {hour} * * {days}";
    }
}
