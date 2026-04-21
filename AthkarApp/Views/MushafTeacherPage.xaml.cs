using System.Collections.ObjectModel;
using System.Globalization;
using AthkarApp.Models;
using AthkarApp.Services;
using CommunityToolkit.Maui.Media;
using Plugin.Maui.Audio;

namespace AthkarApp.Views;

public partial class MushafTeacherPage : ContentPage
{
    private readonly QuranDatabase _quranDatabase;
    private readonly IQuranApiService _quranApiService;
    private readonly IHifzAssessmentService _assessmentService;
    private readonly ISoundService _soundService;
    
    private List<Ayah> _currentSurahAyahs = new();
    private int _currentAyahIndex = -1;
    private IAudioPlayer? _audioPlayer;
    private IAudioPlayer? _myAudioPlayer;
    private IAudioRecorder? _audioRecorder;
    private CancellationTokenSource? _sttTokenSource;
    private List<bool> _revealedWords = new();
    private bool _isStopRequested = false;
    private string? _lastRecordingPath;

    public MushafTeacherPage(
        QuranDatabase quranDatabase, 
        IQuranApiService quranApiService,
        IHifzAssessmentService assessmentService,
        ISoundService soundService)
    {
        InitializeComponent();
        _quranDatabase = quranDatabase;
        _quranApiService = quranApiService;
        _assessmentService = assessmentService;
        _soundService = soundService;

        LoadSurahs();
    }

    private async void LoadSurahs()
    {
        try
        {
            var surahs = await _quranApiService.GetSurahsAsync();
            if (SurahPicker != null)
                SurahPicker.ItemsSource = surahs;
        }
        catch { }
    }

    private async void OnSurahSelected(object sender, EventArgs e)
    {
        if (SurahPicker?.SelectedItem is not Surah selectedSurah) return;
        if (LoadingIndicator != null) LoadingIndicator.IsRunning = true;

        try
        {
            // جلب الآيات الأساسية (النص)
            var ayahs = await _quranApiService.GetAyahsAsync(selectedSurah.Number);
            _currentSurahAyahs = ayahs ?? new List<Ayah>();
            
            // تحديث روابط الصوت حسب القارئ المختار في القائمة (مهم جداً)
            await UpdateAyahAudioUrls();

            _currentAyahIndex = 0;
            DisplayCurrentAyah();
        }
        catch (Exception ex)
        {
            await DisplayAlert("خطأ", "فشل تحميل آيات السورة. تأكد من الاتصال بالإنترنت.", "حسناً");
        }
        finally
        {
            LoadingIndicator.IsRunning = false;
        }
    }

    private void DisplayCurrentAyah()
    {
        if (_currentAyahIndex < 0 || _currentAyahIndex >= _currentSurahAyahs.Count) return;

        var ayah = _currentSurahAyahs[_currentAyahIndex];
        
        // تقسيم الآية لكلمات
        var words = ayah.Text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        _revealedWords = words.Select(w => !HiddenModeSwitch.IsToggled).ToList();

        UpdateAyaDisplay();

        string surahName = (SurahPicker.SelectedItem as Surah)?.Name ?? "";
        SurahAyaInfoLabel.Text = $"سورة {surahName} | آية رقم {ayah.NumberInSurah}";
        
        FeedbackFrame.IsVisible = false;
        AssessmentResultLayout.Children.Clear();
        RecognitionStatusLabel.Text = "";
    }

    private void UpdateAyaDisplay(List<WordAssessment>? assessments = null)
    {
        var ayah = _currentAyahIndex >= 0 ? _currentSurahAyahs[_currentAyahIndex] : null;
        if (ayah == null) return;

        var words = ayah.Text?.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (words == null || AyaFlexLayout == null) return;

        AyaFlexLayout.Children.Clear();
        for (int i = 0; i < words.Length; i++)
        {
            var isRevealed = _revealedWords.Count > i && _revealedWords[i];
            var assessment = assessments != null && assessments.Count > i ? assessments[i] : null;

            var label = new Label
            {
                Text = isRevealed ? words[i] : ".....",
                FontSize = 28,
                Margin = new Thickness(5),
                FontFamily = "Amiri",
                TextColor = assessment?.Status switch
                {
                    WordStatus.Correct => Colors.SpringGreen,
                    WordStatus.Wrong => Colors.OrangeRed,
                    WordStatus.Missing => Colors.Gray,
                    _ => isRevealed ? Colors.White : Color.FromArgb("#444444")
                },
                VerticalOptions = LayoutOptions.Center
            };
            AyaFlexLayout.Children.Add(label);
        }
    }

