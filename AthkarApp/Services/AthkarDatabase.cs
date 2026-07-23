using SQLite;
using AthkarApp.Models;

namespace AthkarApp.Services;

public class AthkarDatabase
{
    private SQLiteAsyncConnection _database;
    private readonly SemaphoreSlim _initSemaphore = new SemaphoreSlim(1, 1);

    private async Task Init()
    {
        if (_database is not null)
            return;

        await _initSemaphore.WaitAsync();
        try
        {
            if (_database is not null)
                return;

        var dbPath = Path.Combine(FileSystem.AppDataDirectory, "athkar_v1.db3");
        _database = new SQLiteAsyncConnection(dbPath);

        await _database.CreateTableAsync<AthkarCategory>();
        await _database.CreateTableAsync<ThikrItem>();
        await _database.CreateTableAsync<CounterState>();
        await _database.CreateTableAsync<DailyPrayerTracker>();
        }
        finally
        {
            _initSemaphore.Release();
        }
    }

    public async Task<List<AthkarCategory>> GetCategoriesAsync()
    {
        await Init();
        var categories = await _database.Table<AthkarCategory>().ToListAsync();
        foreach (var category in categories)
        {
            category.AthkarList = await _database.Table<ThikrItem>()
                                                .Where(t => t.CategoryId == category.Id)
                                                .ToListAsync();
        }
        return categories;
    }

    public async Task<AthkarCategory?> GetCategoryByNameAsync(string name)
    {
        await Init();
        var category = await _database.Table<AthkarCategory>()
                                     .Where(c => c.Name == name)
                                     .FirstOrDefaultAsync();
        if (category != null)
        {
            category.AthkarList = await _database.Table<ThikrItem>()
                                                .Where(t => t.CategoryId == category.Id)
                                                .ToListAsync();
        }
        return category;
    }

    public async Task SeedInitialDataAsync(List<AthkarCategory> categories)
    {
        await Init();
        
        // Version 2 has enriched Athkar quantity and counts
        int currentVersion = Preferences.Default.Get("Athkar_SeededVersion", 0);
        if (currentVersion >= 3)
        {
            return;
        }

        // Clear old static tables to force update of new/enriched Athkar
        await _database.DeleteAllAsync<AthkarCategory>();
        await _database.DeleteAllAsync<ThikrItem>();

        foreach (var category in categories)
        {
            category.Id = 0; // Let SQLite auto-increment
            await _database.InsertAsync(category);
            foreach (var thikr in category.AthkarList)
            {
                thikr.Id = 0;
                thikr.CategoryId = category.Id;
                await _database.InsertAsync(thikr);
            }
        }

        Preferences.Default.Set("Athkar_SeededVersion", 3);
    }

    public async Task SaveCounterStateAsync(CounterState state)
    {
        await Init();
        var existing = await _database.Table<CounterState>()
                                     .Where(s => s.CategoryName == state.CategoryName)
                                     .FirstOrDefaultAsync();
        if (existing != null)
        {
            state.Id = existing.Id;
            await _database.UpdateAsync(state);
        }
        else
        {
            await _database.InsertAsync(state);
        }
    }

    public async Task<CounterState?> GetCounterStateAsync(string categoryName)
    {
        await Init();
        return await _database.Table<CounterState>()
                             .Where(s => s.CategoryName == categoryName)
                             .FirstOrDefaultAsync();
    }

    public async Task SaveDailyTrackerAsync(DailyPrayerTracker tracker)
    {
        await Init();
        var existing = await _database.Table<DailyPrayerTracker>()
                                     .Where(t => t.DateString == tracker.DateString)
                                     .FirstOrDefaultAsync();
        if (existing != null)
        {
            await _database.UpdateAsync(tracker);
        }
        else
        {
            await _database.InsertAsync(tracker);
        }
    }

    public async Task<DailyPrayerTracker> GetDailyTrackerAsync(string dateStr)
    {
        await Init();
        var tracker = await _database.Table<DailyPrayerTracker>()
                                     .Where(t => t.DateString == dateStr)
                                     .FirstOrDefaultAsync();

        if (tracker == null)
        {
            tracker = new DailyPrayerTracker { DateString = dateStr };

            // Safe Migration from Preferences
            bool hasLegacyData = false;
            if (Preferences.Default.ContainsKey($"Tracker_{dateStr}_Fajr"))
            {
                tracker.FajrStatus = Preferences.Default.Get($"Tracker_{dateStr}_Fajr", 0);
                tracker.DhuhrStatus = Preferences.Default.Get($"Tracker_{dateStr}_Dhuhr", 0);
                tracker.AsrStatus = Preferences.Default.Get($"Tracker_{dateStr}_Asr", 0);
                tracker.MaghribStatus = Preferences.Default.Get($"Tracker_{dateStr}_Maghrib", 0);
                tracker.IshaStatus = Preferences.Default.Get($"Tracker_{dateStr}_Isha", 0);
                hasLegacyData = true;
            }

            if (Preferences.Default.ContainsKey($"TrackerSunnah_{dateStr}_Fajr"))
            {
                tracker.FajrSunnah = Preferences.Default.Get($"TrackerSunnah_{dateStr}_Fajr", false);
                tracker.DhuhrSunnah = Preferences.Default.Get($"TrackerSunnah_{dateStr}_Dhuhr", false);
                tracker.AsrSunnah = Preferences.Default.Get($"TrackerSunnah_{dateStr}_Asr", false);
                tracker.MaghribSunnah = Preferences.Default.Get($"TrackerSunnah_{dateStr}_Maghrib", false);
                tracker.IshaSunnah = Preferences.Default.Get($"TrackerSunnah_{dateStr}_Isha", false);
                hasLegacyData = true;
            }

            if (hasLegacyData)
            {
                await SaveDailyTrackerAsync(tracker);
                // Optionally clear Preferences here to save space
            }
        }

        return tracker;
    }
}
