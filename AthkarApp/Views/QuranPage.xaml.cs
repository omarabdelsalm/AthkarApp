using System.Collections.ObjectModel;
using AthkarApp.Models;
using AthkarApp.Services;

namespace AthkarApp.Views;

public partial class QuranPage : ContentPage
{
    private readonly IQuranApiService _quranApiService;
    private List<Surah> _allSurahs;

    public ObservableCollection<Surah> Surahs { get; set; } = new();

    public QuranPage(IQuranApiService quranApiService)
    {
        InitializeComponent();
        _quranApiService = quranApiService;
        BindingContext = this;
        LoadSurahs();
    }

    private async void LoadSurahs()
    {
        try
        {
            _allSurahs = await _quranApiService.GetSurahsAsync();
            Surahs.Clear();
            foreach (var surah in _allSurahs)
                Surahs.Add(surah);
        }
        catch (Exception ex)
        {
            await DisplayAlert("خطأ", $"فشل تحميل السور: {ex.Message}", "حسناً");
        }
    }

    private void OnSearchTextChanged(object sender, TextChangedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(e.NewTextValue))
        {
            Surahs.Clear();
            foreach (var surah in _allSurahs)
                Surahs.Add(surah);
        }
        else
        {
            var filtered = _allSurahs.Where(s =>
                s.Name.Contains(e.NewTextValue) ||
                s.EnglishName.Contains(e.NewTextValue, StringComparison.OrdinalIgnoreCase) ||
                s.EnglishNameTranslation.Contains(e.NewTextValue, StringComparison.OrdinalIgnoreCase))
                .ToList();

            Surahs.Clear();
            foreach (var surah in filtered)
                Surahs.Add(surah);
        }
    }

    private async void OnSurahSelected(object sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is Surah selectedSurah)
        {
            var detailPage = new SurahDetailPage(_quranApiService, selectedSurah);
            await Navigation.PushAsync(detailPage);
            ((CollectionView)sender).SelectedItem = null;
        }
    }
}