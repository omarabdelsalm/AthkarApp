using SQLite;

namespace AthkarApp.Models;

public class AthkarCategory
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }
    [Indexed]
    public string Name { get; set; } = string.Empty;
    [Ignore]
    public List<ThikrItem> AthkarList { get; set; } = new();
    public string ImagePath { get; set; } = string.Empty;
    public string CardColor { get; set; } = string.Empty;
}

public class ThikrItem
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }
    [Indexed]
    public int CategoryId { get; set; }
    public string Text { get; set; } = string.Empty;
    public int Count { get; set; } = 1;
    public string Reference { get; set; } = string.Empty;
    public string HadithSource { get; set; } = string.Empty;
    public string AudioUrl { get; set; } = string.Empty;
}

public class CounterState
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }
    public int Count { get; set; }
    public int CurrentIndex { get; set; }
    public string CategoryName { get; set; } = string.Empty;
}