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
    private bool _isAutoScrolling = false;
    private int  _scrollSpeed = 1; // 1, 2, 3
    private int  _currentAyahIndex = 0;

    private const string LastReadPageKey = "Mushaf_LastReadPage";
    private const string BookmarkPageKey = "Mushaf_BookmarkPage";

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

        int startPage = Preferences.Default.Get(LastReadPageKey, 1);
        LoadScrollPage(startPage);
    }

    // ===================== تحميل الصفحة (وضع التمرير) =====================

    private async void LoadScrollPage(int pageNumber)
    {
        if (pageNumber < 1 || pageNumber > 604) return;

        _currentPage = pageNumber;
        UpdateNavUI();

        MainLoader.IsRunning = true;
        MainLoader.IsVisible = true;

        var pageData = await _quranApiService.GetPageAsync(pageNumber);
        
        MainLoader.IsRunning = false;
        MainLoader.IsVisible = false;

        if (pageData?.Ayahs == null || !pageData.Ayahs.Any())
        {
            BindableLayout.SetItemsSource(AyahsLayout, null);
            if (Connectivity.Current.NetworkAccess != NetworkAccess.Internet)
            {
                await DisplayAlert("تنبيه", "هذه الصفحة غير محملة أوفلاين. يرجى الاتصال بالإنترنت مرة واحدة لتحميلها أو عمل مزامنة شاملة من صفحة السور.", "حسناً");
            }
            return;
        }

        bool showBismillah = pageData.Ayahs.Any(a =>
            a.NumberInSurah == 1 &&
            a.Surah?.Number != 9 &&
            a.Surah?.Number != 1);

        BismillahLabel.IsVisible = showBismillah;

        var firstAyah = pageData.Ayahs.First();
        CurrentSurahLabel.Text = firstAyah.Surah?.Name ?? "";

        BindableLayout.SetItemsSource(AyahsLayout, pageData.Ayahs);

        // عودة للأعلى بشكل فوري
        if (AyahsScrollView != null && pageData.Ayahs.Any())
        {
            AyahsScrollView.ScrollToAsync(0, 0, false);
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

        MainLoader.IsRunning = true;
        MainLoader.IsVisible = true;

        var data = await _quranApiService.GetPageAsync(pageNumber);

        MainLoader.IsRunning = false;
        MainLoader.IsVisible = false;

        if (data != null)
        {
            data.HasBismillah = data.Ayahs?.Any(a =>
                a.NumberInSurah == 1 &&
                a.Surah?.Number != 9 &&
                a.Surah?.Number != 1) ?? false;

            // تحديث بالصفحة المحملة بلا استبدال الكائن لتجنب إعادة تعيين 
            existingPage.Ayahs = data.Ayahs;
            existingPage.HasBismillah = data.HasBismillah;
        }
    }

    // ===================== تبديل الأوضاع =====================

    private void OnScrollModeClicked(object sender, EventArgs e)
    {
        _isScrollMode = true;
        AyahsScrollView.IsVisible     = true;
        SwipeModeView.IsVisible       = false;

        ScrollModeBtnCtrl.BackgroundColor = Color.FromArgb("#2C6E2C");
        SwipeModeBtnCtrl.BackgroundColor  = Color.FromArgb("#A5A58D");

        _isFullscreen = true;
        UpdateFullscreenState();

        LoadScrollPage(_currentPage);
    }

    private async void OnSwipeModeClicked(object sender, EventArgs e)
    {
        StopAutoScroll();
        _isScrollMode = false;
        AyahsScrollView.IsVisible     = false;
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
        StopAutoScroll();
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
        StopAutoScroll();
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
        StopAutoScroll();
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
        
        int savedBookmark = Preferences.Default.Get(BookmarkPageKey, 0);
        BookmarkIconLabel.Opacity = (savedBookmark == _currentPage) ? 1.0 : 0.5;

        // حفظ مكان التوقف تلقائياً
        Preferences.Default.Set(LastReadPageKey, _currentPage);

        // التحقق من إنجاز الورد اليومي للختمة
        CheckKhatmahProgress();
    }

    private async void CheckKhatmahProgress()
    {
        bool isKhatmahActive = Preferences.Default.Get("Khatmah_PlanActive", false);
        if (isKhatmahActive)
        {
            int durationDays = Preferences.Default.Get("Khatmah_PlanDuration", 30);
            DateTime startDate = Preferences.Default.Get("Khatmah_PlanStartDate", DateTime.Now);
            
            int totalPagesPerDay = (int)Math.Ceiling(604.0 / durationDays);
            int daysPassed = (DateTime.Now.Date - startDate.Date).Days;
            int expectedPageToBeAt = Math.Min(604, (daysPassed + 1) * totalPagesPerDay);

            if (_currentPage >= expectedPageToBeAt)
            {
                string lastGoalDate = Preferences.Default.Get("Khatmah_LastGoalReachedDate", "");
                string todayStr = DateTime.Now.Date.ToString("yyyyMMdd");
                
                if (lastGoalDate != todayStr)
                {
                    Preferences.Default.Set("Khatmah_LastGoalReachedDate", todayStr);
                    await DisplayAlert("تهانينا 🌟", "لقد أتممت وردك اليومي من الختمة بنجاح! تقبل الله طاعتك.", "الحمد لله");
                }
            }
        }
    }

    // ===================== اختيار القارئ =====================
    
    private async void OnReciterIconTapped(object sender, EventArgs e)
    {
        var reciters = QuranReciter.GetPopularReciters();
        var options = reciters.Select(r => r.Name).ToArray();
        
        string currentReciterId = Preferences.Default.Get("SelectedReciterId", "ar.alafasy");
        var currentReciter = reciters.FirstOrDefault(r => r.Id == currentReciterId);
        
        string title = $"القارئ الحالي: {currentReciter?.Name ?? "العفاسي"}";
        string action = await DisplayActionSheet(title, "إلغاء", null, options);
        
        if (action != null && action != "إلغاء")
        {
            var selected = reciters.FirstOrDefault(r => r.Name == action);
            if (selected != null && selected.Id != currentReciterId)
            {
                Preferences.Default.Set("SelectedReciterId", selected.Id);
                
                // إعادة تحميل الصفحة الحالية لجلب روابط الصوت الجديدة لهذا القارئ
                if (_isScrollMode) LoadScrollPage(_currentPage);
                else {
                    // مسح الكاش لصفحات الكاروسيل لإجبارها على إعادة التحميل بروابط الصوت الجديدة
                    foreach(var p in _carouselPages) p.Ayahs = new List<Ayah>();
                    await LoadCarouselPage(_currentPage);
                }
                
                await DisplayAlert("تم تغيير القارئ", $"سيتم تشغيل التلاوات بصوت الشيخ {selected.Name}.", "حسناً");
            }
        }
    }

    private async void OnBookmarkTapped(object sender, EventArgs e)
    {
        int savedBookmark = Preferences.Default.Get(BookmarkPageKey, 0);

        if (savedBookmark == _currentPage)
        {
            Preferences.Default.Remove(BookmarkPageKey);
            BookmarkIconLabel.Opacity = 0.5;
            await DisplayAlert("العلامة المرجعية", "تم إزالة العلامة المرجعية من هذه الصفحة.", "حسناً");
        }
        else if (savedBookmark > 0)
        {
            string action = await DisplayActionSheet("العلامة المرجعية", "إلغاء", null, 
                $"الذهاب للعلامة السابقة (صفحة {savedBookmark})", 
                "حفظ هذه الصفحة كعلامة جديدة");

            if (action == $"الذهاب للعلامة السابقة (صفحة {savedBookmark})")
            {
                if (_isScrollMode) LoadScrollPage(savedBookmark);
                else await GoToCarouselPage(savedBookmark);
            }
            else if (action == "حفظ هذه الصفحة كعلامة جديدة")
            {
                Preferences.Default.Set(BookmarkPageKey, _currentPage);
                BookmarkIconLabel.Opacity = 1.0;
                await DisplayAlert("تم", $"تم حفظ صفحة {_currentPage} كعلامة مرجعية جديدة.", "حسناً");
            }
        }
        else
        {
            Preferences.Default.Set(BookmarkPageKey, _currentPage);
            BookmarkIconLabel.Opacity = 1.0;
            await DisplayAlert("تم", $"تم حفظ صفحة {_currentPage} كعلامة مرجعية.", "حسناً");
        }
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

    // ===================== التحرك التلقائي (Auto-Scroll) =====================

    private void OnAutoScrollClicked(object sender, EventArgs e)
    {
        if (_isAutoScrolling)
        {
            StopAutoScroll();
        }
        else
        {
            StartAutoScroll();
        }
    }

    private void StartAutoScroll()
    {
        if (!_isScrollMode)
        {
            DisplayAlert("تنبيه", "خاصية التحرك التلقائي متاحة في وضع التمرير فقط.", "حسناً");
            return;
        }

        _isAutoScrolling = true;
        AutoScrollBtn.Text = "⏹️";
        SpeedControls.IsVisible = true;
        
        // البدء من أول آية ظاهرة حالياً أو من البداية
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
            
            // تحديث مظهر الأزرار
            foreach (var child in SpeedControls.Children)
            {
                if (child is Button b)
                {
                    b.BackgroundColor = (b.CommandParameter?.ToString() == speed.ToString()) 
                        ? Color.FromArgb("#88FFFFFF") 
                        : Color.FromArgb("#44FFFFFF");
                }
            }
        }
    }

    private async void RunAutoScrollStep()
    {
        if (!_isAutoScrolling || !_isScrollMode) return;

        // تحرك بكسل بكسل بدلاً من الانتقال بين الآيات
        double scrollStep = _scrollSpeed * 0.8; 
        double currentY = AyahsScrollView.ScrollY;
        double maxY = AyahsScrollView.ContentSize.Height - AyahsScrollView.Height;

        if (currentY >= maxY - 5)
        {
            if (_currentPage < 604)
            {
                LoadScrollPage(_currentPage + 1);
                await Task.Delay(2000); 
                RunAutoScrollStep();
            }
            else
            {
                StopAutoScroll();
            }
            return;
        }

        // التحريك السلس
        await AyahsScrollView.ScrollToAsync(0, currentY + scrollStep, false);

        // تكرار الخطوة بسرعة لضمان النعومة
        await Task.Delay(30);

        if (_isAutoScrolling)
        {
            RunAutoScrollStep();
        }
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
