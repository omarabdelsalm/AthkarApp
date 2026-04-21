namespace AthkarApp.Models
{
    public class FiqhChapter : List<FiqhTopic>
    {
        public string Title { get; set; } = string.Empty;
        public string Icon { get; set; } = "🕌";
    }

    public class FiqhTopic
    {
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
    }

    public static class FiqhExtensions
    {
        public static FiqhChapter AddRangeReturn(this FiqhChapter chapter, IEnumerable<FiqhTopic> topics)
        {
            chapter.AddRange(topics);
            return chapter;
        }
    }
}
