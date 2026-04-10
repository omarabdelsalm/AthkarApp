using System.Text.Json;
using AthkarApp.Models;

namespace AthkarApp.Services;

public interface IQuranApiService
{
    Task<List<Surah>> GetSurahsAsync();
    Task<List<Ayah>> GetAyahsAsync(int surahNumber);
    Task<PageData> GetPageAsync(int pageNumber);
    Task<string> GetTafsirAsync(int ayahNumber);
}

public class QuranApiService : IQuranApiService
{
    private readonly HttpClient _httpClient;
    private readonly IFileStorageService _fileStorage;
    private List<Surah> _surahsCache;

    public QuranApiService(HttpClient httpClient, IFileStorageService fileStorage)
    {
        _httpClient = httpClient;
        _fileStorage = fileStorage;
    }

    public async Task<List<Surah>> GetSurahsAsync()
    {
        // 1. Check Memory Cache
        if (_surahsCache != null && _surahsCache.Any())
            return _surahsCache;

        // 2. Check Local Storage
        var localSurahs = await _fileStorage.LoadJsonAsync<List<Surah>>("surahs.json");
        if (localSurahs != null && localSurahs.Any())
        {
            _surahsCache = localSurahs;
            return _surahsCache;
        }

        // 3. Fetch from API
        try
        {
            var response = await _httpClient.GetStringAsync("https://api.alquran.cloud/v1/surah");
            var surahResponse = JsonSerializer.Deserialize<SurahResponse>(response);
            _surahsCache = surahResponse?.Data ?? new List<Surah>();

            // Save to Local Storage for next time
            if (_surahsCache.Any())
            {
                await _fileStorage.SaveJsonAsync("surahs.json", _surahsCache);
            }

            return _surahsCache;
        }
        catch
        {
            return new List<Surah>();
        }
    }

    public async Task<List<Ayah>> GetAyahsAsync(int surahNumber)
    {
        var fileName = $"surah_{surahNumber}.json";

        // 1. Check Local Storage
        var localAyahs = await _fileStorage.LoadJsonAsync<List<Ayah>>(fileName);
        if (localAyahs != null && localAyahs.Any())
        {
            return localAyahs;
        }

        // 2. Fetch from API
        try
        {
            var url = $"https://api.alquran.cloud/v1/surah/{surahNumber}/ar.alafasy";
            var response = await _httpClient.GetStringAsync(url);
            var ayahResponse = JsonSerializer.Deserialize<AyahResponse>(response);
            var ayahs = ayahResponse?.Data?.Ayahs ?? new List<Ayah>();

            // Save to Local Storage
            if (ayahs.Any())
            {
                await _fileStorage.SaveJsonAsync(fileName, ayahs);
            }

            return ayahs;
        }
        catch
        {
            return new List<Ayah>();
        }
    }

    public async Task<PageData> GetPageAsync(int pageNumber)
    {
        try
        {
            var url = $"https://api.alquran.cloud/v1/page/{pageNumber}/quran-uthmani";
            var response = await _httpClient.GetStringAsync(url);
            var pageResponse = JsonSerializer.Deserialize<PageResponse>(response);
            return pageResponse?.Data ?? new PageData { Number = pageNumber, Ayahs = new List<Ayah>() };
        }
        catch
        {
            return new PageData { Number = pageNumber, Ayahs = new List<Ayah>() };
        }
    }

    public async Task<string> GetTafsirAsync(int ayahNumber)
    {
        try
        {
            // Using Jalalayn as default tafsir
            var url = $"https://api.alquran.cloud/v1/ayah/{ayahNumber}/ar.jalalayn";
            var response = await _httpClient.GetStringAsync(url);
            var tafsirResponse = JsonSerializer.Deserialize<TafsirResponse>(response);
            return tafsirResponse?.Data?.Text ?? "تعذر جلب التفسير حالياً.";
        }
        catch
        {
            return "حدث خطأ أثناء جلب التفسير.";
        }
    }
}