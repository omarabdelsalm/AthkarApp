using SQLite;
using AthkarApp.Models;

namespace AthkarApp.Services;

public class AthkarDatabase
{
    private SQLiteAsyncConnection _database;

    private async Task Init()
    {
        if (_database is not null)
            return;

        var dbPath = Path.Combine(FileSystem.AppDataDirectory, "athkar_v1.db3");
        _database = new SQLiteAsyncConnection(dbPath);

        await _database.CreateTableAsync<AthkarCategory>();
        await _database.CreateTableAsync<ThikrItem>();
        await _database.CreateTableAsync<CounterState>();
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
        // التحقق مما إذا كانت قاعدة البيانات فارغة لتجنب التكرار
        var count = await _database.Table<AthkarCategory>().CountAsync();
        if (count > 0) return;

        foreach (var category in categories)
        {
            await _database.InsertAsync(category);
            foreach (var thikr in category.AthkarList)
            {
                thikr.CategoryId = category.Id;
                await _database.InsertAsync(thikr);
            }
        }
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
