using AthkarApp.Services;
using AthkarApp.Models;

namespace AthkarApp.Views;

public partial class AthkarPage : ContentPage
{
    private readonly AthkarService _athkarService;
    private readonly ISoundService _soundService;
    private List<AthkarCategory> _categories;
    private List<string> _currentAthkarList;
    private int _currentIndex;
    private int _currentCount;

    public AthkarPage(AthkarService athkarService, ISoundService soundService)
    {
        InitializeComponent();
        _athkarService = athkarService;
        _soundService = soundService;
        LoadCategories();
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
            AthkarLabel.Text = _currentAthkarList[_currentIndex];
            CounterLabel.Text = _currentCount.ToString();
            ProgressLabel.Text = $"{_currentIndex + 1} / {_currentAthkarList.Count}";
            
            // تحديث نص الزر
            NextButton.Text = (_currentIndex == _currentAthkarList.Count - 1) ? "العودة للبداية ↺" : "الذكر التالي ➔";
        }
        else
        {
            AthkarLabel.Text = "لا توجد أذكار في هذا القسم";
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
        _currentCount++;
        CounterLabel.Text = _currentCount.ToString();
        
        // تأثير اهتزاز أو تكبير بسيط للعداد
        await CounterLabel.ScaleTo(1.2, 50);
        await CounterLabel.ScaleTo(1.0, 50);

        // تم تعطيل الصوت مؤقتاً لعدم وجود ملفات الصوت في الموارد
        // await _soundService.PlaySoundAsync("tap");
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

                // تلاشي بسيط للنص عند التغيير - تم تبسيطه لتجنب أي تعليق
                AthkarLabel.Opacity = 0;
                await AthkarLabel.FadeTo(1, 400);
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("تنبيه", $"حدث خطأ أثناء الانتقال: {ex.Message}", "موافق");
        }
    }
}