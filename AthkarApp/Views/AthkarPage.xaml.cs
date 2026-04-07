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
        CategoryPicker.ItemsSource = _categories.Select(c => c.Name).ToList();

        if (_categories.Any())
        {
            CategoryPicker.SelectedIndex = 0;
            SetCurrentCategory(_categories[0]);
        }
    }

    private void SetCurrentCategory(AthkarCategory category)
    {
        _currentAthkarList = category.AthkarList;
        _currentIndex = 0;
        _currentCount = 0;
        UpdateDisplay();
    }

    private void UpdateDisplay()
    {
        if (_currentAthkarList != null && _currentAthkarList.Any())
        {
            AthkarLabel.Text = _currentAthkarList[_currentIndex];
            CounterLabel.Text = _currentCount.ToString();
        }
        else
        {
            AthkarLabel.Text = "لا توجد أذكار في هذا القسم";
        }
    }

    private void OnCategorySelected(object sender, EventArgs e)
    {
        var selectedCategory = _categories[CategoryPicker.SelectedIndex];
        SetCurrentCategory(selectedCategory);
    }

    private async void OnIncrementCount(object sender, EventArgs e)
    {
        _currentCount++;
        CounterLabel.Text = _currentCount.ToString();
        await _soundService.PlaySoundAsync("tap");
        await ShowSuccessMessage();
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
        if (_currentAthkarList != null && _currentAthkarList.Any())
        {
            _currentIndex = (_currentIndex + 1) % _currentAthkarList.Count;
            _currentCount = 0;
            UpdateDisplay();
            await _soundService.PlaySoundAsync("next");
        }
    }

    private async Task ShowSuccessMessage()
    {
        SuccessMessage.IsVisible = true;
        await SuccessMessage.FadeTo(0, 2000);
        SuccessMessage.IsVisible = false;
    }
}