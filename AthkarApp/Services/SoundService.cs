using Plugin.Maui.Audio;

namespace AthkarApp.Services;

public interface ISoundService
{
    Task PlaySoundAsync(string soundName);
}

public class SoundService : ISoundService
{
    private readonly IAudioManager _audioManager;

    public SoundService(IAudioManager audioManager)
    {
        _audioManager = audioManager;
    }

    public async Task PlaySoundAsync(string soundName)
    {
        try
        {
            var fileName = $"{soundName}.mp3";
            
            // محاولة فتح الملف، وإذا لم يوجد سيتم تجاهله بهدوء في الـ catch
            using var stream = await FileSystem.OpenAppPackageFileAsync(fileName);
            if (stream == null) return;

            var player = _audioManager.CreatePlayer(stream);
            player.Play();
        }
        catch (FileNotFoundException)
        {
            // تجاهل الخطأ إذا كان الملف غير موجود (لمنع توقف التطبيق)
            System.Diagnostics.Debug.WriteLine($"ملف الصوت {soundName}.mp3 غير موجود في الموارد.");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"خطأ آخر في تشغيل الصوت: {ex.Message}");
        }
    }
}