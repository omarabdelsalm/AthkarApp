using System.Collections.ObjectModel;
using System.Windows.Input;
using AthkarApp.Models;
using AthkarApp.Services;

namespace AthkarApp.Views;

public partial class MushafPage : ContentPage
{
    private readonly IQuranApiService _quranApiService;

    // وضع العرض الحالي: true = تمرير، false = تقليب
    private bool _isScrollMode = true;

    // رقم الصفحة الحالية (1-604)
    private int _currentPage = 1;

    // كاش الصفحات المحملة في وضع التقليب
    private readonly ObservableCollection<PageData> _carouselPages = new();

    // آخر آية تم تحديدها
    private Ayah _selectedAyah;

    // Command للآيات داخل CarouselView DataTemplate
    public ICommand AyahTappedCommand { get; }

    public MushafPage(IQuranApiService quranApiService)
    {
        InitializeComponent();
        _quranApiService = quranApiService;

        AyahTappedCommand = new Command<Ayah>(OnAyahTapped);
        BindingContext = this;

        // تهيئة CarouselView
        SwipeModeView.ItemsSource = _carouselPages;

        // تحميل الصفحة الأولى
        LoadScrollPage(1);
    }

    // ==================== تحميل الصفحات ====================

    private async void LoadScrollPage(int pageNumber)
    {
        if (pageNumber < 1 || pageNumber > 604) return;

        _currentPage = pageNumber;
        UpdatePageUI();

        var pageData = await _quranApiService.GetPageAsync(pageNumber);

        if (pageData?.Ayahs == null || !pageData.Ayahs.Any()) return;

        // عرض البسملة إذا كانت الصفحة تبدأ بسورة جديدة (الأولى أو آية 1 من سورة غير التوبة)
        bool showBismillah = pageData.Ayahs.Any(a =>
            a.NumberInSurah == 1 && a.Surah?.Number != 9 && a.Surah?.Number != 1);

        BismillahLabel.IsVisible = showBismillah;

        // تحديث عنوان السورة والجزء
        var firstAyah = pageData.Ayahs.First();
        CurrentSurahLabel.Text = firstAyah.Surah?.Name ?? "";
        JuzLabel.Text = "";

        AyahsCollectionView.ItemsSource = pageData.Ayahs;
    }

    private async Task LoadCarouselPagesUpTo(int targetPage)
    {
        while (_carouselPages.Count < targetPage && _carouselPages.Count < 604)
        {
            int nextPageNum = _carouselPages.Count + 1;
            var pageData = await _quranApiService.GetPageAsync(nextPageNum);

            if (pageData != null)
            {
                // تحديد إذا بدأ الصفحة بسورة جديدة
                pageData.HasBismillah = pageData.Ayahs?.Any(a =>
                    a.NumberInSurah == 1 && a.Surah?.Number != 9 && a.Surah?.Number != 1) ?? false;
            }

            _carouselPages.Add(pageData ?? new PageData { Number = nextPageNum, Ayahs = new List<Ayah>() });
        }
    }

    // ==================== أوضاع العرض ====================

    private async void OnScrollModeClicked(object sender, EventArgs e)
    {
        _isScrollMode = true;
        ScrollModeView.IsVisible = true;
        SwipeModeView.IsVisible = false;

        ScrollModeBtn.BackgroundColor = Color.FromArgb("#2C6E2C");
        SwipeModeBtn.BackgroundColor = Color.FromArgb("#A5A58D");

        // تزامن الصفحة مع وضع التقليب
        LoadScrollPage(_currentPage);
    }

    private async void OnSwipeModeClicked(object sender, EventArgs e)
    {
        _isScrollMode = false;
        ScrollModeView.IsVisible = false;
        SwipeModeView.IsVisible = true;

        ScrollModeBtn.BackgroundColor = Color.FromArgb("#A5A58D");
        SwipeModeBtn.BackgroundColor = Color.FromArgb("#2C6E2C");

        // تحميل الصفحات حتى الصفحة الحالية
        await LoadCarouselPagesUpTo(_currentPage);
        SwipeModeView.Position = _currentPage - 1;
    }

    // ==================== التنقل بين الصفحات ====================

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
        if (int.TryParse(PageEntry.Text, out int pageNum) && pageNum >= 1 && pageNum <= 604)
        {
            if (_isScrollMode)
                LoadScrollPage(pageNum);
            else
                _ = SwitchCarouselToPage(pageNum);
        }
        else
        {
            PageEntry.Text = _currentPage.ToString();
        }
    }

    private async Task SwitchCarouselToPage(int pageNum)
    {
        await LoadCarouselPagesUpTo(pageNum);
        SwipeModeView.Position = pageNum - 1;
    }

    private async void OnCarouselPositionChanged(object sender, PositionChangedEventArgs e)
    {
        _currentPage = e.CurrentPosition + 1;
        UpdatePageUI();

        // تحديث اسم السورة
        if (_carouselPages.Count > e.CurrentPosition)
        {
            var page = _carouselPages[e.CurrentPosition];
            var firstAyah = page.Ayahs?.FirstOrDefault();
            if (firstAyah != null)
                CurrentSurahLabel.Text = firstAyah.Surah?.Name ?? "";
        }

        // تحميل الصفحات التالية مسبقاً (3 صفحات)
        if (e.CurrentPosition >= _carouselPages.Count - 2)
            await LoadCarouselPagesUpTo(_carouselPages.Count + 3);
    }

    // ==================== تحديد الآية في وضع التمرير ====================

    private void OnAyahSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is Ayah ayah)
        {
            OnAyahTapped(ayah);
            // إلغاء التحديد البصري بعد لحظة
            ((CollectionView)sender).SelectedItem = null;
        }
    }

    // ==================== التفسير ====================

    private async void OnAyahTapped(Ayah ayah)
    {
        if (ayah == null) return;
        _selectedAyah = ayah;

        // إظهار اللوحة
        TafsirOverlay.IsVisible = true;
        TafsirLoader.IsVisible = true;
        TafsirBodyLabel.Text = "";

        // تحديث معلومات الآية
        TafsirAyahTitle.Text = $"سورة {ayah.Surah?.Name ?? ""} - الآية {ayah.NumberInSurah}";
        TafsirAyahText.Text = ayah.Text;

        // جلب التفسير
        var tafsir = await _quranApiService.GetTafsirAsync(ayah.Number);
        TafsirLoader.IsVisible = false;
        TafsirBodyLabel.Text = tafsir;
    }

    private void OnCloseTafsir(object sender, EventArgs e)
    {
        TafsirOverlay.IsVisible = false;
    }

    private void OnTafsirOverlayTapped(object sender, TappedEventArgs e)
    {
        // إغلاق عند النقر خارج اللوحة
        TafsirOverlay.IsVisible = false;
    }

    // ==================== تحديث واجهة الصفحة ====================

    private void UpdatePageUI()
    {
        PageEntry.Text = _currentPage.ToString();
        PageCounterLabel.Text = $"{_currentPage} / 604";
        PrevButton.IsEnabled = _currentPage > 1;
        NextButton.IsEnabled = _currentPage < 604;
    }
}
