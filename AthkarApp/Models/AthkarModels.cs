namespace AthkarApp.Models;

public class AthkarCategory
{
    public string Name { get; set; } = string.Empty;
    public List<ThikrItem> AthkarList { get; set; } = new();
    public string ImagePath { get; set; } = string.Empty;
    public string CardColor { get; set; } = string.Empty;
}

public class ThikrItem
{
    public string Text { get; set; } = string.Empty;
    public int Count { get; set; } = 1;
    public string Reference { get; set; } = string.Empty;
}

public class CounterState
{
    public int Count { get; set; }
    public int CurrentIndex { get; set; }
    public string CategoryName { get; set; }
}