    private async void OnPlayAyahClicked(object sender, EventArgs e)
    {
        if (_currentAyahIndex < 0) return;
        var ayah = _currentSurahAyahs[_currentAyahIndex];

        if (ayah == null || string.IsNullOrEmpty(ayah.Audio))
        {
            await DisplayAlert("تنبيه", "تلاوة هذه الآية غير متوفرة حالياً.", "حسناً");
            return;
        }

        if (_audioPlayer != null && _audioPlayer.IsPlaying)
        {
            _isStopRequested = true;
            _audioPlayer.Stop();
            return;
        }

        try
        {
            _isStopRequested = false;
            PlayButton.Text = "⏳";
            
            int repeats = 1;
            if (RepeatPicker != null)
            {
                repeats = RepeatPicker.SelectedIndex switch
                {
                    1 => 3,
                    2 => 5,
                    3 => 10,
                    _ => 1
                };
            }

            using var client = new HttpClient();
            var audioBytes = await client.GetByteArrayAsync(ayah.Audio);
            
            for (int i = 0; i < repeats && !_isStopRequested; i++)
            {
                MainThread.BeginInvokeOnMainThread(() => {
                    PlayButton.Text = "⏹";
                    if (repeats > 1 && RecognitionStatusLabel != null)
                        RecognitionStatusLabel.Text = $"تكرار {i + 1} من {repeats}...";
                });

                using var memoryStream = new MemoryStream(audioBytes);
                _audioPlayer?.Dispose();
                _audioPlayer = AudioManager.Current.CreatePlayer(memoryStream);
                
                var tcs = new TaskCompletionSource<bool>();
                _audioPlayer.PlaybackEnded += (s, args) => tcs.TrySetResult(true);
                
                _audioPlayer.Play();
                await tcs.Task;
                
                if (repeats > 1 && i < repeats - 1 && !_isStopRequested)
                    await Task.Delay(1000); 
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("خطأ", "فشل تشغيل الصوت. تأكد من الاتصال بالإنترنت.", "حسناً");
        }
        finally
        {
            _isStopRequested = false;
            MainThread.BeginInvokeOnMainThread(() => {
                if (PlayButton != null) PlayButton.Text = "🔊";
                if (RecognitionStatusLabel != null) RecognitionStatusLabel.Text = "";
            });
        }
    }

    private async void OnReciteClicked(object sender, EventArgs e)
    {
        if (_currentAyahIndex < 0) return;

        _sttTokenSource?.Cancel();
        _sttTokenSource = new CancellationTokenSource();

        try
        {
            var micStatus = await Permissions.RequestAsync<Permissions.Microphone>();
            if (micStatus != PermissionStatus.Granted)
            {
                await DisplayAlert("تنبيه", "يجب منح صلاحية الميكروفون لاستخدام ميزة التسميع.", "حسناً");
                return;
            }

            if (ReciteButton != null)
            {
                ReciteButton.IsEnabled = false;
                ReciteButton.BackgroundColor = Colors.Red;
            }
            if (RecognitionStatusLabel != null)
                RecognitionStatusLabel.Text = "🎙 جاري الاستماع... ابدأ القراءة الآن";

            var recognitionResult = await SpeechToText.Default.ListenAsync(
                CultureInfo.GetCultureInfo("ar-SA"), 
                new Progress<string>(p => { if (RecognitionStatusLabel != null) RecognitionStatusLabel.Text = $"جاري التعرف: {p}"; }), 
                _sttTokenSource.Token);

            if (recognitionResult != null && recognitionResult.IsSuccessful)
            {
                ProcessAssessment(recognitionResult.Text);
            }
            else
            {
                await DisplayAlert("تنبيه", "لم نتمكن من التعرف على الصوت. تأكد من جودة الميكروفون والاتصال بالإنترنت.", "حسناً");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("خطأ", "حدث خطأ أثناء محاولة التعرف على الصوت.", "حسناً");
        }
        finally
        {
            if (ReciteButton != null)
            {
                ReciteButton.IsEnabled = true;
                ReciteButton.BackgroundColor = Color.FromArgb("#0B5E1B"); // DeepGreen
            }
            if (RecognitionStatusLabel != null)
                RecognitionStatusLabel.Text = "";
        }
    }

    private void ProcessAssessment(string userText)
    {
        var originalAyah = _currentSurahAyahs[_currentAyahIndex];
        var results = _assessmentService.AssessRecitation(originalAyah.Text, userText);

        AssessmentResultLayout.Children.Clear();
        int correctCount = 0;

        foreach (var word in results)
        {
            var label = new Label
            {
                Text = word.Word,
                FontSize = 22,
                Margin = new Thickness(5),
                FontFamily = "Amiri",
                TextColor = word.Status switch
                {
                    WordStatus.Correct => Colors.SpringGreen,
                    WordStatus.Wrong => Colors.OrangeRed,
                    WordStatus.Missing => Colors.Gray,
                    _ => Colors.White
                }
            };
            
            if (word.Status == WordStatus.Correct)
            {
                correctCount++;
                // كشف الكلمة في لوحة العرض الرئيسية أيضاً
                int index = results.IndexOf(word);
                if (index < _revealedWords.Count) _revealedWords[index] = true;
            }
            AssessmentResultLayout.Children.Add(label);
        }

        UpdateAyaDisplay(results);

        double accuracy = (double)correctCount / results.Count;
        AssessmentSummaryLabel.Text = $"الدقة: {accuracy:P0} ({correctCount} من أصل {results.Count} كلمات)";
        
        if (FeedbackFrame != null)
        {
            FeedbackFrame.IsVisible = true;
            var scrollView = this.FindByName<ScrollView>("MainScrollView");
            scrollView?.ScrollToAsync(FeedbackFrame, ScrollToPosition.Center, true);
        }

        if (accuracy > 0.8)
        {
            HapticFeedback.Default.Perform(HapticFeedbackType.Click);
            _soundService.PlaySuccessAsync();
        }
        else
        {
            HapticFeedback.Default.Perform(HapticFeedbackType.LongPress);
        }
    }

    private void OnHiddenModeToggled(object sender, ToggledEventArgs e)
    {
        if (_currentAyahIndex >= 0)
            DisplayCurrentAyah();
    }

    private async void OnReciterChanged(object sender, EventArgs e)
    {
        if (LoadingIndicator != null) LoadingIndicator.IsRunning = true;
        try {
            await UpdateAyahAudioUrls();
            DisplayCurrentAyah();
        } catch { }
        if (LoadingIndicator != null) LoadingIndicator.IsRunning = false;
    }

    private async Task UpdateAyahAudioUrls()
    {
        if (_currentSurahAyahs == null || !_currentSurahAyahs.Any() || ReciterPicker == null) return;

        string reciterId = "ar.husarymuallim";
        if (ReciterPicker.SelectedIndex >= 0)
        {
            reciterId = ReciterPicker.SelectedIndex switch
            {
                0 => "ar.husarymuallim",
                1 => "ar.minshawimuallim",
                2 => "ar.alafasy",
                3 => "ar.hudhaify",
                _ => "ar.husarymuallim"
            };
        }

        // ملاحظة: روابط API الخاصة بكل آية تعتمد على القارئ المختار
        // سنقوم بتحديث الروابط في القائمة الحالية (هذا يتطلب اتصال بالإنترنت في المرة الأولى لكل قارئ)
        int surahNumber = (SurahPicker.SelectedItem as Surah)?.Number ?? 1;
        
        try {
            // تحديث Preferences ليستهلكها API الخدمة (اختياري ولكن منظم)
            Preferences.Default.Set("SelectedReciterId", reciterId);
            
            // في وضع "المعلم"، نحتاج للروابط المباشرة من الـ API لهذا القارئ
            var updatedAyahs = await _quranApiService.GetAyahsAsync(surahNumber, true);
            if (updatedAyahs != null && updatedAyahs.Any())
            {
                _currentSurahAyahs = updatedAyahs;
            }
        } catch { }
    }

    private async void OnPlayMyRecitationClicked(object sender, EventArgs e)
    {
        if (string.IsNullOrEmpty(_lastRecordingPath) || !File.Exists(_lastRecordingPath))
        {
            await DisplayAlert("تنبيه", "لا يوجد تسجيل حالي للاستماع إليه.", "حسناً");
            return;
        }

        try
        {
            _myAudioPlayer?.Dispose();
            _myAudioPlayer = AudioManager.Current.CreatePlayer(File.OpenRead(_lastRecordingPath));
            _myAudioPlayer.Play();
        }
        catch (Exception ex)
        {
            await DisplayAlert("خطأ", "فشل تشغيل تسجيلك الشخصي.", "حسناً");
        }
    }

    private void OnNextAyahClicked(object sender, EventArgs e)
    {
        if (_currentAyahIndex < _currentSurahAyahs.Count - 1)
        {
            _currentAyahIndex++;
            DisplayCurrentAyah();
            MyRecitationPanel.IsVisible = false; // إخفاء زر التسجيل القديم للآية الجديدة
        }
        else
        {
            DisplayAlert("تم", "لقد وصلت لنهاية السورة. أحسنت!", "حسناً");
        }
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _audioPlayer?.Dispose();
        _myAudioPlayer?.Dispose();
        _sttTokenSource?.Cancel();

        // تنظيف الملفات المؤقتة للتسجيل
        if (!string.IsNullOrEmpty(_lastRecordingPath) && File.Exists(_lastRecordingPath))
        {
            try { File.Delete(_lastRecordingPath); } catch { }
        }
    }
}
