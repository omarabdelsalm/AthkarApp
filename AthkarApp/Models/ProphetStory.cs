namespace AthkarApp.Models
{
    public class ProphetStory
    {
        public string Name { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty; // e.g. "أبو البشر"
        public string Story { get; set; } = string.Empty;
        public string Lessons { get; set; } = string.Empty;
        public string Icon { get; set; } = "👤";
        public string ColorHex { get; set; } = "#2E7D32";
    }
}
