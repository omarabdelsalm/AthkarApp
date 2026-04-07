using System.Text.Json;
using AthkarApp.Models;

namespace AthkarApp.Services;

public interface IQuranApiService
{
    Task<List<Surah>> GetSurahsAsync();
    Task<List<Ayah>> GetAyahsAsync(int surahNumber);
}

public class QuranApiService : IQuranApiService
{
    private readonly HttpClient _httpClient;
    private List<Surah> _surahsCache;

    public QuranApiService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<List<Surah>> GetSurahsAsync()
    {
        if (_surahsCache != null && _surahsCache.Any())
            return _surahsCache;

        var response = await _httpClient.GetStringAsync("https://api.alquran.cloud/v1/surah");
        var surahResponse = JsonSerializer.Deserialize<SurahResponse>(response);
        _surahsCache = surahResponse?.Data ?? new List<Surah>();
        return _surahsCache;
    }

    public async Task<List<Ayah>> GetAyahsAsync(int surahNumber)
    {
        var url = $"https://api.alquran.cloud/v1/surah/{surahNumber}/ar.alafasy";
        var response = await _httpClient.GetStringAsync(url);
        var ayahResponse = JsonSerializer.Deserialize<AyahResponse>(response);
        return ayahResponse?.Data?.Ayahs ?? new List<Ayah>();
    }
}