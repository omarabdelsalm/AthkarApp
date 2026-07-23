using SQLite;

namespace AthkarApp.Models
{
    public class DailyPrayerTracker
    {
        [PrimaryKey]
        public string DateString { get; set; } // Format: yyyyMMdd

        // Status for obligatory prayers: 0=None, 1=Masjid, 2=Time, 3=Late, 4=Missed
        public int FajrStatus { get; set; }
        public int DhuhrStatus { get; set; }
        public int AsrStatus { get; set; }
        public int MaghribStatus { get; set; }
        public int IshaStatus { get; set; }

        // Status for Sunnah prayers: true/false
        public bool FajrSunnah { get; set; }
        public bool DhuhrSunnah { get; set; }
        public bool AsrSunnah { get; set; }
        public bool MaghribSunnah { get; set; }
        public bool IshaSunnah { get; set; }
    }
}
