namespace AthkarApp.Models;

public class CalculationMethodOption
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;

    public static List<CalculationMethodOption> GetMethods()
    {
        return new List<CalculationMethodOption>
        {
            new() { Id = 5, Name = "الهيئة المصرية العامة للمساحة" },
            new() { Id = 4, Name = "جامعة أم القرى، مكة المكرمة" },
            new() { Id = 1, Name = "جامعة العلوم الإسلامية، كراتشي" },
            new() { Id = 2, Name = "الجمعية الإسلامية لأمريكا الشمالية (ISNA)" },
            new() { Id = 3, Name = "رابطة العالم الإسلامي" },
            new() { Id = 8, Name = "منطقة الخليج" },
            new() { Id = 9, Name = "الكويت" },
            new() { Id = 10, Name = "قطر" }
        };
    }
}

public class MadhabOption
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;

    public static List<MadhabOption> GetMadhabs()
    {
        return new List<MadhabOption>
        {
            new() { Id = 0, Name = "القياسي (شافعي، مالكي، حنبلي)" },
            new() { Id = 1, Name = "الحنفي" }
        };
    }
}
