using AthkarApp.Models;

namespace AthkarApp.Services;

public interface IQuranDownloadService
{
    Task DownloadSurahAsync(Surah surah, Action<double> progressCallback);
    bool IsSurahDownloaded(int surahNumber);
    string GetSurahAudioPath(int surahNumber);
    Task SyncAllSurahsTextAsync(Action<double> progressCallback);
}

public class QuranDownloadService : IQuranDownloadService
{
    private readonly IQuranApiService _quranApiService;
    private readonly IFileStorageService _fileStorage;
    private readonly HttpClient _httpClient;

    public QuranDownloadService(IQuranApiService quranApiService, IFileStorageService fileStorage, HttpClient httpClient)
    {
        _quranApiService = quranApiService;
        _fileStorage = fileStorage;
        _httpClient = httpClient;
    }

    public async Task DownloadSurahAsync(Surah surah, Action<double> progressCallback)
    {
        try
        {
            // 1. Download Ayahs text (this also caches them via QuranApiService)
            progressCallback(0.1);
            await _quranApiService.GetAyahsAsync(surah.Number);
            progressCallback(0.3);

            // 2. Download Full Surah Audio
            var audioUrl = $"https://download.quranicaudio.com/quran/mishaari_raashid_al_3afaasee/{surah.Number:000}.mp3";
            var fileName = $"surah_{surah.Number}.mp3";

            using var response = await _httpClient.GetAsync(audioUrl, HttpCompletionOption.ResponseHeadersRead);
            response.EnsureSuccessStatusCode();

            var totalBytes = response.Content.Headers.ContentLength ?? -1L;
            var canReportProgress = totalBytes != -1;

            using var contentStream = await response.Content.ReadAsStreamAsync();
            using var fileStream = new FileStream(_fileStorage.GetFilePath(fileName), FileMode.Create, FileAccess.Write, FileShare.None, 8192, true);

            var totalRead = 0L;
            var buffer = new byte[8192];
            var isMoreToRead = true;

            do
            {
                var read = await contentStream.ReadAsync(buffer, 0, buffer.Length);
                if (read == 0)
                {
                    isMoreToRead = false;
                    continue;
                }

                await fileStream.WriteAsync(buffer, 0, read);

                totalRead += read;
                if (canReportProgress)
                {
                    progressCallback(0.3 + (0.7 * (double)totalRead / totalBytes));
                }
            } while (isMoreToRead);

            surah.IsDownloaded = true;
            progressCallback(1.0);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error downloading surah: {ex.Message}");
            throw;
        }
    }

    public bool IsSurahDownloaded(int surahNumber)
    {
        var audioFile = $"surah_{surahNumber}.mp3";
        var jsonFile = $"surah_{surahNumber}.json";
        return _fileStorage.Exists(audioFile) && _fileStorage.Exists(jsonFile);
    }

    public async Task SyncAllSurahsTextAsync(Action<double> progressCallback)
    {
        try
        {
            var surahs = await _quranApiService.GetSurahsAsync();
            int total = surahs.Count;
            int current = 0;

            // نستخدم 114 سورة كمرجع
            for (int i = 1; i <= 114; i++)
            {
                await _quranApiService.GetAyahsAsync(i);
                current++;
                progressCallback((double)current / total);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error syncing all surahs: {ex.Message}");
            throw;
        }
    }

    public string GetSurahAudioPath(int surahNumber)
    {
        return _fileStorage.GetFilePath($"surah_{surahNumber}.mp3");
    }
}
