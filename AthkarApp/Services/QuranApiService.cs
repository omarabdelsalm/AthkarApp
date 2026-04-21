using System.Text.Json;
using System.Text.Json.Serialization;
using AthkarApp.Models;

namespace AthkarApp.Services;

public interface IQuranApiService
{
    Task<List<Surah>> GetSurahsAsync();
    Task<List<Ayah>> GetAyahsAsync(int surahNumber, bool forceRefresh = false);
    Task<PageData> GetPageAsync(int pageNumber);
    Task<string> GetTafsirAsync(int ayahNumber);
    Task SyncFullQuranAsync(Action<double> progressCallback);
}

public class QuranApiService : IQuranApiService
{
    private readonly HttpClient _httpClient;
    private readonly IFileStorageService _fileStorage;
    private readonly QuranDatabase _database;
    private List<Surah> _surahsCache;

    public QuranApiService(HttpClient httpClient, IFileStorageService fileStorage, QuranDatabase database)
    {
        _httpClient = httpClient;
        _fileStorage = fileStorage;
        _database = database;
    }

    public async Task<List<Surah>> GetSurahsAsync()
    {
        // 1. Check Memory Cache
        if (_surahsCache != null && _surahsCache.Any())
            return _surahsCache;

        // 2. Check Database
        var localSurahs = await _database.GetSurahsAsync();
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

            // Save to Database
            if (_surahsCache.Any())
            {
                await _database.SaveSurahsAsync(_surahsCache);
            }

            return _surahsCache;
        }
        catch
        {
            return new List<Surah>();
        }
    }

    public async Task<List<Ayah>> GetAyahsAsync(int surahNumber, bool forceRefresh = false)
    {
        // 1. Check Database
        if (!forceRefresh)
        {
            var ayahs = await _database.GetAyahsBySurahAsync(surahNumber);
            if (ayahs != null && ayahs.Any() && !string.IsNullOrEmpty(ayahs.First().Audio))
            {
                return ayahs;
            }
        }

        // 2. Fetch from API
        try
        {
            string reciterId = Preferences.Default.Get("SelectedReciterId", "ar.alafasy");
            var url = $"https://api.alquran.cloud/v1/surah/{surahNumber}/{reciterId}";
            var response = await _httpClient.GetStringAsync(url);
            var ayahResponse = JsonSerializer.Deserialize<AyahResponse>(response);
            var apiAyahs = ayahResponse?.Data?.Ayahs ?? new List<Ayah>();

            // Save to Database
            if (apiAyahs.Any())
            {
                foreach (var a in apiAyahs) a.SurahNumber = surahNumber;
                await _database.SaveAyahsAsync(apiAyahs);
            }

            return apiAyahs;
        }
        catch
        {
            return new List<Ayah>();
        }
    }

    public async Task<PageData> GetPageAsync(int pageNumber)
    {
        // 1. Check Database (Offline)
        var ayahs = await _database.GetPageAsync(pageNumber);
        if (ayahs != null && ayahs.Any())
        {
            return new PageData { Number = pageNumber, Ayahs = ayahs };
        }

        // 2. Fetch from API
        try
        {
            var url = $"https://api.alquran.cloud/v1/page/{pageNumber}/quran-uthmani";
            var response = await _httpClient.GetStringAsync(url);
            var pageResponse = JsonSerializer.Deserialize<PageResponse>(response);
            var pageData = pageResponse?.Data ?? new PageData { Number = pageNumber, Ayahs = new List<Ayah>() };

            // Save to Database
            if (pageData.Ayahs.Any())
            {
                foreach (var a in pageData.Ayahs) a.SurahNumber = a.Surah?.Number ?? 0;
                await _database.SaveAyahsAsync(pageData.Ayahs);
            }

            return pageData;
        }
        catch
        {
            return new PageData { Number = pageNumber, Ayahs = new List<Ayah>() };
        }
    }

    public async Task<string> GetTafsirAsync(int ayahNumber)
    {
        // 1. Check Database (Offline)
        var ayah = await _database.GetAyahAsync(ayahNumber);
        if (ayah != null && !string.IsNullOrEmpty(ayah.TafsirText))
        {
            return ayah.TafsirText;
        }

        // 2. Fetch from API
        try
        {
            // Using Jalalayn as default tafsir
            var url = $"https://api.alquran.cloud/v1/ayah/{ayahNumber}/ar.jalalayn";
            var response = await _httpClient.GetStringAsync(url);
            var tafsirResponse = JsonSerializer.Deserialize<TafsirResponse>(response);
            var tafsirText = tafsirResponse?.Data?.Text ?? "تعذر جلب التفسير حالياً.";

            // Save to Database
            if (ayah != null && !tafsirText.Contains("تعذر") && !string.IsNullOrEmpty(tafsirResponse?.Data?.Text))
            {
                ayah.TafsirText = tafsirText;
                await _database.UpdateAyahAsync(ayah);
            }

            return tafsirText;
        }
        catch
        {
            return "حدث خطأ أثناء الاتصال بالإنترنت لعرض التفسير.";
        }
    }

    public async Task SyncFullQuranAsync(Action<double> progressCallback)
    {
        try
        {
            // 1. Sync Suras
            var surahs = await GetSurahsAsync();
            progressCallback(0.05);

            // 2. Sync Ayahs Page by Page (604 pages) to be efficient
            // One call to get all ayahs is better if possible, but the API is paged.
            // We'll use the complete quran-uthmani edition to get all text.
            
            var url = "https://api.alquran.cloud/v1/quran/quran-uthmani";
            var response = await _httpClient.GetStringAsync(url);
            var fullQuran = JsonSerializer.Deserialize<FullQuranResponse>(response);
            
            if (fullQuran?.Data?.Surahs == null) return;

            int totalAyahs = 6236;
            int currentAyahCount = 0;
            
            foreach (var surah in fullQuran.Data.Surahs)
            {
                foreach (var ayah in surah.Ayahs)
                {
                    ayah.SurahNumber = surah.Number;
                    currentAyahCount++;
                }
                await _database.SaveAyahsAsync(surah.Ayahs);
                progressCallback(0.05 + (0.45 * (double)currentAyahCount / totalAyahs));
            }

            // 3. Sync Tafsir (Jalalayn)
            var tafsirUrl = "https://api.alquran.cloud/v1/quran/ar.jalalayn";
            var tafsirResponseStr = await _httpClient.GetStringAsync(tafsirUrl);
            var fullTafsir = JsonSerializer.Deserialize<FullQuranResponse>(tafsirResponseStr);

            if (fullTafsir?.Data?.Surahs != null)
            {
                var allAyahsInDb = new List<Ayah>();
                var surahNumbers = fullTafsir.Data.Surahs.Select(s => s.Number).ToList();
                
                // جلب كافة الآيات الموجودة دفعة واحدة لتقليل مكالمات قاعدة البيانات
                foreach (var sNum in surahNumbers)
                {
                    allAyahsInDb.AddRange(await _database.GetAyahsBySurahAsync(sNum));
                }

                var ayahsToUpdate = new List<Ayah>();
                currentAyahCount = 0;

                foreach (var surah in fullTafsir.Data.Surahs)
                {
                    foreach (var tafsirAyah in surah.Ayahs)
                    {
                        var dbAyah = allAyahsInDb.FirstOrDefault(a => a.SurahNumber == surah.Number && a.NumberInSurah == tafsirAyah.NumberInSurah);
                        if (dbAyah != null)
                        {
                            dbAyah.TafsirText = tafsirAyah.Text;
                            ayahsToUpdate.Add(dbAyah);
                        }
                        currentAyahCount++;
                    }
                }

                // تحديث كافة التفسيرات في عملية واحدة (Transaction)
                if (ayahsToUpdate.Any())
                {
                    await _database.UpdateAyahsInTransactionAsync(ayahsToUpdate);
                }
                
                progressCallback(1.0);
            }
        }
        catch (Exception ex)
        {
            throw;
        }
    }
}

public class FullQuranResponse
{
    [JsonPropertyName("data")]
    public FullQuranData Data { get; set; }
}

public class FullQuranData
{
    [JsonPropertyName("surahs")]
    public List<SurahWithAyahs> Surahs { get; set; }
}

public class SurahWithAyahs : Surah
{
    [JsonPropertyName("ayahs")]
    public List<Ayah> Ayahs { get; set; }
}