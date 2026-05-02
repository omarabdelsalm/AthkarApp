using System.Text.Json;
using System.Text.Json.Serialization;
using AthkarApp.Models;
using Microsoft.Maui.Networking;

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
            // If offline, return local immediately
            if (Connectivity.Current.NetworkAccess != NetworkAccess.Internet)
                return _surahsCache;
        }

        // 3. Fetch from API
        try
        {
            var response = await _httpClient.GetStringAsync("https://api.alquran.cloud/v1/surah");
            var surahResponse = JsonSerializer.Deserialize<SurahResponse>(response);
            var apiSurahs = surahResponse?.Data ?? new List<Surah>();

            if (apiSurahs.Any())
            {
                _surahsCache = apiSurahs;
                await _database.SaveSurahsAsync(apiSurahs);
            }

            return _surahsCache ?? localSurahs ?? new List<Surah>();
        }
        catch
        {
            return localSurahs ?? new List<Surah>();
        }
    }

    public async Task<List<Ayah>> GetAyahsAsync(int surahNumber, bool forceRefresh = false)
    {
        // 1. Check Database
        var localAyahs = await _database.GetAyahsBySurahAsync(surahNumber);
        
        if (!forceRefresh && localAyahs != null && localAyahs.Any())
        {
            // If we have audio OR we are offline, return local
            // (If we only have text and are online, we proceed to fetch to get audio links)
            if (!string.IsNullOrEmpty(localAyahs.First().Audio) || Connectivity.Current.NetworkAccess != NetworkAccess.Internet)
            {
                return localAyahs;
            }
        }

        // 2. Fetch from API
        try
        {
            string reciterId = Preferences.Default.Get("SelectedReciterId", "ar.alafasy");
            var url = $"https://api.alquran.cloud/v1/surah/{surahNumber}/editions/quran-uthmani,{reciterId}";
            var response = await _httpClient.GetStringAsync(url);
            
            var multiResponse = JsonSerializer.Deserialize<MultiAyahResponse>(response);
            var textEdition = multiResponse?.Data?.FirstOrDefault(e => e.Edition?.Identifier == "quran-uthmani");
            var audioEdition = multiResponse?.Data?.FirstOrDefault(e => e.Edition?.Identifier == reciterId);

            var apiAyahs = textEdition?.Ayahs ?? new List<Ayah>();
            
            if (audioEdition?.Ayahs != null)
            {
                for (int i = 0; i < apiAyahs.Count && i < audioEdition.Ayahs.Count; i++)
                {
                    apiAyahs[i].Audio = audioEdition.Ayahs[i].Audio;
                }
            }

            if (apiAyahs.Any())
            {
                foreach (var a in apiAyahs) a.SurahNumber = surahNumber;
                await _database.SaveAyahsAsync(apiAyahs);
                return apiAyahs;
            }

            return localAyahs ?? new List<Ayah>();
        }
        catch
        {
            return localAyahs ?? new List<Ayah>();
        }
    }

    public async Task<PageData> GetPageAsync(int pageNumber)
    {
        // 1. Check Database (Offline)
        var localAyahs = await _database.GetPageAsync(pageNumber);
        string currentReciterId = Preferences.Default.Get("SelectedReciterId", "ar.alafasy");

        if (localAyahs != null && localAyahs.Any())
        {
            // If we are offline, ALWAYS return what we have locally
            if (Connectivity.Current.NetworkAccess != NetworkAccess.Internet)
            {
                return new PageData { Number = pageNumber, Ayahs = localAyahs };
            }

            // If we have audio for the current reciter, we can also stay offline
            bool hasCorrectAudio = localAyahs.Any(a => !string.IsNullOrEmpty(a.Audio) && a.Audio.Contains(currentReciterId));
            if (hasCorrectAudio)
            {
                return new PageData { Number = pageNumber, Ayahs = localAyahs };
            }
        }

        // 2. Fetch from API
        try
        {
            // Fetch Text Edition
            var textUrl = $"https://api.alquran.cloud/v1/page/{pageNumber}/quran-uthmani";
            var textResponseStr = await _httpClient.GetStringAsync(textUrl);
            var textResponse = JsonSerializer.Deserialize<PageResponse>(textResponseStr);
            var apiAyahs = textResponse?.Data?.Ayahs ?? new List<Ayah>();

            if (apiAyahs.Any())
            {
                // Fetch Audio Edition (Optional/Best effort)
                try
                {
                    var audioUrl = $"https://api.alquran.cloud/v1/page/{pageNumber}/{currentReciterId}";
                    var audioResponseStr = await _httpClient.GetStringAsync(audioUrl);
                    var audioResponse = JsonSerializer.Deserialize<PageResponse>(audioResponseStr);
                    
                    if (audioResponse?.Data?.Ayahs != null)
                    {
                        for (int i = 0; i < apiAyahs.Count && i < audioResponse.Data.Ayahs.Count; i++)
                        {
                            apiAyahs[i].Audio = audioResponse.Data.Ayahs[i].Audio;
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Failed to fetch audio for page {pageNumber}: {ex.Message}");
                }

                foreach (var a in apiAyahs)
                {
                    a.Page = pageNumber;
                    a.SurahNumber = a.Surah?.Number ?? 0;
                }
                
                await _database.SaveAyahsAsync(apiAyahs);
                return new PageData { Number = pageNumber, Ayahs = apiAyahs };
            }

            return new PageData { Number = pageNumber, Ayahs = localAyahs ?? new List<Ayah>() };
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to fetch page {pageNumber}: {ex.Message}");
            return new PageData { Number = pageNumber, Ayahs = localAyahs ?? new List<Ayah>() };
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
            var url = $"https://api.alquran.cloud/v1/ayah/{ayahNumber}/ar.jalalayn";
            var response = await _httpClient.GetStringAsync(url);
            var tafsirResponse = JsonSerializer.Deserialize<TafsirResponse>(response);
            var tafsirText = tafsirResponse?.Data?.Text ?? "تعذر جلب التفسير حالياً.";

            if (ayah != null && !tafsirText.Contains("تعذر") && !string.IsNullOrEmpty(tafsirResponse?.Data?.Text))
            {
                ayah.TafsirText = tafsirText;
                await _database.UpdateAyahAsync(ayah);
            }

            return tafsirText;
        }
        catch
        {
            return ayah?.TafsirText ?? "حدث خطأ أثناء الاتصال بالإنترنت لعرض التفسير.";
        }
    }

    public async Task SyncFullQuranAsync(Action<double> progressCallback)
    {
        try
        {
            // 1. Sync Suras
            progressCallback(0.05);
            var surahs = await GetSurahsAsync();
            progressCallback(0.10);

            // 2. Fetch Full Quran Text (Uthmani)
            // Using a single call is much faster and more reliable than 604 individual calls
            var quranUrl = "https://api.alquran.cloud/v1/quran/quran-uthmani";
            var quranResponseStr = await _httpClient.GetStringAsync(quranUrl);
            var fullQuran = JsonSerializer.Deserialize<FullQuranResponse>(quranResponseStr);
            progressCallback(0.40);

            // 3. Fetch Full Tafsir (Jalalayn)
            var tafsirUrl = "https://api.alquran.cloud/v1/quran/ar.jalalayn";
            var tafsirResponseStr = await _httpClient.GetStringAsync(tafsirUrl);
            var fullTafsir = JsonSerializer.Deserialize<FullQuranResponse>(tafsirResponseStr);
            progressCallback(0.70);

            if (fullQuran?.Data?.Surahs != null && fullTafsir?.Data?.Surahs != null)
            {
                var allAyahsToSave = new List<Ayah>();
                
                for (int i = 0; i < fullQuran.Data.Surahs.Count; i++)
                {
                    var surahText = fullQuran.Data.Surahs[i];
                    var surahTafsir = fullTafsir.Data.Surahs[i];

                    for (int j = 0; j < surahText.Ayahs.Count; j++)
                    {
                        var ayah = surahText.Ayahs[j];
                        ayah.SurahNumber = surahText.Number;
                        
                        // Merge Tafsir
                        if (j < surahTafsir.Ayahs.Count)
                        {
                            ayah.TafsirText = surahTafsir.Ayahs[j].Text;
                        }

                        allAyahsToSave.Add(ayah);
                    }
                    
                    // Update progress periodically during processing
                    if (i % 10 == 0)
                        progressCallback(0.70 + (0.25 * (double)i / fullQuran.Data.Surahs.Count));
                }

                // 4. Save all Ayahs in bulk (Transaction)
                if (allAyahsToSave.Any())
                {
                    await _database.SaveAyahsAsync(allAyahsToSave);
                }
                
                progressCallback(1.0);
            }
        }
        catch (Exception ex)
        {
            throw new Exception($"خطأ أثناء المزامنة الشاملة: {ex.Message}");
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

    [JsonPropertyName("edition")]
    public EditionInfo Edition { get; set; }
}

public class EditionInfo
{
    [JsonPropertyName("identifier")]
    public string Identifier { get; set; }
}


public class MultiAyahResponse
{
    [JsonPropertyName("data")]
    public List<SurahWithAyahs> Data { get; set; }
}