namespace AthkarApp.Models;

public class KhatmahPlanConfig
{
    public string Name { get; set; } = string.Empty;
    public int DurationDays { get; set; } = 30;
    public int StartPage { get; set; } = 1;
    public int EndPage { get; set; } = 604;
    public string ColorHex { get; set; } = "#5EB670";
    public bool ReminderEnabled { get; set; } = true;
    public TimeSpan ReminderTime { get; set; } = TimeSpan.FromHours(21);
}
