using AthkarApp.Models;

namespace AthkarApp.Services;

public interface IQuranDownloadService
{
    Task DownloadSurahAsync(Surah surah, Action<double> progressCallback);
    bool IsSurahDownloaded(int surahNumber);
    string GetSurahAudioPath(int surahNumber);
    Task SyncAllSurahsTextAsync(Action<double> progressCallback);
    Task ClearAllDownloadsAsync();
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
            string folderName = Preferences.Default.Get("SelectedReciterFolder", "mishaari_raashid_al_3afaasee");
            string reciterId = Preferences.Default.Get("SelectedReciterId", "ar.alafasy");
            
            var audioUrl = $"https://download.quranicaudio.com/quran/{folderName}/{surah.Number:000}.mp3";
            var fileName = $"surah_{surah.Number}_{reciterId}.mp3";

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
            throw;
        }
    }

    public bool IsSurahDownloaded(int surahNumber)
    {
        string reciterId = Preferences.Default.Get("SelectedReciterId", "ar.alafasy");
        var audioFile = $"surah_{surahNumber}_{reciterId}.mp3";
        return _fileStorage.Exists(audioFile);
    }

    public async Task SyncAllSurahsTextAsync(Action<double> progressCallback)
    {
        await _quranApiService.SyncFullQuranAsync(progressCallback);
    }

    public string GetSurahAudioPath(int surahNumber)
    {
        string reciterId = Preferences.Default.Get("SelectedReciterId", "ar.alafasy");
        return _fileStorage.GetFilePath($"surah_{surahNumber}_{reciterId}.mp3");
    }

    public async Task ClearAllDownloadsAsync()
    {
        await Task.Run(() =>
        {
            var dir = Microsoft.Maui.Storage.FileSystem.AppDataDirectory;
            var files = Directory.GetFiles(dir, "surah_*.mp3")
                        .Concat(Directory.GetFiles(dir, "surah_*.json"));
            
            foreach (var file in files)
            {
                try { File.Delete(file); } catch { }
            }
        });
    }
}
