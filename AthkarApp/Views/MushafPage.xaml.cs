using System.Collections.ObjectModel;
using AthkarApp.Models;
using AthkarApp.Services;

namespace AthkarApp.Views;

public partial class MushafPage : ContentPage
{
    private readonly IQuranApiService _quranApiService;

    private bool _isScrollMode = true;
    private int  _currentPage  = 1;

    private readonly ObservableCollection<PageData> _carouselPages = new();

    public MushafPage(IQuranApiService quranApiService)
    {
        InitializeComponent();
        _quranApiService = quranApiService;
        SwipeModeView.ItemsSource = _carouselPages;

        LoadScrollPage(1);
    }

    // ===================== تحميل الصفحة (وضع التمرير) =====================

    private async void LoadScrollPage(int pageNumber)
    {
        if (pageNumber < 1 || pageNumber > 604) return;

        _currentPage = pageNumber;
        UpdateNavUI();

        var pageData = await _quranApiService.GetPageAsync(pageNumber);
        if (pageData?.Ayahs == null || !pageData.Ayahs.Any()) return;

        bool showBismillah = pageData.Ayahs.Any(a =>
            a.NumberInSurah == 1 &&
            a.Surah?.Number != 9 &&
            a.Surah?.Number != 1);

        BismillahLabel.IsVisible = showBismillah;

        var firstAyah = pageData.Ayahs.First();
        CurrentSurahLabel.Text = firstAyah.Surah?.Name ?? "";

        AyahsCollectionView.ItemsSource = pageData.Ayahs;
    }

    // ===================== تحميل صفحات CarouselView =====================

    private async Task LoadCarouselPagesUpTo(int targetPage)
    {
        while (_carouselPages.Count < targetPage && _carouselPages.Count < 604)
        {
            int next = _carouselPages.Count + 1;
            var data = await _quranApiService.GetPageAsync(next);

            if (data != null)
            {
                data.HasBismillah = data.Ayahs?.Any(a =>
                    a.NumberInSurah == 1 &&
                    a.Surah?.Number != 9 &&
                    a.Surah?.Number != 1) ?? false;
            }

            _carouselPages.Add(data ?? new PageData
            {
                Number = next,
                Ayahs  = new List<Ayah>()
            });
        }
    }

    // ===================== تبديل الأوضاع =====================

    private void OnScrollModeClicked(object sender, EventArgs e)
    {
        _isScrollMode = true;
        ScrollModeContainer.IsVisible = true;
        SwipeModeView.IsVisible       = false;

        ScrollModeBtnCtrl.BackgroundColor = Color.FromArgb("#2C6E2C");
        SwipeModeBtnCtrl.BackgroundColor  = Color.FromArgb("#A5A58D");

        LoadScrollPage(_currentPage);
    }

    private async void OnSwipeModeClicked(object sender, EventArgs e)
    {
        _isScrollMode = false;
        ScrollModeContainer.IsVisible = false;
        SwipeModeView.IsVisible       = true;

        ScrollModeBtnCtrl.BackgroundColor = Color.FromArgb("#A5A58D");
        SwipeModeBtnCtrl.BackgroundColor  = Color.FromArgb("#2C6E2C");

        await LoadCarouselPagesUpTo(_currentPage);
        SwipeModeView.Position = _currentPage - 1;
    }

    // ===================== التنقل =====================

    private void OnNextPage(object sender, EventArgs e)
    {
        if (_isScrollMode)
        {
            LoadScrollPage(_currentPage + 1);
        }
        else
        {
            if (SwipeModeView.Position < _carouselPages.Count - 1)
                SwipeModeView.Position++;
        }
    }

    private void OnPrevPage(object sender, EventArgs e)
    {
        if (_isScrollMode)
        {
            LoadScrollPage(_currentPage - 1);
        }
        else
        {
            if (SwipeModeView.Position > 0)
                SwipeModeView.Position--;
        }
    }

    private void OnPageEntryCompleted(object sender, EventArgs e)
    {
        if (int.TryParse(PageEntry.Text, out int p) && p >= 1 && p <= 604)
        {
            if (_isScrollMode)
                LoadScrollPage(p);
            else
                _ = GoToCarouselPage(p);
        }
        else
        {
            PageEntry.Text = _currentPage.ToString();
        }
    }

    private async Task GoToCarouselPage(int pageNum)
    {
        await LoadCarouselPagesUpTo(pageNum);
        SwipeModeView.Position = pageNum - 1;
    }

    private async void OnCarouselPositionChanged(object sender, PositionChangedEventArgs e)
    {
        _currentPage = e.CurrentPosition + 1;
        UpdateNavUI();

        if (_carouselPages.Count > e.CurrentPosition)
        {
            var firstAyah = _carouselPages[e.CurrentPosition].Ayahs?.FirstOrDefault();
            if (firstAyah != null)
                CurrentSurahLabel.Text = firstAyah.Surah?.Name ?? "";
        }

        // تحميل 3 صفحات مقدماً
        if (e.CurrentPosition >= _carouselPages.Count - 2)
            await LoadCarouselPagesUpTo(_carouselPages.Count + 3);
    }

    // ===================== تحديد الآية =====================

    private void OnAyahSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is Ayah ayah)
        {
            ((CollectionView)sender).SelectedItem = null;
            ShowTafsir(ayah);
        }
    }

    private void OnSwipeAyahSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is Ayah ayah)
        {
            ((CollectionView)sender).SelectedItem = null;
            ShowTafsir(ayah);
        }
    }

    // ===================== التفسير =====================

    private async void ShowTafsir(Ayah ayah)
    {
        TafsirOverlay.IsVisible  = true;
        TafsirLoader.IsRunning   = true;
        TafsirLoader.IsVisible   = true;
        TafsirBodyLabel.Text     = "";

        TafsirAyahTitle.Text = $"سورة {ayah.Surah?.Name ?? ""} — الآية {ayah.NumberInSurah}";
        TafsirAyahText.Text  = ayah.Text;

        var tafsir = await _quranApiService.GetTafsirAsync(ayah.Number);

        TafsirLoader.IsRunning = false;
        TafsirLoader.IsVisible = false;
        TafsirBodyLabel.Text   = tafsir;
    }

    private void OnCloseTafsir(object sender, EventArgs e)
        => TafsirOverlay.IsVisible = false;

    private void OnTafsirOverlayTapped(object sender, TappedEventArgs e)
        => TafsirOverlay.IsVisible = false;

    // يمنع إغلاق اللوحة عند النقر على محتواها
    private void OnTafsirPanelTapped(object sender, TappedEventArgs e)
    {
        // لا شيء - يمنع الحدث من الوصول إلى TafsirOverlay
    }

    // ===================== تحديث عناصر التنقل =====================

    private void UpdateNavUI()
    {
        PageEntry.Text         = _currentPage.ToString();
        PageCounterLabel.Text  = $"{_currentPage} / 604";
        PrevButton.IsEnabled   = _currentPage > 1;
        NextButton.IsEnabled   = _currentPage < 604;
    }
}
