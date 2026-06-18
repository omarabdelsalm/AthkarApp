using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Microsoft.Maui.Storage;
using AthkarApp.Models;

namespace AthkarApp.Services
{
    public class AchievementsService
    {
        private const string AchievementsKey = "user_achievements_data";
        private Dictionary<string, DailyDeeds> _deedsHistory;

        public AchievementsService()
        {
            LoadData();
        }

        private void LoadData()
        {
            string json = Preferences.Default.Get(AchievementsKey, string.Empty);
            if (!string.IsNullOrEmpty(json))
            {
                try
                {
                    _deedsHistory = JsonConvert.DeserializeObject<Dictionary<string, DailyDeeds>>(json) ?? new Dictionary<string, DailyDeeds>();
                }
                catch
                {
                    _deedsHistory = new Dictionary<string, DailyDeeds>();
                }
            }
            else
            {
                _deedsHistory = new Dictionary<string, DailyDeeds>();
            }
        }

        private void SaveData()
        {
            string json = JsonConvert.SerializeObject(_deedsHistory);
            Preferences.Default.Set(AchievementsKey, json);
        }

        public DailyDeeds GetTodayDeeds()
        {
            string todayStr = DateTime.Today.ToString("yyyy-MM-dd");
            if (!_deedsHistory.ContainsKey(todayStr))
            {
                _deedsHistory[todayStr] = new DailyDeeds { Date = DateTime.Today };
            }
            return _deedsHistory[todayStr];
        }

        public void AddTasbeeh(int count)
        {
            var today = GetTodayDeeds();
            today.TasbeehCount += count;
            SaveData();
        }

        public void AddQuranPage()
        {
            var today = GetTodayDeeds();
            today.QuranPagesRead += 1;
            SaveData();
        }

        public void MarkAthkarCompleted(string athkarType)
        {
            var today = GetTodayDeeds();
            if (athkarType == "أذكار الصباح")
            {
                today.MorningAthkarCompleted = true;
            }
            else if (athkarType == "أذكار المساء")
            {
                today.EveningAthkarCompleted = true;
            }
            SaveData();
        }

        public List<DailyDeeds> GetLast7Days()
        {
            var result = new List<DailyDeeds>();
            for (int i = 6; i >= 0; i--)
            {
                var date = DateTime.Today.AddDays(-i);
                string dateStr = date.ToString("yyyy-MM-dd");
                if (_deedsHistory.ContainsKey(dateStr))
                {
                    result.Add(_deedsHistory[dateStr]);
                }
                else
                {
                    result.Add(new DailyDeeds { Date = date });
                }
            }
            return result;
        }
        
        public int GetTotalQuranPages() => _deedsHistory.Values.Sum(d => d.QuranPagesRead);
        public int GetTotalTasbeeh() => _deedsHistory.Values.Sum(d => d.TasbeehCount);
    }
}
