using System.Collections.ObjectModel;
using AthkarApp.Models;
using AthkarApp.Services;

namespace AthkarApp.Views;

public partial class MushafPage : ContentPage
{
    private readonly IQuranApiService _quranApiService;

    private bool _isScrollMode = true;
    private int  _currentPage  = 1;
    private bool _isFullscreen = false;

    private readonly ObservableCollection<PageData> _carouselPages = new();

    public MushafPage(IQuranApiService quranApiService)
    {
        InitializeComponent();
        _quranApiService = quranApiService;

        // تهيئة 604 صفحة فارغة لتسريع الأداء ومنع التعليق
        for (int i = 1; i <= 604; i++)
        {
            _carouselPages.Add(new PageData { Number = i, Ayahs = new List<Ayah>() });
        }

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

        // عودة للأعلى بشكل فوري
        if (ScrollModeContainer != null)
        {
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                await Task.Delay(50); // انتظار بسيط لضمان تحديث الواجهة
                await ScrollModeContainer.ScrollToAsync(0, 0, animated: false);
            });
        }
    }

    // ===================== تحميل صفحات CarouselView =====================

    // ===================== تحميل صفحات CarouselView (صفحة محددة) =====================

    private async Task LoadCarouselPage(int pageNumber)
    {
        if (pageNumber < 1 || pageNumber > 604) return;
        
        // إذا تم تحميلها مسبقاً، تخطى
        var existingPage = _carouselPages[pageNumber - 1];
        if (existingPage.Ayahs != null && existingPage.Ayahs.Any()) return;

        var data = await _quranApiService.GetPageAsync(pageNumber);
        if (data != null)
        {
            data.HasBismillah = data.Ayahs?.Any(a =>
                a.NumberInSurah == 1 &&
                a.Surah?.Number != 9 &&
                a.Surah?.Number != 1) ?? false;

            // تحديث بالصفحة المحملة
            _carouselPages[pageNumber - 1] = data;
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

        _isFullscreen = true;
        UpdateFullscreenState();

        LoadScrollPage(_currentPage);
    }

    private async void OnSwipeModeClicked(object sender, EventArgs e)
    {
        _isScrollMode = false;
        ScrollModeContainer.IsVisible = false;
        SwipeModeView.IsVisible       = true;

        ScrollModeBtnCtrl.BackgroundColor = Color.FromArgb("#A5A58D");
        SwipeModeBtnCtrl.BackgroundColor  = Color.FromArgb("#2C6E2C");

        _isFullscreen = true;
        UpdateFullscreenState();

        await LoadCarouselPage(_currentPage);
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
        await LoadCarouselPage(pageNum);
        SwipeModeView.Position = pageNum - 1;
    }

    private void OnCarouselPositionChanged(object sender, PositionChangedEventArgs e)
    {
        _currentPage = e.CurrentPosition + 1;
        UpdateNavUI();

        var current = _carouselPages[e.CurrentPosition];
        if (current.Ayahs != null && current.Ayahs.Any())
        {
            CurrentSurahLabel.Text = current.Ayahs.First().Surah?.Name ?? "";
        }

        // تحميل الصفحة الحالية (إذا لم تكن)، والسابقة والتالية
        _ = LoadCarouselPage(_currentPage);
        _ = LoadCarouselPage(_currentPage + 1);
        _ = LoadCarouselPage(_currentPage - 1);
    }

    // ===================== تحديد الآية =====================

    private void OnAyahTapped(object sender, TappedEventArgs e)
    {
        if (e.Parameter is Ayah ayah)
        {
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

    // ===================== وضع ملء الشاشة =====================

    private void OnTapScreen(object sender, TappedEventArgs e)
    {
        _isFullscreen = !_isFullscreen;
        UpdateFullscreenState();
    }

    private void UpdateFullscreenState()
    {
        HeaderUI.IsVisible  = !_isFullscreen;
        ModeBarUI.IsVisible = !_isFullscreen;
        FooterUI.IsVisible  = !_isFullscreen;

        // إخفاء الـ Navigation Bar و TabBar الخاصين بـ Shell لمزيد من المساحة للآيات
        Shell.SetNavBarIsVisible(this, !_isFullscreen);
        Shell.SetTabBarIsVisible(this, !_isFullscreen);

        // تغيير أيقونة الزر العائم
        FullscreenBtnLabel.Text = _isFullscreen ? "⤡" : "⤢";
    }

    // ===================== زر الرجوع (الهاتف) =====================

    protected override bool OnBackButtonPressed()
    {
        // 1. إذا كانت لوحة التفسير مفتوحة، نغلقها فقط ولا نغلق التطبيق
        if (TafsirOverlay.IsVisible)
        {
            TafsirOverlay.IsVisible = false;
            return true;
        }

        // 2. إذا كان وضع ملء الشاشة مفعلاً، نخرج منه أولاً
        if (_isFullscreen)
        {
            _isFullscreen = false;
            UpdateFullscreenState();
            return true;
        }

        // 3. خلاف ذلك، تنفيذ الرجوع الافتراضي المعتاد
        return base.OnBackButtonPressed();
    }
}
