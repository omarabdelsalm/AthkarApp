using System.Collections.ObjectModel;
using AthkarApp.Models;
using AthkarApp.Services;

namespace AthkarApp.Views;

public partial class QuranPage : ContentPage
{
    private readonly IQuranApiService _quranApiService;
    private readonly IQuranDownloadService _quranDownloadService;
    private List<Surah> _allSurahs;
    private bool _isAutoScrolling = false;
    private int  _scrollSpeed = 1;
    private int  _currentIndex = 0;

    public ObservableCollection<Surah> Surahs { get; set; } = new();

    public QuranPage(IQuranApiService quranApiService, IQuranDownloadService quranDownloadService)
    {
        InitializeComponent();
        _quranApiService = quranApiService;
        _quranDownloadService = quranDownloadService;
        BindingContext = this;
    }

    protected override bool OnBackButtonPressed()
    {
        if (!string.IsNullOrWhiteSpace(SearchBar.Text))
        {
            SearchBar.Text = string.Empty;
            return true;
        }

        if (Navigation.NavigationStack.Count > 1)
        {
            return base.OnBackButtonPressed();
        }

        Shell.Current.GoToAsync("//AthkarPage");
        return true;
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
            filtered = _allSurahs.Where(s => s.Name.Contains(searchText));
        }

        // تحسين الأداء: تحديث القائمة على الخيط الرئيسي لتجنب التجمد
        MainThread.BeginInvokeOnMainThread(() => {
            Surahs.Clear();
            foreach (var surah in filtered)
                Surahs.Add(surah);
        });
    }

    private async void OnTeacherClicked(object sender, EventArgs e)
    {
        await Navigation.PushAsync(Handler.MauiContext.Services.GetService<MushafTeacherPage>());
    }

    private void OnSearchTextChanged(object sender, TextChangedEventArgs e)
    {
        RefreshList();
    }

    private async void OnSurahTapped(object sender, EventArgs e)
    {
        if (sender is BindableObject bo && bo.BindingContext is Surah selectedSurah)
        {
            var detailPage = new SurahDetailPage(_quranApiService, _quranDownloadService, selectedSurah);
            await Navigation.PushAsync(detailPage);
        }
        else if (sender is VisualElement ve && ve.BindingContext is Surah s) // للهواتف التي تستخدم التاب جشر
        {
            var detailPage = new SurahDetailPage(_quranApiService, _quranDownloadService, s);
            await Navigation.PushAsync(detailPage);
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

    // ===================== التحرك التلقائي (Auto-Scroll) =====================

    private void OnAutoScrollClicked(object sender, EventArgs e)
    {
        if (_isAutoScrolling) StopAutoScroll();
        else StartAutoScroll();
    }

    private void StartAutoScroll()
    {
        _isAutoScrolling = true;
        AutoScrollBtn.Text = "⏹️";
        SpeedControls.IsVisible = true;
        _currentIndex = 0;
        RunAutoScrollStep();
    }

    private void StopAutoScroll()
    {
        _isAutoScrolling = false;
        AutoScrollBtn.Text = "▶️";
        SpeedControls.IsVisible = false;
    }

    private void OnSpeedClicked(object sender, EventArgs e)
    {
        if (sender is Button btn && int.TryParse(btn.CommandParameter?.ToString(), out int speed))
        {
            _scrollSpeed = speed;
            foreach (var child in SpeedControls.Children)
            {
                if (child is Button b)
                    b.BackgroundColor = (b.CommandParameter?.ToString() == speed.ToString()) 
                        ? Color.FromArgb("#88FFFFFF") : Color.FromArgb("#44FFFFFF");
            }
        }
    }

    private async void RunAutoScrollStep()
    {
        if (!_isAutoScrolling || Surahs.Count == 0) return;

        if (_currentIndex >= Surahs.Count)
        {
            StopAutoScroll();
            return;
        }

        // التحرك سورة بسورة في CollectionView
        SurahsCollectionView.ScrollTo(_currentIndex, position: ScrollToPosition.Start, animate: true);
        
        _currentIndex++;

        // سرعة التحريك
        int delay = 5000 / _scrollSpeed; 
        await Task.Delay(delay);

        if (_isAutoScrolling)
        {
            RunAutoScrollStep();
        }
    }
}