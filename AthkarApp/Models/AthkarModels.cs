namespace AthkarApp.Models;

public class AthkarCategory
{
    public string Name { get; set; }
    public List<string> AthkarList { get; set; }
}

public class CounterState
{
    public int Count { get; set; }
    public int CurrentIndex { get; set; }
    public string CategoryName { get; set; }
}