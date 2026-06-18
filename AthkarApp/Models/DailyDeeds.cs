using System;

namespace AthkarApp.Models
{
    public class DailyDeeds
    {
        public DateTime Date { get; set; }
        public int QuranPagesRead { get; set; }
        public int TasbeehCount { get; set; }
        public bool MorningAthkarCompleted { get; set; }
        public bool EveningAthkarCompleted { get; set; }

        public DailyDeeds()
        {
            Date = DateTime.Today;
        }
    }
}
