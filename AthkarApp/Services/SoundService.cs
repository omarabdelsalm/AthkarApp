using Plugin.Maui.Audio;

namespace AthkarApp.Services;

public interface ISoundService
{
    Task PlaySoundAsync(string soundName);
    Task PlaySuccessAsync();
}

public class SoundService : ISoundService
{
    private readonly IAudioManager _audioManager;

    public SoundService(IAudioManager audioManager)
    {
        _audioManager = audioManager;
    }

    public async Task PlaySuccessAsync()
    {
        await PlaySoundAsync("success");
    }

    public async Task PlaySoundAsync(string soundName)
    {
        try
        {
            string[] extensions = { ".mp3", ".wav" };
            Stream? stream = null;
            
            foreach (var ext in extensions)
            {
                try 
                {
                    stream = await FileSystem.OpenAppPackageFileAsync($"{soundName}{ext}");
                    if (stream != null) break;
                }
                catch { }
            }

            if (stream == null) return;

            var player = _audioManager.CreatePlayer(stream);
            player.Play();
        }
        catch (Exception ex)
        {
        }
    }
}