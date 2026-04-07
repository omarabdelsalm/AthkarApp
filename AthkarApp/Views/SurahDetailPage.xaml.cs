using System.Collections.ObjectModel;
using System.IO;
using AthkarApp.Models;
using AthkarApp.Services;
using Plugin.Maui.Audio;

namespace AthkarApp.Views;

public partial class SurahDetailPage : ContentPage
{
    private readonly IQuranApiService _quranApiService;
    private readonly Surah _surah;
    private IAudioPlayer _audioPlayer;
    private bool _isPlaying;

    public ObservableCollection<Ayah> Ayahs { get; set; } = new();

    public SurahDetailPage(IQuranApiService quranApiService, Surah surah)
    {
        InitializeComponent();
        _quranApiService = quranApiService;
        _surah = surah;
        BindingContext = this;

        SurahNameLabel.Text = $"{_surah.Name} ({_surah.EnglishName})";
        SurahInfoLabel.Text = $"عدد الآيات: {_surah.NumberOfAyahs} | {_surah.RevelationType}";

        LoadAyahs();
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
            await DisplayAlert("خطأ", $"فشل تحميل الآيات: {ex.Message}", "حسناً");
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

            ayahLayout.Children.Add(numberLabel);
            ayahLayout.Children.Add(textLabel);
            ayahFrame.Content = ayahLayout;
            AyahsLayout.Children.Add(ayahFrame);
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

            // تحديث واجهة المستخدم لحالة التحميل
            PlayButton.Text = "⏳ جاري التحميل...";
            PlayButton.IsEnabled = false;

            var audioUrl = $"https://download.quranicaudio.com/quran/mishaari_raashid_al_3afaasee/{_surah.Number:000}.mp3";
            using var client = new HttpClient();
            
            // تحميل الملف بالكامل في الذاكرة لضمان استقرار التشغيل
            var audioBytes = await client.GetByteArrayAsync(audioUrl);
            var memoryStream = new MemoryStream(audioBytes);

            var audioManager = AudioManager.Current;
            _audioPlayer = audioManager.CreatePlayer(memoryStream);
            
            _audioPlayer.Play();
            _isPlaying = true;

            PlayButton.Text = "▶ تشغيل التلاوة";
            PlayButton.IsEnabled = false; // معطل أثناء التشغيل
            StopButton.IsEnabled = true;

            _audioPlayer.PlaybackEnded += (s, args) =>
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    _isPlaying = false;
                    PlayButton.IsEnabled = true;
                    StopButton.IsEnabled = false;
                });
            };
        }
        catch (Exception ex)
        {
            PlayButton.Text = "▶ تشغيل التلاوة";
            PlayButton.IsEnabled = true;
            await DisplayAlert("خطأ", $"فشل تشغيل التلاوة: {ex.Message}\nتأكد من اتصال الإنترنت.", "حسناً");
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