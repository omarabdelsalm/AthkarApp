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
        LoadCategories();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        
        var streakInfo = await _streakService.CheckAndUpdateStreakAsync();
        StreakLabel.Text = streakInfo.Count.ToString();

        if (streakInfo.IsNewDay)
        {
            await DisplayAlert("سلسلة الأذكار 🔥", streakInfo.Message, "الحمد لله");
        }
    }

    private void LoadCategories()
    {
        _categories = _athkarService.GetAllCategories();
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
            ReferenceLabel.Text = item.Reference;
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

    private async void OnSettingsClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(SettingsPage));
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