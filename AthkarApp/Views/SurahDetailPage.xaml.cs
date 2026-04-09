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

    public ObservableCollection<Ayah> Ayahs { get; set; } = new();

    public SurahDetailPage(IQuranApiService quranApiService, IQuranDownloadService quranDownloadService, Surah surah)
    {
        InitializeComponent();
        _quranApiService = quranApiService;
        _quranDownloadService = quranDownloadService;
        _surah = surah;
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

            CreateAyahViews();
        }
        catch (Exception ex)
        {
            await DisplayAlert("خطأ", $"فشل تحميل الآيات: {ex.Message}\nتأكد من الاتصال بالإنترنت.", "حسناً");
        }
    }

    private void CreateAyahViews()
    {
        AyahsLayout.Children.Clear();

        foreach (var ayah in Ayahs)
        {
            var ayahFrame = new Frame
            {
                BackgroundColor = Colors.White,
                CornerRadius = 15,
                Padding = new Thickness(15, 10),
                HasShadow = true,
                Margin = new Thickness(0, 5)
            };

            var ayahLayout = new VerticalStackLayout();

            var numberLabel = new Label
            {
                Text = $"﴿{ayah.NumberInSurah}﴾",
                FontSize = 16,
                TextColor = Colors.Gray,
                HorizontalOptions = LayoutOptions.Start
            };

            var textLabel = new Label
            {
                Text = ayah.Text,
                FontSize = 22,
                TextColor = Color.FromArgb("#2C6E2C"),
                HorizontalOptions = LayoutOptions.Center,
                HorizontalTextAlignment = TextAlignment.Center,
                LineBreakMode = LineBreakMode.WordWrap
            };

            var playAyahButton = new Button
            {
                Text = "▶ استماع لهذه الآية",
                FontSize = 14,
                BackgroundColor = Color.FromArgb("#2C6E2C"),
                TextColor = Colors.White,
                Margin = new Thickness(0, 5, 0, 0),
                HorizontalOptions = LayoutOptions.End,
                CornerRadius = 10,
                Padding = new Thickness(10, 5)
            };
            playAyahButton.Clicked += async (s, e) => await PlayAyahAudio(ayah.Audio);

            ayahLayout.Children.Add(numberLabel);
            ayahLayout.Children.Add(textLabel);
            ayahLayout.Children.Add(playAyahButton);
            ayahFrame.Content = ayahLayout;
            AyahsLayout.Children.Add(ayahFrame);
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

                var audioUrl = $"https://download.quranicaudio.com/quran/mishaari_raashid_al_3afaasee/{_surah.Number:000}.mp3";
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