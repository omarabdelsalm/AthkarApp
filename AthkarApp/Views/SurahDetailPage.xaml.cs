using System.Collections.ObjectModel;
using System.IO;
using AthkarApp.Models;
using AthkarApp.Services;
using Plugin.Maui.Audio;

namespace AthkarApp.Views;

public partial class SurahDetailPage : ContentPage
{
    private readonly IQuranApiService _quranApiService;
    private readonly IQuranDownloadService _quranDownloadService;
    private readonly Surah _surah;
    private IAudioPlayer _audioPlayer;
    private bool _isPlaying;
    public System.Windows.Input.ICommand PlayAyahCommand { get; }

    public ObservableCollection<Ayah> Ayahs { get; set; } = new();

    public SurahDetailPage(IQuranApiService quranApiService, IQuranDownloadService quranDownloadService, Surah surah)
    {
        InitializeComponent();
        _quranApiService = quranApiService;
        _quranDownloadService = quranDownloadService;
        _surah = surah;
        PlayAyahCommand = new Command<Ayah>(async (ayah) => await PlayAyahAudio(ayah.Audio));
        BindingContext = this;

        SurahNameLabel.Text = $"{_surah.Name} ({_surah.EnglishName})";
        SurahInfoLabel.Text = $"عدد الآيات: {_surah.NumberOfAyahs} | {_surah.RevelationType}";

        UpdateDownloadStatus();
        LoadAyahs();
    }

    private void UpdateDownloadStatus()
    {
        _surah.IsDownloaded = _quranDownloadService.IsSurahDownloaded(_surah.Number);
        if (_surah.IsDownloaded)
        {
            DownloadButton.Text = "✓ تم التحميل (متاح بدون إنترنت)";
            DownloadButton.BackgroundColor = Color.FromArgb("#2C6E2C");
            DownloadButton.IsEnabled = false;
        }
    }

    private async void LoadAyahs()
    {
        try
        {
            var ayahs = await _quranApiService.GetAyahsAsync(_surah.Number);
            Ayahs.Clear();
            foreach (var ayah in ayahs)
                Ayahs.Add(ayah);
        }
        catch (Exception ex)
        {
            await DisplayAlert("خطأ", $"فشل تحميل الآيات: {ex.Message}\nتأكد من الاتصال بالإنترنت.", "حسناً");
        }
    }


    private async Task PlayAyahAudio(string audioUrl)
    {
        try
        {
            if (string.IsNullOrEmpty(audioUrl)) return;

            // إيقاف أي تلاوة جارية
            if (_audioPlayer != null && _isPlaying)
            {
                _audioPlayer.Stop();
                _audioPlayer.Dispose();
            }

            // ملاحظة: تلاوة الآيات المنفردة لا تدعم حالياً التخزين المحلي الكامل لكل آية لتوفير المساحة
            // ولكن يتم تحميلها عند الحاجة. في حال الرغبة في دعم كامل، يمكن تعديل QuranDownloadService
            using var client = new HttpClient();
            var audioBytes = await client.GetByteArrayAsync(audioUrl);
            var memoryStream = new MemoryStream(audioBytes);

            var audioManager = AudioManager.Current;
            _audioPlayer = audioManager.CreatePlayer(memoryStream);
            
            _audioPlayer.Play();
            _isPlaying = true;

            StopButton.IsEnabled = true;

            _audioPlayer.PlaybackEnded += (s, args) =>
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    _isPlaying = false;
                    StopButton.IsEnabled = false;
                });
            };
        }
        catch (Exception ex)
        {
            await DisplayAlert("خطأ", $"فشل تشغيل آية: {ex.Message}\nتأكد من الاتصال بالإنترنت.", "حسناً");
        }
    }

    private async void OnPlayTafseer(object sender, EventArgs e)
    {
        try
        {
            // إيقاف أي تلاوة جارية
            if (_audioPlayer != null && _isPlaying)
            {
                _audioPlayer.Stop();
                _audioPlayer.Dispose();
            }

            Stream audioStream;
            
            if (_quranDownloadService.IsSurahDownloaded(_surah.Number))
            {
                // تشغيل من الملف المحلي
                var path = _quranDownloadService.GetSurahAudioPath(_surah.Number);
                audioStream = File.OpenRead(path);
            }
            else
            {
                // تحميل مؤقت وتشغيل من الرابط
                PlayButton.Text = "⏳ جاري التحميل...";
                PlayButton.IsEnabled = false;

                var folderName = Preferences.Default.Get("SelectedReciterFolder", "mishaari_raashid_al_3afaasee");
                var audioUrl = $"https://download.quranicaudio.com/quran/{folderName}/{_surah.Number:000}.mp3";
                using var client = new HttpClient();
                var audioBytes = await client.GetByteArrayAsync(audioUrl);
                audioStream = new MemoryStream(audioBytes);
                
                PlayButton.Text = "▶ تشغيل التلاوة";
            }

            var audioManager = AudioManager.Current;
            _audioPlayer = audioManager.CreatePlayer(audioStream);
            
            _audioPlayer.Play();
            _isPlaying = true;

            PlayButton.IsEnabled = false; 
            StopButton.IsEnabled = true;

            _audioPlayer.PlaybackEnded += (s, args) =>
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    _isPlaying = false;
                    PlayButton.IsEnabled = true;
                    StopButton.IsEnabled = false;
                    audioStream.Dispose();
                });
            };
        }
        catch (Exception ex)
        {
            PlayButton.Text = "▶ تشغيل التلاوة";
            PlayButton.IsEnabled = true;
            await DisplayAlert("خطأ", $"فشل تشغيل التلاوة: {ex.Message}", "حسناً");
        }
    }

    private async void OnDownloadClicked(object sender, EventArgs e)
    {
        try
        {
            DownloadButton.IsVisible = false;
            DownloadProgressLayout.IsVisible = true;
            DownloadProgressBar.Progress = 0;
            DownloadStatusLabel.Text = "بدء التحميل...";

            await _quranDownloadService.DownloadSurahAsync(_surah, (progress) =>
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    DownloadProgressBar.Progress = progress;
                    DownloadStatusLabel.Text = $"جاري التحميل... {progress:P0}";
                });
            });

            DownloadProgressLayout.IsVisible = false;
            DownloadButton.IsVisible = true;
            UpdateDownloadStatus();
            
            await DisplayAlert("تم بنجاح", "تم تحميل السورة بنجاح وهي الآن متاحة بدون إنترنت.", "حسناً");
        }
        catch (Exception ex)
        {
            DownloadProgressLayout.IsVisible = false;
            DownloadButton.IsVisible = true;
            await DisplayAlert("خطأ", $"فشل التحميل: {ex.Message}", "حسناً");
        }
    }

    private void OnStopTafseer(object sender, EventArgs e)
    {
        if (_audioPlayer != null && _isPlaying)
        {
            _audioPlayer.Stop();
            _audioPlayer.Dispose();
            _isPlaying = false;
        }

        PlayButton.IsEnabled = true;
        StopButton.IsEnabled = false;
    }
}