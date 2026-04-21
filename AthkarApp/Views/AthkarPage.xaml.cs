using AthkarApp.Services;
using AthkarApp.Models;

namespace AthkarApp.Views;

public partial class AthkarPage : ContentPage
{
    private readonly AthkarService _athkarService;
    private readonly ISoundService _soundService;
    private readonly IStreakService _streakService;
    private List<AthkarCategory> _categories;
    private List<ThikrItem> _currentAthkarList;
    private int _currentIndex;
    private int _currentCount;

    public AthkarPage(AthkarService athkarService, ISoundService soundService, IStreakService streakService)
    {
        InitializeComponent();
        _athkarService = athkarService;
        _soundService = soundService;
        _streakService = streakService;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        
        await LoadCategoriesAsync();

        var streakInfo = await _streakService.CheckAndUpdateStreakAsync();
        StreakLabel.Text = streakInfo.Count.ToString();

        if (streakInfo.IsNewDay)
        {
            await DisplayAlert("سلسلة الأذكار 🔥", streakInfo.Message, "الحمد لله");
        }
    }

    private async Task LoadCategoriesAsync()
    {
        _categories = await _athkarService.GetAllCategoriesAsync();
        CategoriesCollectionView.ItemsSource = _categories;
    }


    private void SetCurrentCategory(AthkarCategory category)
    {
        _currentAthkarList = category.AthkarList;
        _currentIndex = 0;
        _currentCount = 0;
        CategoryTitleLabel.Text = category.Name;
        
        SelectionView.IsVisible = false;
        ThikrView.IsVisible = true;
        
        UpdateDisplay();
    }

    private void UpdateDisplay()
    {
        if (_currentAthkarList != null && _currentAthkarList.Any())
        {
            var item = _currentAthkarList[_currentIndex];
            AthkarLabel.Text = item.Text;
            
            // عرض المرجع والمصدر بشكل منفصل واحترافي
            ReferenceLabel.Text = item.Reference;
            HadithSourceLabel.Text = item.HadithSource;
            HadithSourceLabel.IsVisible = !string.IsNullOrEmpty(item.HadithSource);

            TargetCountLabel.Text = item.Count > 1 ? $"الهدف: {item.Count}" : "";
            CounterLabel.Text = _currentCount.ToString();
            ProgressLabel.Text = $"{_currentIndex + 1} / {_currentAthkarList.Count}";
            
            NextButton.Text = (_currentIndex == _currentAthkarList.Count - 1) ? "العودة للبداية ↺" : "الذكر التالي ➔";
        }
        else
        {
            AthkarLabel.Text = "لا توجد أذكار في هذا القسم";
            ReferenceLabel.Text = "";
            TargetCountLabel.Text = "";
        }
    }

    private void OnCategoryTapped(object sender, TappedEventArgs e)
    {
        if (e.Parameter is AthkarCategory selectedCategory)
        {
            SetCurrentCategory(selectedCategory);
        }
    }

    private void OnBackToSelection(object sender, EventArgs e)
    {
        StopSpeech();
        ThikrView.IsVisible = false;
        SelectionView.IsVisible = true;
    }

    private async void OnIncrementCount(object sender, EventArgs e)
    {
        if (_currentAthkarList == null || !_currentAthkarList.Any()) return;

        var currentItem = _currentAthkarList[_currentIndex];
        
        if (_currentCount < currentItem.Count)
        {
            _currentCount++;
            CounterLabel.Text = _currentCount.ToString();
            
            await CounterLabel.ScaleTo(1.2, 50);
            await CounterLabel.ScaleTo(1.0, 50);

            if (_currentCount >= currentItem.Count && _currentIndex < _currentAthkarList.Count - 1)
            {
                await Task.Delay(300);
                OnNextAthkar(null, null);
            }
        }
    }

    private void OnDecrementCount(object sender, EventArgs e)
    {
        if (_currentCount > 0)
        {
            _currentCount--;
            CounterLabel.Text = _currentCount.ToString();
        }
    }

    private void OnResetCounter(object sender, EventArgs e)
    {
        _currentCount = 0;
        CounterLabel.Text = "0";
    }

    private async void OnNextAthkar(object sender, EventArgs e)
    {
        StopSpeech();
        try 
        {
            if (_currentAthkarList != null && _currentAthkarList.Any())
            {
                _currentIndex = (_currentIndex + 1) % _currentAthkarList.Count;
                _currentCount = 0;
                
                UpdateDisplay();

                AthkarLabel.Opacity = 0;
                ReferenceLabel.Opacity = 0;
                await Task.WhenAll(
                    AthkarLabel.FadeTo(1, 400),
                    ReferenceLabel.FadeTo(1, 400)
                );
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("تنبيه", $"حدث خطأ أثناء الانتقال: {ex.Message}", "موافق");
        }
    }

    private async void OnShareAthkar(object sender, EventArgs e)
    {
        if (_currentAthkarList != null && _currentIndex < _currentAthkarList.Count)
        {
            var item = _currentAthkarList[_currentIndex];
            string shareText = $"🌟 {item.Text}\n\n📖 {item.Reference}\n\nتمت المشاركة من تطبيق \"أذكار\"";
            
            await Share.Default.RequestAsync(new ShareTextRequest
            {
                Title = "مشاركة ذكر",
                Text = shareText
            });
        }
    }

    private CancellationTokenSource _cts;

    private async void OnListenClicked(object sender, EventArgs e)
    {
        if (_currentAthkarList == null || _currentIndex >= _currentAthkarList.Count) return;

        var textToSpeak = _currentAthkarList[_currentIndex].Text;

        if (_cts != null && !_cts.IsCancellationRequested)
        {
            _cts.Cancel();
            ListenIconLabel.Text = "🔊";
            return;
        }

        try
        {
            _cts = new CancellationTokenSource();
            ListenIconLabel.Text = "🛑"; // تغيير الأيقونة أثناء القراءة
            
            await TextToSpeech.Default.SpeakAsync(textToSpeak, new SpeechOptions
            {
                Locale = (await TextToSpeech.Default.GetLocalesAsync()).FirstOrDefault(l => l.Language == "ar"),
                Pitch = 1.0f,
                Volume = 1.0f
            }, _cts.Token);
        }
        catch (Exception ex)
        {
        }
        finally
        {
            ListenIconLabel.Text = "🔊";
            _cts = null;
        }
    }

    private void StopSpeech()
    {
        if (_cts != null)
        {
            _cts.Cancel();
            _cts = null;
            ListenIconLabel.Text = "🔊";
        }
    }

    private async void OnSettingsClicked(object sender, EventArgs e)
    {
        try 
        {
            StopSpeech();
            await MainThread.InvokeOnMainThreadAsync(async () => {
                await Shell.Current.GoToAsync(nameof(SettingsPage));
            });
        }
        catch (Exception ex)
        {
            // Fallback for potential shell navigation issues on Android 15
            await MainThread.InvokeOnMainThreadAsync(async () => {
                await Navigation.PushAsync(new SettingsPage(
                    App.Current.Handler.MauiContext.Services.GetService<IAthkarNotificationService>(),
                    App.Current.Handler.MauiContext.Services.GetService<IQuranDownloadService>(),
                    App.Current.Handler.MauiContext.Services.GetService<IPrayerService>()
                ));
            });
        }
    }

    protected override bool OnBackButtonPressed()
    {
        if (ThikrView.IsVisible)
        {
            OnBackToSelection(null, null);
            return true;
        }
        return base.OnBackButtonPressed();
    }
}