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
        if (currentVersion >= 2)
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

        Preferences.Default.Set("Athkar_SeededVersion", 2);
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
}
