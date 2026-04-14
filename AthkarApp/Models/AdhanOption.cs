namespace AthkarApp.Models;

public class AdhanOption
{
    public string Name { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;

    public static List<AdhanOption> GetAvailableAdhans()
    {
        return new List<AdhanOption>
        {
            new AdhanOption { Name = "أذان مكة (علي ملا)", FileName = "adhan_makkah" },
            new AdhanOption { Name = "أذان المدينة المنورة", FileName = "adhan_madina" },
            new AdhanOption { Name = "أذان مصر (عبد الباسط)", FileName = "adhan_egypt" },
            new AdhanOption { Name = "الأذان الافتراضي", FileName = "adhan" }
        };
    }
}
