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
            using var stream = await FileSystem.OpenAppPackageFileAsync(fileName);
            var player = _audioManager.CreatePlayer(stream);
            player.Play();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"خطأ في تشغيل الصوت: {ex.Message}");
        }
    }
}