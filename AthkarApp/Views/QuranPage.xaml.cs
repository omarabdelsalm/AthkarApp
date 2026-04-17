using System.Collections.ObjectModel;
using AthkarApp.Models;
using AthkarApp.Services;

namespace AthkarApp.Views;

public partial class QuranPage : ContentPage
{
    private readonly IQuranApiService _quranApiService;
    private readonly IQuranDownloadService _quranDownloadService;
    private List<Surah> _allSurahs;

    public ObservableCollection<Surah> Surahs { get; set; } = new();

    public QuranPage(IQuranApiService quranApiService, IQuranDownloadService quranDownloadService)
    {
        InitializeComponent();
        _quranApiService = quranApiService;
        _quranDownloadService = quranDownloadService;
        BindingContext = this;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        LoadSurahs();
    }

    private async void LoadSurahs()
    {
        try
        {
            _allSurahs = await _quranApiService.GetSurahsAsync();
            
            // تحديث حالة التحميل لكل سورة
            foreach (var surah in _allSurahs)
            {
                surah.IsDownloaded = _quranDownloadService.IsSurahDownloaded(surah.Number);
            }

            RefreshList();
        }
        catch (Exception ex)
        {
            await DisplayAlert("خطأ", $"فشل تحميل السور: {ex.Message}", "حسناً");
        }
    }

    private void RefreshList()
    {
        var searchText = SearchBar.Text;
        IEnumerable<Surah> filtered;

        if (string.IsNullOrWhiteSpace(searchText))
        {
            filtered = _allSurahs;
        }
        else
        {
            filtered = _allSurahs.Where(s =>
                s.Name.Contains(searchText) ||
                s.EnglishName.Contains(searchText, StringComparison.OrdinalIgnoreCase) ||
                s.EnglishNameTranslation.Contains(searchText, StringComparison.OrdinalIgnoreCase));
        }

        Surahs.Clear();
        foreach (var surah in filtered)
            Surahs.Add(surah);
    }

    private void OnSearchTextChanged(object sender, TextChangedEventArgs e)
    {
        RefreshList();
    }

    private async void OnSurahSelected(object sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is Surah selectedSurah)
        {
            var detailPage = new SurahDetailPage(_quranApiService, _quranDownloadService, selectedSurah);
            await Navigation.PushAsync(detailPage);
            ((CollectionView)sender).SelectedItem = null;
        }
    }

    private async void OnSyncAllClicked(object sender, EventArgs e)
    {
        try
        {
            SyncButton.IsVisible = false;
            SyncProgressLayout.IsVisible = true;
            SyncProgressBar.Progress = 0;
            SyncStatusLabel.Text = "بدء مزامنة المصحف... 0%";

            await _quranApiService.SyncFullQuranAsync((progress) =>
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    SyncProgressBar.Progress = progress;
                    SyncStatusLabel.Text = $"جاري المزامنة... {progress:P0}";
                });
            });

            SyncProgressLayout.IsVisible = false;
            SyncButton.Text = "✓ تم تحديث المصحف أوفلاين";
            SyncButton.IsEnabled = false;
            SyncButton.IsVisible = true;

            // تحديث القائمة
            LoadSurahs();

            await DisplayAlert("تمت المزامنة", "تم تحميل كافة نصوص السور بنجاح. يمكنك الآن قراءة القرآن كاملاً بدون إنترنت.", "موافق");
        }
        catch (Exception ex)
        {
            SyncProgressLayout.IsVisible = false;
            SyncButton.IsVisible = true;
            await DisplayAlert("خطأ", $"فشل المزامنة: {ex.Message}", "حسناً");
        }
    }
}