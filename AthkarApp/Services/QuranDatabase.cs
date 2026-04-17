using SQLite;
using AthkarApp.Models;

namespace AthkarApp.Services;

public class QuranDatabase
{
    private SQLiteAsyncConnection _database;

    private async Task Init()
    {
        if (_database is not null)
            return;

        var dbPath = Path.Combine(FileSystem.AppDataDirectory, "quran_v1.db3");
        _database = new SQLiteAsyncConnection(dbPath);

        await _database.CreateTableAsync<Surah>();
        await _database.CreateTableAsync<Ayah>();
    }

    public async Task<List<Surah>> GetSurahsAsync()
    {
        await Init();
        return await _database.Table<Surah>().OrderBy(s => s.Number).ToListAsync();
    }

    public async Task<List<Ayah>> GetPageAsync(int pageNumber)
    {
        await Init();
        var ayahs = await _database.Table<Ayah>()
                                   .Where(a => a.Page == pageNumber)
                                   .OrderBy(a => a.Number)
                                   .ToListAsync();
        
        // ربط السور يدوياً إذا لزم الأمر، أو نعتمد على SurahNumber المخزن
        if (ayahs.Any())
        {
            var surahs = await GetSurahsAsync();
            foreach (var ayah in ayahs)
            {
                ayah.Surah = surahs.FirstOrDefault(s => s.Number == ayah.SurahNumber);
            }
        }
        
        return ayahs;
    }

    public async Task<List<Ayah>> GetAyahsBySurahAsync(int surahNumber)
    {
        await Init();
        return await _database.Table<Ayah>()
                                   .Where(a => a.SurahNumber == surahNumber)
                                   .OrderBy(a => a.NumberInSurah)
                                   .ToListAsync();
    }

    public async Task<Ayah> GetAyahAsync(int ayahNumber)
    {
        await Init();
        return await _database.Table<Ayah>().Where(a => a.Number == ayahNumber).FirstOrDefaultAsync();
    }

    public async Task SaveSurahsAsync(List<Surah> surahs)
    {
        await Init();
        await _database.InsertAllAsync(surahs, "OR REPLACE");
    }

    public async Task SaveAyahsAsync(List<Ayah> ayahs)
    {
        await Init();
        await _database.InsertAllAsync(ayahs, "OR REPLACE");
    }

    public async Task UpdateAyahsInTransactionAsync(List<Ayah> ayahs)
    {
        await Init();
        await _database.RunInTransactionAsync(conn =>
        {
            foreach (var ayah in ayahs)
            {
                conn.Update(ayah);
            }
        });
    }

    public async Task UpdateAyahAsync(Ayah ayah)
    {
        await Init();
        await _database.UpdateAsync(ayah);
    }

    public async Task<int> GetAyahCountAsync()
    {
        await Init();
        return await _database.Table<Ayah>().CountAsync();
    }

    public async Task ClearAllDataAsync()
    {
        await Init();
        await _database.DeleteAllAsync<Surah>();
        await _database.DeleteAllAsync<Ayah>();
    }
}
